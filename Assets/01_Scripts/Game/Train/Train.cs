using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cumic;
using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Train : MonoBehaviour, ITrainable, IProjectileTarget,
        IAttackEvents, IExternalProjectileSpawner, IForceAttacker, ISlowProvider
    {
        #region Verialbes

        #region Field
        [SerializeField]
        protected string id;
        [SerializeField]
        protected float explosionRadius = 5f;
        [SerializeField]
        protected float explosionForce = 10f;
        [SerializeField]
        protected bool isRotateTurret = true;
        #endregion

        [ShowInInspector, ReadOnly]
        protected TrainData _trainData;
        protected bool _isDead;
        protected float _currentHp;
        protected int _currentLevel;
        protected float _currentMaxHp;
        protected readonly TrainSkillModule _skillModule = new();
        // 공격 동작 컴포넌트(컴포지션). 같은 GameObject의 *AttackModule. MainTrain 등 비전투 기차는 null.
        protected IAttackModule _attackModule;
        protected TrainChoiceSkillType _skillTypeMask = TrainChoiceSkillType.None;
        protected string _selectedSkillId = null;
        protected bool _initialized;
        private static Material _flashMaterial;
        private SpriteRenderer[] _spriteRenderers;
        private Material[] _originalMaterials;
        private Coroutine _flashCoroutine;

        // ApplyStat 퍼센트 누적 손실 방지용 fractional accumulator (UtilMath.AccumulateIntDelta 참조)
        private float _statMaxHpAccum;

        [HideInInspector]
        public bool IsUnDead;

        public string Id => id;
        public TrainData TrainData => _trainData;
        public bool IsMainTrain => _trainData.IsMainTrain;

        public bool IsDead => _isDead;
        public int CurrentLevel => _currentLevel;
        public bool HasActiveSkill => _skillModule.HasActiveSkill;
        public Sprite SkillIcon => _skillModule.SkillIcon;
        public float SkillCooldown => _skillModule.SkillCooldown;
        public bool CanUseSkill => _skillModule.CanUse;
        public float SkillCooldownRatio => _skillModule.CooldownRatio;
        public float SkillRemainingCooldown => _skillModule.RemainingCooldown;
        public float CurrentHpRatio => _currentMaxHp > 0f ? _currentHp / _currentMaxHp : 0f;

        // 현재(업그레이드 반영) 공격 사거리. 공격 모듈이 있으면 그 값을 사용.
        public virtual float CurrentAttackRange => _attackModule?.CurrentAttackRange ?? 0f;

        // 포탑 회전 여부(공격 모듈이 읽음).
        public bool IsRotateTurret => isRotateTurret;

        // 사거리 표시 원의 반지름. 기본은 공격 사거리, 레인지 포탑은 공격 범위(AttackArea)로 오버라이드.
        public virtual float RangeIndicatorRadius => CurrentAttackRange;

        #endregion

        protected virtual void OnEnable()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            if (_spriteRenderers == null || _originalMaterials == null) return;

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null && i < _originalMaterials.Length)
                    _spriteRenderers[i].sharedMaterial = _originalMaterials[i];
            }
        }

        protected virtual void Awake()
        {
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            _originalMaterials = new Material[_spriteRenderers.Length];

            for (int i = 0; i < _spriteRenderers.Length; i++)
                _originalMaterials[i] = _spriteRenderers[i].sharedMaterial;
        }

        protected virtual void Start()
        {
            if (_initialized)
                return;

            Setup();
        }

        public virtual void Initialize(TrainData trainData, TrainChoiceSkillType skillType = TrainChoiceSkillType.None, string selectedSkillId = null)
        {
            _trainData = trainData;
            _skillTypeMask = skillType;
            _selectedSkillId = selectedSkillId;
            Setup();
            _initialized = true;
        }

        protected virtual void Setup()
        {
            _isDead = false;
            if (_trainData == null)
            {
                _skillModule.Initialize(this, null);
                return;
            }

            _currentMaxHp = _trainData.TrainStatusData.MaxHp;
            _currentHp = _currentMaxHp;
            _currentLevel = -1;
            _skillModule.Initialize(this, _trainData, _skillTypeMask);

            // 공격 모듈(같은 GameObject) 연결 및 초기화. 없으면(MainTrain 등) 비전투 기차.
            _attackModule = GetComponent<IAttackModule>();
            _attackModule?.InitializeModule(this);
        }

        public Transform TargetTransform => transform;
        public bool IsActive => !IsDead;

        // 부착된 공격 모듈(없으면 null). 패시브/스킬이 능력 인터페이스로 접근할 때 사용.
        public IAttackModule AttackModule => _attackModule;

        public virtual Transform GetSkillSpawnPoint(int index)
        {
            return _attackModule?.GetSkillSpawnPoint(index) ?? transform;
        }

        public void Slow(float slowValue, float duration)
        {

        }
        public void ResetMoveSpeed()
        {

        }
        public void Shove(float shovePower, float shoveDuration)
        {

        }
        public void Stun(float stunDuration)
        {

        }

        public virtual void TakeDamage(float damage)
        {
            TakeDamage(damage, false);
        }

        public virtual void TakeDamage(float damage, bool isCritical)
        {
            if (_isDead) return;

            _currentHp -= damage;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);

            _StartDamageFlash();
            GameEventSystem.Publish(new HitEvent(_currentHp, _currentMaxHp, this, transform.position, damage, isCritical));

            if (_currentHp <= 0)
            {
                OnDead();
            }
        }

        protected virtual void OnDead()
        {
            if (IsUnDead) return;

            if (!IsMainTrain)
            {
                GameEventSystem.Publish(new TrainDeadEvent(this));
            }

            _isDead = true;

            if (TrainData.AttackSoundType != SoundType.None && TrainData.DamageType == DamageType.Tick)
            {
                SoundManager.Instance.StopSFX(TrainData.AttackSoundType);
            }

            // 주변 적을 밀치는 효과
            PushNearbyEnemies();
        }

        protected virtual void PushNearbyEnemies()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius)
            .Where(collider => collider.TryGetComponent(out Monster monster) && monster.gameObject.activeInHierarchy)
            .ToArray();

            foreach (var collider in colliders)
            {
                if (collider.TryGetComponent(out Monster monster))
                {
                    monster.Shove(explosionForce, 0.5f);
                }
            }
        }

        public virtual void Resurrect()
        {
            // HP를 최대치로 복원하고 죽음 상태 해제
            _isDead = false;
            RestoreHpToMax();
        }

        public virtual void RestoreHpToMax()
        {
            _currentHp = _currentMaxHp;
            GameEventSystem.Publish(new HitEvent(_currentHp, _currentMaxHp, this, transform.position, 0));//체력 UI 복원 이벤트 재사용
        }

        /// <summary>최대 체력의 ratio(0~1) 만큼 체력을 회복한다. (업그레이드 선택 시 일부 회복용)</summary>
        public virtual void RestoreHpByRatio(float ratio)
        {
            if (_isDead || ratio <= 0f) return;
            _currentHp = Mathf.Clamp(_currentHp + _currentMaxHp * ratio, 0f, _currentMaxHp);
            GameEventSystem.Publish(new HitEvent(_currentHp, _currentMaxHp, this, transform.position, 0));//체력 UI 복원 이벤트 재사용
        }

        public virtual bool TryUseSkill() => _skillModule.TryUse();

        public void ApplyTimedStat(StatType type, float percent, float duration)
            => _skillModule.ApplyTimedStat(type, percent, duration);

        protected virtual void Update()
        {
            _skillModule.Tick(Time.deltaTime);
        }

        protected virtual void OnDestroy()
        {
            _skillModule.Dispose();
        }

        private void _StartDamageFlash()
        {
            if (_spriteRenderers == null || _spriteRenderers.Length == 0) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);

            _flashCoroutine = StartCoroutine(_FlashRoutine());
        }

        private IEnumerator _FlashRoutine()
        {
            if (_flashMaterial == null)
                _flashMaterial = Resources.Load<Material>("SpriteRed");

            if (_flashMaterial == null) yield break;

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                    _spriteRenderers[i].sharedMaterial = _flashMaterial;
            }

            yield return new WaitForSecondsRealtime(0.15f);

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                    _spriteRenderers[i].sharedMaterial = _originalMaterials[i];
            }

            _flashCoroutine = null;
        }

        public virtual void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            int currentLevel = CurrentLevel; // 업그레이드 전 레벨
            _currentLevel++;

            // Train 초기 레벨은 -1, upgradeStats 배열은 0부터 시작
            // View 표시 및 업그레이드 적용 시: 레벨 + 1 인덱스 사용
            // 레벨 -1이면 인덱스 0, 레벨 0이면 인덱스 1
            int upgradeLevelIndex = currentLevel + 1;
            var statusUpgrade = upgradeData.GetStatusUpgrade(upgradeLevelIndex);
            _currentMaxHp += statusUpgrade.MaxHp;
            _currentHp += statusUpgrade.MaxHp;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);

            // 공격 모듈 스탯 업그레이드(currentLevel = 증가 전 레벨 = prevLevel).
            _attackModule?.ApplyUpgrade(upgradeData, currentLevel);

            // 업그레이드가 부여하는 패시브 등록(포탑 업그레이드만 ID 반환). _skillModule은 Train 소유.
            var grantedPassiveId = upgradeData.GetPassiveSkillDataId(currentLevel + 1);
            if (!string.IsNullOrEmpty(grantedPassiveId))
            {
                var passiveData = DatabaseManager.Instance?.GetDB()?.TrainSkillDataDB?.trainPassiveSkillDataList
                    ?.Find(s => s != null && s.Id == grantedPassiveId);
                if (passiveData != null) _skillModule.RegisterPassiveFromData(passiveData);
            }

            GameEventSystem.Publish(new TrainLevelUpEvent(this, _currentLevel));
        }

        public virtual void StatusUpgrade(TrainStatusData upgradeData)
        {
            _currentMaxHp += upgradeData.MaxHp;
            _currentHp += upgradeData.MaxHp;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
        }

        public virtual void StatusUpgrade(TurretTrainStatus upgradeData)
        {

        }

        public virtual void StatusUpgrade(RangeTrainStatus upgradeData)
        {

        }

        public virtual void ApplyStats(IStat[] stats)
        {
            if (stats == null || stats.Length == 0) return;

            foreach (var stat in stats)
            {
                ApplyStat(stat);
            }
        }

        public virtual void ApplyStatsLevelAware(IStat[] stats, int newLevel, int prevLevel = 0)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
                ApplyStatLevelAware(stat, newLevel, prevLevel);
        }

        protected virtual void ApplyStatLevelAware(IStat stat, int newLevel, int prevLevel)
        {
            if (stat == null) return;
            // 공격 스탯이면 모듈이 처리. 처리됐으면 종료, 아니면 공통(HP) 처리.
            if (_attackModule != null && _attackModule.ApplyAttackStatLevelAware(stat, newLevel, prevLevel)) return;
            switch (stat.Type)
            {
                case StatType.MaxHp:
                {
                    float baseMaxHp = _trainData.TrainStatusData.MaxHp;
                    float percent = stat.Value / 100f;
                    float delta = baseMaxHp * percent * (newLevel - prevLevel);
                    _currentMaxHp += delta;
                    _currentHp += delta;
                    _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
                    break;
                }
            }
        }

        public virtual void ApplyStatsByCurrentValue(IStat[] stats)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
            {
                if (stat == null) continue;
                // 현재값 기준 처리는 모듈이 우선, 미처리 타입은 기본 적용(base+모듈)으로 폴백.
                if (_attackModule != null && _attackModule.ApplyAttackStatByCurrentValue(stat)) continue;
                ApplyStat(stat);
            }
        }

        public virtual void ApplyPassiveSkills()
        {
            var passives = _trainData?.PassiveSkillDatas;
            if (passives == null) return;
            // 삼중택일은 픽한 스킬 1개만 부여. (_IsPassiveApplied 공용 규칙)
            foreach (var p in passives)
                if (p != null && _IsPassiveApplied(p.Id))
                    _skillModule.RegisterPassiveFromData(p);
        }

        /// <summary>
        /// 기차 하위에 부착된 채 풀로 반환되지 않는 공격 투사체(범위 공격·화염 파티클 등)를 정리한다.
        /// ResourceManager.ReturnAll은 persistent(기차)의 자식을 건너뛰므로, 상점 진입 시 잔류 투사체를 따로 비울 때 호출한다.
        /// </summary>
        public virtual void ClearAttachedProjectiles() => _attackModule?.ClearAttachedProjectiles();

        // 마스크에 따른 패시브 적용 규칙 (서브클래스 ApplyPassiveSkills·Detail 표시 공용).
        // Active 픽 = 패시브 미적용, Passive 픽 = 선택한 1개만, None(일반 스폰) = 전부.
        protected bool _IsPassiveApplied(string passiveId)
        {
            if (_skillTypeMask == TrainChoiceSkillType.Active)
                return false;

            if (_skillTypeMask == TrainChoiceSkillType.Passive)
                return passiveId == _selectedSkillId;

            return true;
        }

        // 이 인스턴스에 실제로 적용된 스킬 표시 정보. Detail 팝업이 전체 스킬 대신 이걸 사용한다.
        public virtual IEnumerable<(bool isActive, string name, string description)> GetAppliedSkillDisplays()
        {
            if (_trainData == null)
                yield break;

            if (_skillModule.HasActiveSkill)
            {
                var active = _trainData.TrainSkillData;
                if (active != null)
                    yield return (true, active.Name, active.Description);
            }

            var passives = _trainData.PassiveSkillDatas;
            if (passives == null)
                yield break;

            foreach (var passive in passives)
                if (passive != null && _IsPassiveApplied(passive.Id))
                    yield return (false, passive.Name, passive.Description);
        }

        public virtual string GetStatSummary()
            => _attackModule != null ? $"{_attackModule.GetStatSummary()} | MaxHp={_currentMaxHp}" : $"MaxHp={_currentMaxHp}";

        // 공격 간격(초)을 공격 속도(초당 횟수)로 변환. 0 이하면 0.
        protected static float ToAttackSpeed(float interval) => interval > 0f ? 1f / interval : 0f;

        public virtual (string label, string value)[] GetStatDetails()
        {
            var hp = (TrainDefense.Localize.LocalizeHelper.GetByKey("Detail_HP", "HP"), $"{Mathf.RoundToInt(_currentMaxHp)}");
            if (_attackModule == null) return new[] { hp };
            var attack = _attackModule.GetStatDetailLines();
            var result = new (string label, string value)[attack.Length + 1];
            result[0] = hp;
            attack.CopyTo(result, 1);
            return result;
        }

        protected virtual void ApplyStat(IStat stat)
        {
            if (stat == null) return;

            switch (stat.Type)
            {
                case StatType.MaxHp:
                    {
                        float baseMaxHp = _trainData.TrainStatusData.MaxHp;
                        float deltaHp = UtilMath.AccumulateIntDelta(ref _statMaxHpAccum, baseMaxHp * stat.Value / 100f);
                        _currentMaxHp += deltaHp;
                        _currentHp += deltaHp;
                        _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
                        break;
                    }
            }

            // 공격 스탯은 모듈이 처리(HP 외 타입).
            _attackModule?.ApplyAttackStat(stat);
        }

        // Elite 교체(MainTrain.ReplaceTrain) 시 기존 트레인의 누적 강화(영구 + 카드)를 새 인스턴스에 승계.
        // delta 방식: newCurrent = newBase + (oldCurrent - oldBase). HP는 비율 보존.
        // 서브클래스는 base 호출 후 자기 status struct delta를 직접 옮긴다.
        public virtual void CopyProgressFrom(Train source)
        {
            if (source == null || source._trainData == null || _trainData == null) return;

            _currentLevel = source._currentLevel;

            float oldBaseMaxHp = source._trainData.TrainStatusData.MaxHp;
            float maxHpDelta = source._currentMaxHp - oldBaseMaxHp;
            float hpRatio = source._currentMaxHp > 0 ? source._currentHp / source._currentMaxHp : 1f;

            _currentMaxHp += maxHpDelta;
            _currentHp = Mathf.Clamp(_currentMaxHp * hpRatio, 0, _currentMaxHp);

            _statMaxHpAccum = source._statMaxHpAccum;

            // 공격 모듈 진행도 이관(서로 같은 종류 모듈일 때만 내부에서 처리).
            _attackModule?.CopyProgressFrom(source._attackModule);
        }

        #region Attack capability facade (공격 모듈로 위임)
        public event Action<Monster> OnAttacked
        {
            add { if (_attackModule is IAttackEvents e) e.OnAttacked += value; }
            remove { if (_attackModule is IAttackEvents e) e.OnAttacked -= value; }
        }

        public bool ForceAttack() => _attackModule is IForceAttacker f && f.ForceAttack();

        // 둔화 제공(범위 둔화 포탑). 모듈이 ISlowProvider면 위임, 아니면 1(둔화 없음).
        public float GetSlowValue() => _attackModule is ISlowProvider sp ? sp.GetSlowValue() : 1f;

        public void SpawnExternalProjectile(Projectile prefab, float radius,
            IProjectileTarget target = null, float damageMul = 1f, float shoveScale = 1f)
            => (_attackModule as IExternalProjectileSpawner)?.SpawnExternalProjectile(prefab, radius, target, damageMul, shoveScale);
        #endregion
    }
}
