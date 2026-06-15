using System;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game
{
    /// <summary>
    /// 포탑 공격 기차(shell). 공격 동작은 TurretAttackModule(같은 GameObject)이 담당하고,
    /// 이 클래스는 Train 공통(HP/레벨/스킬) + 직렬화 설정 보유 + 모듈로의 위임을 수행한다.
    /// </summary>
    public class TurretTrain : Train, ITrainable, IProjectileEmitter,
        IAttackEvents, IProjectileAttacker, IExternalProjectileSpawner, IForceAttacker
    {
        #region Serialized config (모듈이 접근자로 읽음)
        [SerializeField]
        private Transform[] turretProjectileSpawnPoints;
        [SerializeField]
        private bool useParticleProjectile;
        [SerializeField]
        private bool isTargeting = false;
        [SerializeField]
        private GameObject turret;
        [SerializeField]
        private GameObject turretModel;

        public Transform[] SpawnPoints => turretProjectileSpawnPoints;
        public GameObject TurretObject => turret;
        public GameObject TurretModel => turretModel;
        public bool UseParticleProjectile => useParticleProjectile;
        public bool IsTargeting => isTargeting;
        public bool IsRotateTurret => isRotateTurret;
        #endregion

        private TurretTrainData turretTrainData => _trainData as TurretTrainData;
        private TurretAttackModule _module;

        public TurretTrainStatus BaseStatus => _module != null ? _module.BaseStatus : default;

        protected override void Setup()
        {
            base.Setup();
            _module = GetComponent<TurretAttackModule>();
            _module?.InitializeModule(this);
        }

        public override void ClearAttachedProjectiles() => _module?.ClearAttachedProjectiles();

        public Monster GetNearTargetMonsterPublic() => _module != null ? _module.GetNearTargetMonsterPublic() : null;

        #region Capability forwarding
        public event Action<Monster> OnAttacked
        {
            add { if (_module != null) _module.OnAttacked += value; }
            remove { if (_module != null) _module.OnAttacked -= value; }
        }

        public event Action OnTargetPosAttacked
        {
            add { if (_module != null) _module.OnTargetPosAttacked += value; }
            remove { if (_module != null) _module.OnTargetPosAttacked -= value; }
        }

        public Func<Vector3?> TargetPosOverride
        {
            get => _module?.TargetPosOverride;
            set { if (_module != null) _module.TargetPosOverride = value; }
        }

        public float CurrentAttackDamage => _module != null ? _module.CurrentAttackDamage : 0f;

        public void RepeatNormalAttack(Vector2? aimPosition = null) => _module?.RepeatNormalAttack(aimPosition);

        public void SpawnProjectileAtWorldPositionPublic(Monster target, Vector3 worldPosition, bool playSound = false)
            => _module?.SpawnProjectileAtWorldPositionPublic(target, worldPosition, playSound);

        public bool ForceAttack() => _module != null && _module.ForceAttack();

        public void SpawnExternalProjectile(Projectile prefab, float radius,
            IProjectileTarget target = null, float damageMul = 1f, float shoveScale = 1f)
            => _module?.SpawnExternalProjectile(prefab, radius, target, damageMul, shoveScale);

        public void AddProjectileModifier(IProjectileModifier modifier) => _module?.AddProjectileModifier(modifier);
        public void RemoveProjectileModifier(IProjectileModifier modifier) => _module?.RemoveProjectileModifier(modifier);
        #endregion

        #region Upgrade / Stats forwarding
        public override void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;
            int prevLevel = CurrentLevel; // 업그레이드 전 레벨
            base.Upgrade(upgradeData);
            _module?.ApplyUpgrade(upgradeData, prevLevel);

            // 업그레이드 부여 패시브 등록은 _skillModule(Train 소유)이므로 shell에서 처리.
            if (upgradeData is TurretTrainUpgradeData turretUpgradeData)
            {
                var passiveId = turretUpgradeData.GetPassiveSkillDataId(prevLevel + 1);
                if (!string.IsNullOrEmpty(passiveId))
                {
                    var passiveData = DatabaseManager.Instance.GetDB().TrainSkillDataDB.trainPassiveSkillDataList
                        .Find(s => s != null && s.Id == passiveId);
                    _skillModule.RegisterPassiveFromData(passiveData);
                }
            }
        }

        public override void StatusUpgrade(TurretTrainStatus upgradeData) => _module?.StatusUpgrade(upgradeData);

        public override void ApplyStatsByCurrentValue(IStat[] stats)
        {
            if (stats == null || stats.Length == 0 || _module == null) return;
            foreach (var stat in stats)
            {
                if (stat == null) continue;
                if (!_module.ApplyAttackStatByCurrentValue(stat))
                    ApplyStat(stat);
            }
        }

        protected override void ApplyStat(IStat stat)
        {
            base.ApplyStat(stat);
            _module?.ApplyAttackStat(stat);
        }

        public override void ApplyStatsLevelAware(IStat[] stats, int newLevel, int prevLevel = 0)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
                ApplyStatLevelAware(stat, newLevel, prevLevel);
        }

        protected override void ApplyStatLevelAware(IStat stat, int newLevel, int prevLevel)
        {
            if (_module != null && _module.ApplyAttackStatLevelAware(stat, newLevel, prevLevel)) return;
            base.ApplyStatLevelAware(stat, newLevel, prevLevel);
        }

        public override void CopyProgressFrom(Train source)
        {
            base.CopyProgressFrom(source);
            if (source is TurretTrain srcTurret)
                _module?.CopyProgressFrom(srcTurret._module);
        }
        #endregion

        #region Display
        public override float CurrentAttackRange => _module != null ? _module.CurrentAttackRange : 0f;

        public override Transform GetSkillSpawnPoint(int index)
        {
            if (turretProjectileSpawnPoints == null || turretProjectileSpawnPoints.Length == 0)
                return base.GetSkillSpawnPoint(index);

            if (index < 0) index = 0;
            if (index >= turretProjectileSpawnPoints.Length) index = turretProjectileSpawnPoints.Length - 1;

            return turretProjectileSpawnPoints[index] != null
                ? turretProjectileSpawnPoints[index]
                : base.GetSkillSpawnPoint(index);
        }

        public override void ApplyPassiveSkills()
        {
            var passives = turretTrainData?.PassiveSkillDatas;
            if (passives == null) return;
            // 삼중택일은 픽한 스킬 1개만 부여: Active 픽=패시브 미적용, Passive 픽=선택 1개,
            // None(일반 스폰)=기본 키트 전부. (Train._IsPassiveApplied 공용 규칙)
            foreach (var p in passives)
                if (_IsPassiveApplied(p.Id))
                    _skillModule.RegisterPassiveFromData(p);
        }

        public override string GetStatSummary()
        {
            var s = _module != null ? _module.CurrentStatus : default;
            return $"DMG={s.AttackDamage} | RANGE={s.AttackRange} | AREA={s.AttackArea} | CNT={s.AttackCount} | TGT={s.TargetCount} | MaxHp={_currentMaxHp}";
        }

        public override (string label, string value)[] GetStatDetails()
        {
            var s = _module != null ? _module.CurrentStatus : default;
            Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            var details = new System.Collections.Generic.List<(string label, string value)>
            {
                (L("Detail_HP", "HP"), $"{Mathf.RoundToInt(_currentMaxHp)}"),
                (L("Detail_Damage", "공격력"), $"{Mathf.RoundToInt(s.AttackDamage)}"),
                (L("Detail_Range", "사거리"), $"{s.AttackRange:F1}"),
            };

            // 범위(AttackArea)는 실제로 폭발 반경으로 쓰는 포탑만 표시.
            if (s.AttackArea > 0f && turretTrainData != null && turretTrainData.UsesAttackArea)
                details.Add((L("Detail_Area", "범위"), $"{s.AttackArea:F1}"));

            details.Add((L("Detail_Speed", "공격속도"), $"{ToAttackSpeed(s.AttackInterval):F2}"));

            if (s.TargetCount > 1)
                details.Add((L("Detail_Targets", "대상 수"), $"{s.TargetCount}"));

            details.Add((L("Detail_CritChance", "크리티컬 확률"), $"{s.CriticalChance:F0}%"));
            details.Add((L("Detail_CritDamage", "크리티컬 데미지"), $"+{Projectile.BaseCriticalDamagePercent + s.CriticalDamage:F0}%"));
            return details.ToArray();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (turretTrainData != null && _module != null)
                Gizmos.DrawWireSphere(transform.position, _module.CurrentAttackRange);
        }
        #endregion
    }
}
