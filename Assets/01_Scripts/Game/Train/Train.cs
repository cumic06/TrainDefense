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
        // 상점에서 이 포탑의 각 스탯을 얼마나 강화했는지(기본 증가량 1단위 = 1.0, 상품 등급 배수만큼 누적).
        // 범위처럼 "누적량에서 현재값을 역산해야 하는" 스탯이 스킬 보정과 섞여도
        // 정확한 증가분을 낼 수 있게 누적량을 따로 센다.
        protected readonly Dictionary<StatType, float> _statUpgradeAmount = new();
        // 상점에서 산 강화 카드 등급의 합(1등급 +1 … 5등급 +5). 레벨은 장수라 고등급 카드도 1로 세서 개조 조건은 이걸로 본다.
        protected int _upgradeGradeSum;
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
        // 포탑 만렙 = upgradeStats 슬롯 수 규격. (업그레이드 데이터 배열 길이와 함께 바꿔야 함)
        public const int MAX_LEVEL = 8;

        /// <summary>런 세이브용 — 이 기차에 부여된 삼중택일 스킬 종류 마스크.</summary>
        public TrainChoiceSkillType SkillTypeMask => _skillTypeMask;

        /// <summary>런 세이브용 — 부여받은 액티브 스킬 id(없으면 null).</summary>
        public string SelectedSkillId => _selectedSkillId;
        // 상점 강화 카드 등급의 합이 10 이상인 포탑부터 엘리트 승격(개조) 가능.
        public const int ELITE_PROMOTION_GRADE_SUM = 10;
        public bool IsEliteEligible => _upgradeGradeSum >= ELITE_PROMOTION_GRADE_SUM;
        public int UpgradeGradeSum => _upgradeGradeSum;

        public void AddUpgradeGradeSum(int grade) => _upgradeGradeSum += grade;
        public float CurrentHpRatio => _currentMaxHp > 0f ? _currentHp / _currentMaxHp : 0f;

        // 현재(업그레이드 반영) 공격 사거리. 서브클래스에서 실제 스탯으로 오버라이드.
        public virtual float CurrentAttackRange => 0f;

        // 사거리 표시 원의 반지름. 기본은 공격 사거리, 레인지 포탑은 공격 범위(AttackArea)로 오버라이드.
        public virtual float RangeIndicatorRadius => CurrentAttackRange;

        // 현재(업그레이드 반영) 스탯값. 엘리트 승격 조건 판정용 — 서브클래스가 실제 스탯으로 오버라이드.
        public virtual float GetCurrentStatValue(StatType statType) => statType == StatType.MaxHp ? _currentMaxHp : 0f;

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
                _skillModule.Initialize(this);
                return;
            }

            _currentMaxHp = _trainData.TrainStatusData.MaxHp;

            // 영구 업그레이드 + 스킬트리: 포탑 최대 체력 % 증가 (레벨당 %, 메인 기차는 무적이라 제외)
            if (!(this is MainTrain))
                _currentMaxHp = GetMetaMaxHp(_currentMaxHp);

            _currentHp = _currentMaxHp;
            _currentLevel = 0;
            _upgradeGradeSum = 0;
            _skillModule.Initialize(this);
        }

        public Transform TargetTransform => transform;
        public bool IsActive => !IsDead;

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

            // 스킬 트리 방어력: 모든 피해원에 같은 비율로 감소 (메인 기차는 오버라이드에서 무적 처리)
            if (SkillTreeManager.Instance != null)
                damage *= 1f - SkillTreeManager.Instance.GetValue(SkillTreePassiveType.DamageReduction) / 100f;

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

        /// <summary>최대 체력의 ratio(0~1) 만큼 체력을 회복한다. (자가 복구 패시브·긴급 수리용)</summary>
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

        /// <summary>상점 진입 시 전투 중 바뀐 비주얼(포탑 조준 각도 등)을 기본 상태로 되돌린다. (MainTrain.OnInspectionStart에서 호출)</summary>
        public virtual void ResetVisualForInspection() { }

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

        // 공격 속도 강화는 상점 카드·레벨업 모두 발사 속도 배율(= 기본 간격 ÷ 지금 간격)에 %를 더한다. 간격 = 기본 ÷ 배율.
        // 지금 간격에서 배율을 역산하므로 상점과 레벨업을 어떤 순서로 받아도 간격 = 기본 ÷ (1 + 상점% + 레벨업%)가 된다.
        public static float GetAttackIntervalAfterSpeedBonus(float baseInterval, float currentInterval, float speedBonus)
            => baseInterval / (baseInterval / currentInterval + speedBonus);

        // 개조 패시브가 공격 속도를 낮출 때(speedBonus 음수) 배율이 0 이하로 떨어져 간격이 무한·음수가 되지 않게 하는 하한.
        private const float MIN_PASSIVE_SPEED_MULTIPLIER = 0.1f;

        // 개조 패시브의 공격 속도 — 상점 공격 속도 카드와 같은 배율 덧셈. 기본 간격 기준이라 개조 시점과 상관없이 같은 양이 바뀐다.
        protected static float _GetIntervalAfterPassiveSpeed(float baseInterval, float currentInterval, float speedBonus)
        {
            float speedMultiplier = Mathf.Max(MIN_PASSIVE_SPEED_MULTIPLIER, baseInterval / currentInterval + speedBonus);
            return baseInterval / speedMultiplier;
        }

        // 이어하기 복원용 — 공격 간격은 강화 순서에 따라 증가량이 달라 저장된 증가량만으로는 재현되지 않아 최종값을 직접 되돌린다.
        public virtual void RestoreAttackInterval(float attackInterval) { }

        public float GetStatUpgradeAmount(StatType type) => _statUpgradeAmount.TryGetValue(type, out var amount) ? amount : 0f;

        public void AddStatUpgradeAmount(StatType type, float amount) => _statUpgradeAmount[type] = GetStatUpgradeAmount(type) + amount;

        /// <summary>런 세이브용 — 스탯별 누적 강화량 전체. (복원은 AddStatUpgradeAmount로 되돌린다)</summary>
        public IReadOnlyDictionary<StatType, float> StatUpgradeAmounts => _statUpgradeAmount;

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
                    float delta = _GetLevelAwareMaxHpDelta(_trainData.TrainStatusData.MaxHp, stat, newLevel, prevLevel);
                    _currentMaxHp += delta;
                    _currentHp += delta;
                    _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
                    break;
                }
            }
        }

        // 판 중 전체 강화(레벨 누적)의 최대 체력 증가량. ApplyStatLevelAware와 상점 새 포탑 미리보기가 같이 쓴다.
        private static float _GetLevelAwareMaxHpDelta(float baseMaxHp, IStat stat, int newLevel, int prevLevel)
            => baseMaxHp * stat.Value / 100f * (newLevel - prevLevel);

        // 개조 패시브 등 % 스탯을 "데이터 기본값 × %"만큼 한 번 더한다(현재 값에 곱하지 않는다 — 개조 시점과 상관없이 같은 양).
        public virtual void ApplyStatsByBaseValue(IStat[] stats)
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
        // Passive 픽 = 선택한 1개만, None(일반 스폰) = 전부.
        protected bool _IsPassiveApplied(string passiveId)
        {
            if (_skillTypeMask == TrainChoiceSkillType.Passive)
                return passiveId == _selectedSkillId;

            return true;
        }

        // 이 인스턴스에 실제로 적용된 스킬 표시 정보. Detail 팝업이 전체 스킬 대신 이걸 사용한다.
        public virtual IEnumerable<(string name, string description)> GetAppliedSkillDisplays()
        {
            if (_trainData == null)
                yield break;

            var passives = _trainData.PassiveSkillDatas;
            if (passives == null)
                yield break;

            foreach (var passive in passives)
                if (passive != null && _IsPassiveApplied(passive.Id))
                    yield return (passive.Name, passive.Description);
        }

        public virtual string GetStatSummary() => $"MaxHp={_currentMaxHp}";

        public virtual (string label, string value)[] GetStatDetails()
            => new[] { (TrainDefense.Localize.LocalizeHelper.GetByKey("Detail_HP", "HP"), $"{Mathf.RoundToInt(_currentMaxHp)}") };

        // 상점 강화 카드 롱프레스용 — upgradeData를 지금 받으면 될 스탯. 포탑은 전용 스탯까지 override한다.
        public virtual (string label, string value)[] GetUpgradePreviewStatDetails(ITrainUpgradeData upgradeData)
            => new[] { (TrainDefense.Localize.LocalizeHelper.GetByKey("Detail_HP", "HP"), $"{Mathf.RoundToInt(GetUpgradedMaxHp(upgradeData))}") };

        // Upgrade가 다음에 적용할 인덱스(= 지금 레벨)의 최대 체력 증가분을 더한 값.
        protected float GetUpgradedMaxHp(ITrainUpgradeData upgradeData)
            => _currentMaxHp + upgradeData.GetStatusUpgrade(CurrentLevel).MaxHp;

        // 영구 업그레이드 + 스킬트리의 포탑 최대 체력 % 증가를 적용한 값. 상점 미리보기(아직 없는 포탑)도 같은 계산을 쓴다.
        public static float GetMetaMaxHp(float baseMaxHp)
        {
            float maxHpPercent = 0f;

            if (PermanentUpgradeManager.Instance != null)
                maxHpPercent += PermanentUpgradeManager.Instance.GetValue(PermanentUpgradeType.MaxHp);

            if (SkillTreeManager.Instance != null)
                maxHpPercent += SkillTreeManager.Instance.GetValue(SkillTreePassiveType.MaxHp);

            return baseMaxHp * (1f + maxHpPercent / 100f);
        }

        /// <summary>
        /// 아직 편성에 없는 포탑의 스탯 표기(상점 카드 롱프레스용).
        /// 새 포탑 카드는 생성 직후 값(base + 영구 강화·스킬 트리 + 판 중 전체 강화), 승격 카드는 promotionSource의
        /// 누적 강화를 승계한 값이다 — 실제 생성·승격(Setup·ApplyExistingUpgradesToTrain·CopyProgressFrom)과 같은 규칙으로 계산한다.
        /// 단, 생성 뒤 붙는 패시브 스킬의 스탯 변화는 들어가지 않는다.
        /// </summary>
        public static (string label, string value)[] GetPreviewStatDetails(TrainData trainData, Train promotionSource)
        {
            if (trainData == null)
                return System.Array.Empty<(string, string)>();

            // 승격은 원본의 현재 스탯에 판 중 강화가 이미 들어 있어 따로 적용하지 않는다(MainTrain.ReplaceTrain과 동일).
            var runUpgrades = promotionSource == null
                ? MainTrain.GetExistingTrainUpgrades()
                : new List<(IStat[] stats, int level)>();

            float baseMaxHp = trainData.TrainStatusData.MaxHp;
            float maxHp;

            if (promotionSource != null && promotionSource._trainData != null)
            {
                maxHp = baseMaxHp + (promotionSource._currentMaxHp - promotionSource._trainData.TrainStatusData.MaxHp);
            }
            else
            {
                maxHp = GetMetaMaxHp(baseMaxHp);
                foreach (var (stats, level) in runUpgrades)
                    foreach (var stat in stats)
                        if (stat != null && stat.Type == StatType.MaxHp)
                            maxHp += _GetLevelAwareMaxHpDelta(baseMaxHp, stat, level, 0);
            }

            if (trainData is TurretTrainData turretTrainData)
                return TurretTrain.GetPreviewStatDetails(turretTrainData, maxHp, promotionSource as TurretTrain, runUpgrades);

            if (trainData is RangeTrainData rangeTrainData)
                return RangeTrain.GetPreviewStatDetails(rangeTrainData, maxHp, promotionSource as RangeTrain, runUpgrades);

            return new[] { (TrainDefense.Localize.LocalizeHelper.GetByKey("Detail_HP", "HP"), $"{Mathf.RoundToInt(maxHp)}") };
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
        }

        // Elite 교체(MainTrain.ReplaceTrain) 시 기존 트레인의 누적 강화(영구 + 카드)를 새 인스턴스에 승계.
        // delta 방식: newCurrent = newBase + (oldCurrent - oldBase). HP는 비율 보존.
        // 서브클래스는 base 호출 후 자기 status struct delta를 직접 옮긴다.
        public virtual void CopyProgressFrom(Train source)
        {
            if (source == null || source._trainData == null || _trainData == null) return;

            _currentLevel = source._currentLevel;
            _upgradeGradeSum = source._upgradeGradeSum;

            float oldBaseMaxHp = source._trainData.TrainStatusData.MaxHp;
            float maxHpDelta = source._currentMaxHp - oldBaseMaxHp;
            float hpRatio = source._currentMaxHp > 0 ? source._currentHp / source._currentMaxHp : 1f;

            _currentMaxHp += maxHpDelta;
            _currentHp = Mathf.Clamp(_currentMaxHp * hpRatio, 0, _currentMaxHp);

            _statMaxHpAccum = source._statMaxHpAccum;

            // 스탯별 누적 강화량도 승계 — 안 옮기면 승격 후 범위 구매가 1회차 증가분(가장 큰 폭)부터
            // 다시 시작하고 사거리·공격 횟수 상한도 풀린다. (스탯값 자체는 서브클래스가 delta로 옮긴다)
            _statUpgradeAmount.Clear();
            foreach (var pair in source._statUpgradeAmount) _statUpgradeAmount[pair.Key] = pair.Value;
        }
    }
}
