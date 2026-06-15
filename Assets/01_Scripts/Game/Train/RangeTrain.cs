using System;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game
{
    /// <summary>
    /// 범위 공격 기차(shell). 공격 동작은 RangeAttackModule(같은 GameObject)이 담당하고,
    /// 이 클래스는 Train 공통(HP/레벨/스킬)과 모듈로의 위임만 수행한다.
    /// </summary>
    public class RangeTrain : Train, ITrainable, ISlowProvider,
        IAttackEvents, IExternalProjectileSpawner, IForceAttacker, IShoveSuppressible
    {
        private RangeTrainData rangeTrainData => _trainData as RangeTrainData;

        private RangeAttackModule _module;

        public RangeTrainStatus BaseStatus => _module != null ? _module.BaseStatus : default;

        protected override void Setup()
        {
            base.Setup();
            if (rangeTrainData == null) return;

            _module = GetComponent<RangeAttackModule>();
            _module?.InitializeModule(this);
        }

        public override void ClearAttachedProjectiles() => _module?.ClearAttachedProjectiles();

        #region Capability forwarding
        public event Action<Monster> OnAttacked
        {
            add { if (_module != null) _module.OnAttacked += value; }
            remove { if (_module != null) _module.OnAttacked -= value; }
        }

        public bool ForceAttack() => _module != null && _module.ForceAttack();

        public void SetSuppressMainProjectileShove(bool suppress) => _module?.SetSuppressMainProjectileShove(suppress);

        public void SpawnExternalProjectile(Projectile prefab, float radius, IProjectileTarget target = null, float damageMul = 1f, float shoveScale = 1f)
            => _module?.SpawnExternalProjectile(prefab, radius, target, damageMul, shoveScale);

        // 누적 둔화율(%)을 둔화 배율로 변환.
        public float GetSlowValue() => _module != null ? _module.GetSlowValue() : 1f;
        #endregion

        #region Upgrade / Stats forwarding
        public override void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;
            int prevLevel = CurrentLevel; // 업그레이드 전 레벨
            base.Upgrade(upgradeData);
            _module?.ApplyUpgrade(upgradeData, prevLevel);
        }

        public override void StatusUpgrade(RangeTrainStatus upgradeData) => _module?.StatusUpgrade(upgradeData);

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
            if (source is RangeTrain srcRange)
                _module?.CopyProgressFrom(srcRange._module);
        }
        #endregion

        #region Display
        public override float CurrentAttackRange => _module != null ? _module.CurrentAttackRange : 0f;
        public override float RangeIndicatorRadius => _module != null ? _module.RangeIndicatorRadius : CurrentAttackRange;

        public override void ApplyPassiveSkills()
        {
            var passives = rangeTrainData?.PassiveSkillDatas;
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
            return $"DMG={s.AttackDamage} | RANGE={s.AttackRange} | AREA={s.AttackArea} | MaxHp={_currentMaxHp}";
        }

        public override (string label, string value)[] GetStatDetails()
        {
            var s = _module != null ? _module.CurrentStatus : default;
            Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            return new[]
            {
                (L("Detail_HP", "HP"), $"{Mathf.RoundToInt(_currentMaxHp)}"),
                (L("Detail_Damage", "공격력"), $"{Mathf.RoundToInt(s.AttackDamage)}"),
                (L("Detail_Range", "사거리"), $"{s.AttackRange:F1}"),
                (L("Detail_Area", "범위"), $"{s.AttackArea:F1}"),
                (L("Detail_Speed", "공격속도"), $"{ToAttackSpeed(s.AttackInterval):F2}"),
                (L("Detail_CritChance", "크리티컬 확률"), $"{s.CriticalChance:F0}%"),
                (L("Detail_CritDamage", "크리티컬 데미지"), $"+{Projectile.BaseCriticalDamagePercent + s.CriticalDamage:F0}%"),
            };
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (rangeTrainData != null)
                Gizmos.DrawWireSphere(transform.position, rangeTrainData.RangeTrainStatus.AttackRange);
        }
        #endregion
    }
}
