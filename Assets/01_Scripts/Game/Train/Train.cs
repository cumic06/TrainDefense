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
    public abstract class Train : MonoBehaviour, ITrainable, IProjectileTarget
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
        // 상점 업그레이드의 스탯별 누적 배율. 중복선택(Upgrade)으로 더하는 flat 증가분에도 이 배율을 곱해
        // "(base + 중복선택합) × (1 + 상점%)" 가 강화 순서와 무관하게 성립하도록 한다.
        protected readonly Dictionary<StatType, float> _shopMultiplier = new();
        protected TrainChoiceSkillType _skillTypeMask = TrainChoiceSkillType.None;
        protected string _selectedSkillId = null;
        protected bool _initialized;
        private static Material _flashMaterial;
        private static Material _grayMaterial;
        private SpriteRenderer[] _spriteRenderers;
        private Material[] _originalMaterials;
        private Collider2D[] _colliders;
        private Coroutine _flashCoroutine;

        // ApplyStat 퍼센트 누적 손실 방지용 fractional accumulator (UtilMath.AccumulateIntDelta 참조)
        private float _statMaxHpAccum;

        [HideInInspector]
        public bool IsUnDead;

        public string Id => id;
        public TrainData TrainData => _trainData;
        public bool IsMainTrain => _trainData.IsMainTrain;
        public bool IsRotateTurret => isRotateTurret;

        public bool IsDead => _isDead;
        // 레벨 = 받은 업그레이드 횟수(획득 0, 만렙 = upgradeStats 개수 8).
        public int CurrentLevel => _currentLevel;
        // 업그레이드 7번을 받은 포탑부터 엘리트 승격 가능.
        public const int ELITE_PROMOTION_LEVEL = 7;
        public bool IsEliteEligible => _currentLevel >= ELITE_PROMOTION_LEVEL;
        public bool HasActiveSkill => _skillModule.HasActiveSkill;
        public Sprite SkillIcon => _skillModule.SkillIcon;
        public float SkillCooldown => _skillModule.SkillCooldown;
        public bool CanUseSkill => _skillModule.CanUse;
        public float SkillCooldownRatio => _skillModule.CooldownRatio;
        public float SkillRemainingCooldown => _skillModule.RemainingCooldown;
        public float CurrentHpRatio => _currentMaxHp > 0f ? _currentHp / _currentMaxHp : 0f;

        // 현재(업그레이드 반영) 공격 사거리. 서브클래스에서 실제 스탯으로 오버라이드.
        public virtual float CurrentAttackRange => 0f;

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

            _colliders = GetComponentsInChildren<Collider2D>(true);
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

            // 영구 업그레이드 + 스킬트리: 포탑 최대 체력 % 증가 (레벨당 %, 메인 기차는 무적이라 제외)
            if (!(this is MainTrain))
            {
                float maxHpPercent = 0f;

                if (PermanentUpgradeManager.Instance != null)
                    maxHpPercent += PermanentUpgradeManager.Instance.GetValue(PermanentUpgradeType.MaxHp);

                if (SkillTreeManager.Instance != null)
                    maxHpPercent += SkillTreeManager.Instance.GetValue(SkillTreePassiveType.MaxHp);

                _currentMaxHp *= 1f + maxHpPercent / 100f;
            }

            _currentHp = _currentMaxHp;
            _currentLevel = 0;
            _skillModule.Initialize(this, _trainData, _skillTypeMask);
        }

        public Transform TargetTransform => transform;
        public bool IsActive => !IsDead;

        public virtual Transform GetSkillSpawnPoint(int index)
        {
            return transform;
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

        // 사망 시 사라지지 않고 회색(흑백) 머티리얼로 전환. 콜라이더도 꺼서 적·물리 상호작용을 멈춘다.
        public void ApplyDeadVisual()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            if (_grayMaterial == null)
                _grayMaterial = Resources.Load<Material>("Custom_Sprite_Grayscale");

            if (_spriteRenderers != null && _grayMaterial != null)
            {
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    if (_spriteRenderers[i] != null)
                        _spriteRenderers[i].sharedMaterial = _grayMaterial;
                }
            }

            _SetCollidersEnabled(false);
        }

        // 부활 시 원래 머티리얼·콜라이더를 복원한다.
        public void RestoreVisual()
        {
            if (_spriteRenderers != null && _originalMaterials != null)
            {
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    if (_spriteRenderers[i] != null && i < _originalMaterials.Length)
                        _spriteRenderers[i].sharedMaterial = _originalMaterials[i];
                }
            }

            _SetCollidersEnabled(true);
        }

        private void _SetCollidersEnabled(bool isEnabled)
        {
            if (_colliders == null)
                return;

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                    _colliders[i].enabled = isEnabled;
            }
        }

        public virtual void Resurrect()
        {
            // HP를 최대치로 복원하고 죽음 상태 해제
            _isDead = false;
            RestoreHpToMax();
            RestoreVisual();
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

        /// <summary>현재 체력을 최대 체력의 ratio(0~1)로 '설정'한다. (긴급 수리로 부활시킨 직후 HP를 일정 비율로 맞출 때 사용)</summary>
        public virtual void SetHpToRatio(float ratio)
        {
            if (_isDead) return;
            _currentHp = Mathf.Clamp(_currentMaxHp * ratio, 0f, _currentMaxHp);
            GameEventSystem.Publish(new HitEvent(_currentHp, _currentMaxHp, this, transform.position, 0));//체력 UI 복원 이벤트 재사용
        }

        public virtual bool TryUseSkill() => _skillModule.TryUse();

        public void ApplyTimedStat(StatType type, float percent, float duration)
            => _skillModule.ApplyTimedStat(type, percent, duration);

        /// <summary>상점 진입 시 활성 시한버프를 즉시 해제하고 액티브 스킬 쿨타임을 초기화한다. (MainTrain.OnInspectionStart에서 호출)</summary>
        public void ResetSkillStateForInspection() => _skillModule.ResetForInspection();

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

        protected float GetShopMultiplier(StatType type) => _shopMultiplier.TryGetValue(type, out var multiplier) ? multiplier : 1f;
        protected void AccumulateShopMultiplier(StatType type, float ratio) => _shopMultiplier[type] = GetShopMultiplier(type) * ratio;

        // 상점 누적 배율 승계(엘리트 전환 후에도 중복선택 flat이 올바른 배율을 받도록).
        protected void InheritShopMultipliers(Train source)
        {
            _shopMultiplier.Clear();
            foreach (var pair in source._shopMultiplier) _shopMultiplier[pair.Key] = pair.Value;
        }

        public virtual void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            int currentLevel = CurrentLevel; // 업그레이드 전 레벨
            _currentLevel++;

            // 레벨 = 받은 업그레이드 횟수(획득 0, 만렙 = upgradeStats 개수).
            // 다음에 적용할 upgradeStats 인덱스 = 업그레이드 전 레벨.
            int upgradeLevelIndex = currentLevel;
            var statusUpgrade = upgradeData.GetStatusUpgrade(upgradeLevelIndex);
            _currentMaxHp += statusUpgrade.MaxHp;
            _currentHp += statusUpgrade.MaxHp;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);

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
            ApplyStats(stats);
        }

        public virtual void ApplyPassiveSkills() { }

        /// <summary>
        /// 기차 하위에 부착된 채 풀로 반환되지 않는 공격 투사체(범위 공격·화염 파티클 등)를 정리한다.
        /// ResourceManager.ReturnAll은 persistent(기차)의 자식을 건너뛰므로, 상점 진입 시 잔류 투사체를 따로 비울 때 호출한다.
        /// </summary>
        public virtual void ClearAttachedProjectiles() { }

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

        public virtual string GetStatSummary() => $"MaxHp={_currentMaxHp}";

        public virtual (string label, string value)[] GetStatDetails()
            => new[] { (TrainDefense.Localize.LocalizeHelper.GetByKey("Detail_HP", "HP"), $"{Mathf.RoundToInt(_currentMaxHp)}") };

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
        }
    }
}
