using System;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using System.Collections;
using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game
{
    public class RangeTrain : Train, ITrainable, ISlowProvider
    {
        #region Fields
        private RangeTrainData rangeTrainData => _trainData as RangeTrainData;

        // 한 번씩 터지는 범위 포탑(냉기)은 TurretTrain처럼 발사 때 모델을 펀치한다. 비우면 펀치 없음.
        [SerializeField]
        private GameObject turretModel;
        #endregion

        protected RangeTrainStatus _currentRangeTrainStatus;
        protected Projectile _rangeProjectilePrefab;
        // AttackArea(데이터값)를 prefab의 base 콜라이더 radius로 나눠 localScale에 적용해야 월드 반경과 일치.
        private float _baseColliderRadius = 1f;
        private Coroutine _rangeAttackCoroutine;
        private float _attackCountdown;

        // ApplyStat 퍼센트 누적 손실 방지용 fractional accumulator (UtilMath.AccumulateIntDelta 참조)
        private float _statAttackDamageAccum;

        private bool _suppressMainProjectileShove;
        private Vector3 _turretmodelScale;

        // 매 프레임 게이트(_UpdateTickLoopSfx)에서 호출되므로, 전환 시점에만 Play/Stop이 나가도록 상태를 기억한다.
        // (TurretTrain은 공격 인터벌 시점에만 Play해서 이런 가드가 필요 없다)
        private bool _isLoopSfxPlaying;

        // 쿨다운이 끝난 뒤 적이 들어올 때까지 매 프레임 범위를 조회하므로 결과 리스트를 재사용한다(GC 할당 방지).
        private readonly List<Collider2D> _areaColliders = new();
        private readonly ContactFilter2D _areaContactFilter = new ContactFilter2D().NoFilter();

        public event Action OnAttacked;

        protected override void Setup()
        {
            base.Setup();
            if (rangeTrainData == null) return;

            // struct 이므로 값 복사가 일어나며, DB 원본은 변경되지 않는다.
            _currentRangeTrainStatus = rangeTrainData.RangeTrainStatus;
            _ApplyPermanentUpgrade();
            _attackCountdown = _currentRangeTrainStatus.AttackInterval;

            if (turretModel != null)
                _turretmodelScale = turretModel.transform.localScale;

            if (TrainData.DamageType == DamageType.Direct) return;

            SpawnRangeProjectile();

            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);

            StopLoopSFX();
        }

        private void _OnInspectionStart(InspectionStartEvent _) => StopLoopSFX();

        private void _OnEngageReady(EngageReadyEvent _) => ClearAttachedProjectiles();

        // 기차 하위에 부착된 범위 공격 투사체를 풀로 반환한다. (상점 진입/전투 준비 시 잔류 투사체 정리)
        public override void ClearAttachedProjectiles()
        {
            if (_rangeProjectilePrefab == null) return;

            ResourceManager.Instance.Destroy(_rangeProjectilePrefab.gameObject);
            _rangeProjectilePrefab = null;
        }

        private void _OnEngageStart(EngageStartEvent _)
        {
            if (_isDead) return;

            if (_rangeProjectilePrefab == null)
                SpawnRangeProjectile();
        }

        private void PlayLoopSFX()
        {
            if (_isLoopSfxPlaying) return;
            if (rangeTrainData == null || rangeTrainData.AttackSoundType == SoundType.None) return;

            _isLoopSfxPlaying = true;
            SoundManager.Instance.PlaySFX(rangeTrainData.AttackSoundType, true);
        }

        private void StopLoopSFX()
        {
            if (!_isLoopSfxPlaying) return;
            if (rangeTrainData == null || rangeTrainData.AttackSoundType == SoundType.None) return;

            _isLoopSfxPlaying = false;
            SoundManager.Instance.StopSFX(rangeTrainData.AttackSoundType);
        }

        protected override void Update()
        {
            base.Update();
            if (_isDead)
            {
                StopLoopSFX();
                return;
            }
            RangeAttackHandler();
            _UpdateTickLoopSfx();
        }

        // 틱(지속형) 포탑의 공격 루프 사운드를 실제 공격 중(범위 안에 틱 대상이 있을 때)에만 재생한다.
        private void _UpdateTickLoopSfx()
        {
            if (TrainData == null || TrainData.DamageType == DamageType.Direct) return;

            bool attacking = _rangeProjectilePrefab != null
                             && _rangeProjectilePrefab.gameObject.activeInHierarchy
                             && _rangeProjectilePrefab.HasTickTargets;
            if (attacking)
                PlayLoopSFX();
            else
                StopLoopSFX();
        }

        private void RangeAttackHandler()
        {
            if (_attackCountdown <= 0)
            {
                // 범위 안에 적이 없으면 쏘지 않고 기다린다. 적이 없을 때 눈·폭발만 혼자 반짝이고 사라지던 문제.
                if (!_HasMonsterInArea()) return;

                if (rangeTrainData.RangeProjectilePrefab != null)
                {
                    if (_rangeProjectilePrefab == null)
                    {
                        SpawnRangeProjectile();
                    }

                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.gameObject.SetActive(true);
                        _rangeProjectilePrefab.SuppressShoveEffect = _suppressMainProjectileShove;
                    }

                    if (rangeTrainData.AttackSoundType != SoundType.None && TrainData.DamageType == DamageType.Direct)
                    {
                        SoundManager.Instance.PlaySFX(rangeTrainData.AttackSoundType);
                    }

                    _attackCountdown = _currentRangeTrainStatus.AttackInterval;
                    OnAttacked?.Invoke();

                    if (turretModel != null)
                        TurretCombatFx.PlayAttackPunch(turretModel.transform, _turretmodelScale);

                    if (TrainData.DamageType == DamageType.Direct)
                    {
                        if (_rangeAttackCoroutine != null)
                        {
                            StopCoroutine(_rangeAttackCoroutine);
                        }
                        _rangeAttackCoroutine = StartCoroutine(RangeProjectileCoroutine(1f));
                    }
                    else
                    {
                        // 버스트(냉기): 장판을 지속시간 동안 켰다가 끄고, 그 뒤부터 쿨다운이 흐른다(총 주기 = 지속시간 + 간격).
                        float burstDuration = _GetBurstDuration();
                        if (burstDuration > 0f)
                        {
                            _attackCountdown = burstDuration + _currentRangeTrainStatus.AttackInterval;
                            if (_rangeAttackCoroutine != null)
                            {
                                StopCoroutine(_rangeAttackCoroutine);
                            }
                            _rangeAttackCoroutine = StartCoroutine(RangeProjectileCoroutine(burstDuration));
                        }
                    }
                }
            }
            else
            {
                _attackCountdown -= Time.deltaTime;
            }
        }

        private bool _HasMonsterInArea()
        {
            Physics2D.OverlapCircle(transform.position, _currentRangeTrainStatus.AttackArea, _areaContactFilter, _areaColliders);
            foreach (var areaCollider in _areaColliders)
            {
                if (areaCollider.TryGetComponent<Monster>(out _))
                    return true;
            }

            return false;
        }

        // activeDuration초 동안 장판을 켜둔 뒤 끈다. (Direct = 기존 1초 유지, 버스트 냉기 = BurstDuration)
        private IEnumerator RangeProjectileCoroutine(float activeDuration)
        {
            yield return new WaitForSeconds(activeDuration);

            if (_rangeProjectilePrefab != null)
            {
                _rangeProjectilePrefab.gameObject.SetActive(false);
            }
        }

        public void SetSuppressMainProjectileShove(bool suppress)
        {
            _suppressMainProjectileShove = suppress;
            if (_rangeProjectilePrefab != null)
                _rangeProjectilePrefab.SuppressShoveEffect = suppress;
        }

        public void SpawnExternalProjectile(Projectile prefab, float radius, IProjectileTarget target = null, float damageMul = 1f, float shoveScale = 1f)
        {
            if (prefab == null) return;
            var spawned = ResourceManager.Instance.Spawn(prefab, transform.position, Quaternion.identity);
            if (spawned == null) return;
            float r = radius >= 0f ? radius : _currentRangeTrainStatus.AttackArea;
            if (target != null) spawned.transform.LookAt2D(target.TargetTransform);
            int damage = Mathf.RoundToInt(_currentRangeTrainStatus.AttackDamage * damageMul);
            spawned.ShoveScale = shoveScale;
            spawned.Init(
                damage,
                this,
                target,
                r,
                _currentRangeTrainStatus.CriticalChance,
                _currentRangeTrainStatus.CriticalDamage);
        }

        private void SpawnRangeProjectile()
        {
            if (rangeTrainData.RangeProjectilePrefab != null)
            {
                // 기존 Projectile 제거
                if (_rangeProjectilePrefab != null)
                {
                    ResourceManager.Instance.Destroy(_rangeProjectilePrefab.gameObject);
                    _rangeProjectilePrefab = null;
                }
                Projectile projectile = rangeTrainData.RangeProjectilePrefab?.GetComponent<Projectile>();

                if (projectile != null)
                {
                    _rangeProjectilePrefab = ResourceManager.Instance.Spawn(projectile);
                    _rangeProjectilePrefab.transform.SetParent(transform);
                    var circle = _rangeProjectilePrefab.GetComponentInChildren<CircleCollider2D>();
                    _baseColliderRadius = circle != null && circle.radius > 0f ? circle.radius : 1f;
                    // ExpandingWave는 자체 확장 코루틴이 localScale을 제어하므로 여기서 스케일하면 소환 직후 한 프레임 깜빡인다.
                    if (_rangeProjectilePrefab is not ExpandingWave)
                    {
                        float scale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                        _rangeProjectilePrefab.transform.localScale = new Vector3(scale, scale, 1);
                    }
                    _rangeProjectilePrefab.transform.localPosition = Vector3.zero;
                    _rangeProjectilePrefab.transform.localRotation = Quaternion.identity;
                    _rangeProjectilePrefab.SuppressShoveEffect = _suppressMainProjectileShove;
                    _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);

                    // 버스트(냉기)는 발동 시점에 켜므로, 깔아둔 장판은 꺼둔 채 대기한다.
                    if (_GetBurstDuration() > 0f)
                        _rangeProjectilePrefab.gameObject.SetActive(false);
                }
            }
        }

        // 버스트 지속시간. 포탑 데이터의 BurstDuration이 base(냉기 5초)이고 업그레이드 누적이 가산된다.
        // base 0 = 비버스트 포탑(강화 규칙도 없어 항상 0). 장판이 없으면 켤 대상이 없으므로 0.
        private float _GetBurstDuration()
        {
            if (_rangeProjectilePrefab == null)
                return 0f;

            return _currentRangeTrainStatus.BurstDuration;
        }

        public override void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            int currentLevel = CurrentLevel; // 업그레이드 전 레벨 저장
            base.Upgrade(upgradeData);

            // RangeTrain 전용 업그레이드 데이터가 있다면 적용
            // 다음에 적용할 upgradeStats 인덱스 = 업그레이드 전 레벨 (레벨 = 받은 업그레이드 횟수)
            if (upgradeData is RangeTrainUpgradeData rangeUpgradeData)
            {
                int upgradeLevelIndex = currentLevel;
                var rangeStatus = rangeUpgradeData.GetRangeStatusUpgrade(upgradeLevelIndex);
                _AddUpgradeStatus(ref _currentRangeTrainStatus, rangeStatus);

                if (_rangeProjectilePrefab != null)
                {
                    float scale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                    _rangeProjectilePrefab.transform.localScale = new Vector3(scale, scale, 1);
                    _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                }
            }
        }

        // 영구(메타) 업그레이드 + 스킬트리의 TurretStat 보너스를 base 스탯에 가산한다. (struct라 값 복사 후 직접 가산)
        private void _ApplyPermanentUpgrade()
        {
            _currentRangeTrainStatus = _GetMetaAppliedStatus(_currentRangeTrainStatus);
            _metaBaseStatus = _currentRangeTrainStatus;
        }

        // 상점 미리보기(아직 없는 포탑)도 생성 직후 값을 보여줘야 해서 인스턴스 없이 계산할 수 있게 static으로 둔다.
        private static RangeTrainStatus _GetMetaAppliedStatus(RangeTrainStatus status)
        {
            var manager = PermanentUpgradeManager.Instance;

            if (manager != null)
            {
                status.AttackDamage += manager.GetBonus(StatType.AttackDamage);
                status.AttackRange += manager.GetBonus(StatType.AttackRange);
                status.AttackArea += manager.GetBonus(StatType.AttackArea);
                status.AttackInterval += manager.GetBonus(StatType.AttackInterval);
                status.CriticalChance += manager.GetBonus(StatType.CriticalChance);
                status.CriticalDamage += manager.GetBonus(StatType.CriticalDamage);
                status.AttackCount += Mathf.RoundToInt(manager.GetBonus(StatType.AttackCount));
                status.SlowRate += manager.GetBonus(StatType.SlowRate);
            }

            var skillTreeManager = SkillTreeManager.Instance;

            if (skillTreeManager != null)
            {
                // 공격력·사거리·범위·간격은 기본값에 % 곱 (포탑마다 기본값이 달라 정수 가산은 저기본 포탑만 유리), 나머지는 포인트 가산
                status.AttackDamage *= skillTreeManager.GetMultiplier(StatType.AttackDamage);
                status.AttackRange *= skillTreeManager.GetMultiplier(StatType.AttackRange);
                status.AttackArea *= skillTreeManager.GetMultiplier(StatType.AttackArea);
                status.AttackInterval *= skillTreeManager.GetMultiplier(StatType.AttackInterval);
                status.CriticalChance += skillTreeManager.GetBonus(StatType.CriticalChance);
                status.CriticalDamage += skillTreeManager.GetBonus(StatType.CriticalDamage);
                status.AttackCount += Mathf.RoundToInt(skillTreeManager.GetBonus(StatType.AttackCount));
                status.SlowRate += skillTreeManager.GetBonus(StatType.SlowRate);
            }

            return status;
        }

        // 엘리트 승격 승계 규칙: newCurrent = newBase + (sourceCurrent - sourceBase). CopyProgressFrom과 상점 승격 카드 미리보기가 같이 쓴다.
        private static RangeTrainStatus _GetInheritedStatus(RangeTrainStatus newBase, RangeTrainStatus sourceBase, RangeTrainStatus sourceCurrent)
        {
            return new RangeTrainStatus
            {
                AttackDamage = newBase.AttackDamage + (sourceCurrent.AttackDamage - sourceBase.AttackDamage),
                AttackRange = newBase.AttackRange + (sourceCurrent.AttackRange - sourceBase.AttackRange),
                AttackArea = newBase.AttackArea + (sourceCurrent.AttackArea - sourceBase.AttackArea),
                AttackCount = newBase.AttackCount + (sourceCurrent.AttackCount - sourceBase.AttackCount),
                AttackInterval = newBase.AttackInterval + (sourceCurrent.AttackInterval - sourceBase.AttackInterval),
                CriticalChance = newBase.CriticalChance + (sourceCurrent.CriticalChance - sourceBase.CriticalChance),
                CriticalDamage = newBase.CriticalDamage + (sourceCurrent.CriticalDamage - sourceBase.CriticalDamage),
                SlowRate = newBase.SlowRate + (sourceCurrent.SlowRate - sourceBase.SlowRate),
                BurstDuration = newBase.BurstDuration + (sourceCurrent.BurstDuration - sourceBase.BurstDuration),
            };
        }

        // 몬스터가 완전히 멈추면 무한 생존이 되므로 이속을 최소 10%는 남긴다.
        // 상점 카드는 점근 곡선이라 100%를 못 넘지만, 패시브(현재값 곱셈)·영구업글·스킬트리 가산이
        // 곡선 밖에서 더해지면 넘을 수 있어 소비 지점에서 한 번에 막는다.
        private const float MAX_TOTAL_SLOW_RATE = 90f;

        // 누적 둔화율(%, 기차 base + 강화)을 둔화 배율로 변환.
        public float GetSlowValue()
        {
            return 1f - Mathf.Min(_currentRangeTrainStatus.SlowRate, MAX_TOTAL_SLOW_RATE) / 100f;
        }

        public override void StatusUpgrade(RangeTrainStatus upgradeData)
        {
            _currentRangeTrainStatus.AttackRange += upgradeData.AttackRange;
            _currentRangeTrainStatus.AttackArea += upgradeData.AttackArea;
            _currentRangeTrainStatus.AttackDamage += upgradeData.AttackDamage;
            _currentRangeTrainStatus.AttackCount += upgradeData.AttackCount;
            _currentRangeTrainStatus.AttackInterval += upgradeData.AttackInterval;
            _currentRangeTrainStatus.CriticalChance += upgradeData.CriticalChance;
            _currentRangeTrainStatus.CriticalDamage += upgradeData.CriticalDamage;
            _currentRangeTrainStatus.SlowRate += upgradeData.SlowRate;
            _currentRangeTrainStatus.BurstDuration += upgradeData.BurstDuration;

            if (_rangeProjectilePrefab != null)
            {
                float scale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                _rangeProjectilePrefab.transform.localScale = new Vector3(scale, scale, 1);
                _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
            }
        }

        public override float CurrentAttackRange => _currentRangeTrainStatus.AttackRange;
        // 레인지 포탑은 사거리가 아닌 공격 범위(자기 위치 중심 원)를 표시한다.
        public override float RangeIndicatorRadius => _currentRangeTrainStatus.AttackArea;

        public override float GetCurrentStatValue(StatType statType)
        {
            var status = _currentRangeTrainStatus;
            switch (statType)
            {
                case StatType.AttackRange: return status.AttackRange;
                case StatType.AttackDamage: return status.AttackDamage;
                case StatType.AttackCount: return status.AttackCount;
                case StatType.AttackInterval: return status.AttackInterval;
                case StatType.CriticalChance: return status.CriticalChance;
                case StatType.CriticalDamage: return status.CriticalDamage;
                case StatType.AttackArea: return status.AttackArea;
                case StatType.SlowRate: return status.SlowRate;
                case StatType.BurstDuration: return status.BurstDuration;
                default: return base.GetCurrentStatValue(statType);
            }
        }

        public override void RestoreAttackInterval(float attackInterval) => _currentRangeTrainStatus.AttackInterval = attackInterval;
        public RangeTrainStatus BaseStatus => rangeTrainData.RangeTrainStatus;
        /// <summary>메타까지 적용된 판 시작 스탯. 상점 카드 증가량의 기준값 (TurretTrain.MetaBaseStatus와 동일한 역할).</summary>
        public RangeTrainStatus MetaBaseStatus => _metaBaseStatus;
        private RangeTrainStatus _metaBaseStatus;

        public override string GetStatSummary() =>
            $"DMG={_currentRangeTrainStatus.AttackDamage} | RANGE={_currentRangeTrainStatus.AttackRange} | AREA={_currentRangeTrainStatus.AttackArea} | MaxHp={_currentMaxHp}";

        public override (string label, string value)[] GetStatDetails()
            => _BuildStatDetails(_currentMaxHp, _currentRangeTrainStatus);

        // 상점 강화 카드 롱프레스용 — upgradeData를 지금 받으면 될 스탯(Upgrade와 같은 _AddUpgradeStatus로 계산).
        public override (string label, string value)[] GetUpgradePreviewStatDetails(ITrainUpgradeData upgradeData)
        {
            if (upgradeData is not RangeTrainUpgradeData rangeUpgradeData)
                return base.GetUpgradePreviewStatDetails(upgradeData);

            var status = _currentRangeTrainStatus;
            _AddUpgradeStatus(ref status, rangeUpgradeData.GetRangeStatusUpgrade(CurrentLevel));
            return _BuildStatDetails(GetUpgradedMaxHp(upgradeData), status);
        }

        private void _AddUpgradeStatus(ref RangeTrainStatus status, RangeTrainStatus upgrade)
        {
            status.AttackRange += upgrade.AttackRange;
            status.AttackArea += upgrade.AttackArea;
            status.AttackDamage += upgrade.AttackDamage;
            status.AttackCount += upgrade.AttackCount;
            status.AttackInterval += upgrade.AttackInterval;
            status.CriticalChance += upgrade.CriticalChance;
            status.CriticalDamage += upgrade.CriticalDamage;
            status.SlowRate += upgrade.SlowRate;
            status.BurstDuration += upgrade.BurstDuration;
        }

        // 상점 카드 롱프레스용 — 새 포탑은 생성 직후 값(영구 강화 + 판 중 전체 강화), 승격은 promotionSource의 누적 강화를 승계한 값.
        // (Train.GetPreviewStatDetails가 부른다. runUpgrades = MainTrain.GetExistingTrainUpgrades)
        public static (string label, string value)[] GetPreviewStatDetails(RangeTrainData data, float maxHp, RangeTrain promotionSource,
            List<(IStat[] stats, int level)> runUpgrades)
        {
            if (promotionSource != null && promotionSource.rangeTrainData != null)
                return _BuildStatDetails(maxHp, _GetInheritedStatus(data.RangeTrainStatus,
                    promotionSource.rangeTrainData.RangeTrainStatus, promotionSource._currentRangeTrainStatus));

            // 생성 순서와 같게: 영구 강화(Setup) → 판 중 전체 강화(MainTrain.ApplyExistingUpgradesToTrain)
            var status = _GetMetaAppliedStatus(data.RangeTrainStatus);
            var metaBaseStatus = status;
            foreach (var (stats, level) in runUpgrades)
                foreach (var stat in stats)
                    if (stat != null)
                        _TryApplyLevelAware(ref status, data.RangeTrainStatus, metaBaseStatus, stat, level, 0);

            return _BuildStatDetails(maxHp, status);
        }

        private static (string label, string value)[] _BuildStatDetails(float maxHp, RangeTrainStatus status)
        {
            System.Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            var details = new System.Collections.Generic.List<(string label, string value)>
            {
                (L("Detail_HP", "HP"), $"{Mathf.RoundToInt(maxHp)}"),
                (L("Detail_Damage", "공격력"), $"{Mathf.RoundToInt(status.AttackDamage)}"),
                (L("Detail_Range", "사거리"), $"{status.AttackRange:F1}"),
                (L("Detail_Area", "범위"), $"{status.AttackArea:F1}"),
                (L("Detail_Speed", "공격속도"), $"{status.AttackInterval:F2}"),
            };

            // 분사 지속시간은 버스트 포탑(냉기)만 표시 — 비버스트는 base가 0이라 값 판정으로 충분.
            if (status.BurstDuration > 0f)
                details.Add((L("Detail_BurstDuration", "지속시간"), $"{status.BurstDuration:F1}"));

            // 둔화율은 둔화 포탑(냉기)만 표시 — 상점 둔화율 강화 카드가 변화를 보여 줄 줄. 라벨은 도감과 같은 키.
            if (status.SlowRate > 0f)
                details.Add((L("Collection_Slow", "둔화"), $"{status.SlowRate:F0}%"));

            details.Add((L("Detail_CritChance", "크리티컬 확률"), $"{status.CriticalChance:F0}%"));
            details.Add((L("Detail_CritDamage", "크리티컬 데미지"), $"+{Projectile.BaseCriticalDamagePercent + status.CriticalDamage:F0}%"));
            return details.ToArray();
        }

        public override void ApplyPassiveSkills()
        {
            var passives = rangeTrainData?.PassiveSkillDatas;
            if (passives == null) return;
            // 삼중택일은 픽한 스킬 1개만 부여한다: Active 픽=패시브 미적용, Passive 픽=선택 1개,
            // None(일반 스폰)=기본 키트 전부. (Train._IsPassiveApplied 공용 규칙)
            foreach (var p in passives)
                if (_IsPassiveApplied(p.Id))
                    _skillModule.RegisterPassiveFromData(p);
        }

        public override void ApplyStatsByCurrentValue(IStat[] stats)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
            {
                if (stat == null) continue;
                float percent = stat.Value / 100f;
                switch (stat.Type)
                {
                    case StatType.AttackRange:
                        _currentRangeTrainStatus.AttackRange += _currentRangeTrainStatus.AttackRange * percent;
                        break;
                    case StatType.AttackArea:
                        _currentRangeTrainStatus.AttackArea += _currentRangeTrainStatus.AttackArea * percent;
                        if (_rangeProjectilePrefab != null)
                        {
                            float areaScale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                            _rangeProjectilePrefab.transform.localScale = new Vector3(areaScale, areaScale, 1f);
                        }
                        break;
                    case StatType.AttackDamage:
                        _currentRangeTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, _currentRangeTrainStatus.AttackDamage * percent);
                        if (_rangeProjectilePrefab != null)
                            _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                        break;
                    case StatType.AttackInterval:
                        // 공속은 상점과 동일하게 현재값 기준 역수 곱셈(DPS 선형, 0 이하 방지). percent 음수=공속 증가.
                        _currentRangeTrainStatus.AttackInterval *= 1f / (1f + (-percent));
                        break;
                    case StatType.SlowRate:
                        // 둔화율은 이미 %라 적힌 값을 %p 그대로 더한다(15 → 30%에서 45%). 상점 둔화 카드와 같은 방식.
                        _currentRangeTrainStatus.SlowRate += stat.Value;
                        break;
                    default:
                        ApplyStat(stat);
                        break;
                }
            }
        }

        protected override void ApplyStat(IStat stat)
        {
            base.ApplyStat(stat);
            if (stat == null) return;

            var baseStatus = rangeTrainData.RangeTrainStatus;
            float percent = stat.Value / 100f;
            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentRangeTrainStatus.AttackRange += baseStatus.AttackRange * percent;
                    break;

                case StatType.AttackArea:
                    _currentRangeTrainStatus.AttackArea += baseStatus.AttackArea * percent;

                    if (_rangeProjectilePrefab != null)
                    {
                        float areaScale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                        _rangeProjectilePrefab.transform.localScale = new Vector3(areaScale, areaScale, 1f);
                    }
                    break;

                case StatType.AttackDamage:
                    _currentRangeTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, stat.Value * rangeTrainData.AttackDamageMultiplier);
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                    }
                    break;

                // 정수 스탯(공격 횟수)은 % 아니라 flat +N (TurretTrain과 동일)
                case StatType.AttackCount:
                    _currentRangeTrainStatus.AttackCount += Mathf.RoundToInt(stat.Value);
                    break;

                case StatType.AttackInterval:
                    _currentRangeTrainStatus.AttackInterval += baseStatus.AttackInterval * percent;
                    break;

                case StatType.CriticalChance:
                    _currentRangeTrainStatus.CriticalChance += stat.Value;
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                    }
                    break;

                case StatType.CriticalDamage:
                    _currentRangeTrainStatus.CriticalDamage += stat.Value;
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                    }
                    break;
            }
        }

        public override void ApplyStatsLevelAware(IStat[] stats, int newLevel, int prevLevel = 0)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
                ApplyStatLevelAware(stat, newLevel, prevLevel);
        }

        protected override void ApplyStatLevelAware(IStat stat, int newLevel, int prevLevel)
        {
            if (stat == null) return;

            // 스탯 계산은 상점 미리보기와 같이 쓰는 _TryApplyLevelAware가 하고, 여기서는 투사체 갱신만 챙긴다.
            if (!_TryApplyLevelAware(ref _currentRangeTrainStatus, rangeTrainData.RangeTrainStatus, _metaBaseStatus, stat, newLevel, prevLevel))
            {
                base.ApplyStatLevelAware(stat, newLevel, prevLevel);
                return;
            }

            switch (stat.Type)
            {
                case StatType.AttackArea:
                    if (_rangeProjectilePrefab != null)
                    {
                        float areaScale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                        _rangeProjectilePrefab.transform.localScale = new Vector3(areaScale, areaScale, 1f);
                    }
                    break;

                case StatType.AttackDamage:
                    if (_rangeProjectilePrefab != null)
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null,
                            _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance,
                            _currentRangeTrainStatus.CriticalDamage);
                    break;

                case StatType.CriticalChance:
                case StatType.CriticalDamage:
                    if (_rangeProjectilePrefab != null)
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null,
                            _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance,
                            _currentRangeTrainStatus.CriticalDamage);
                    break;
            }
        }

        // 판 중 전체 강화(레벨 누적)의 스탯 변화. 실제 적용(ApplyStatLevelAware)과 상점 새 포탑 미리보기가 같이 쓴다.
        // 상점 카드처럼 메타 적용 기본값(metaBaseStatus) 기준으로 더한다. 이 포탑이 다루지 않는 스탯이면 false.
        private static bool _TryApplyLevelAware(ref RangeTrainStatus status, RangeTrainStatus baseStatus, RangeTrainStatus metaBaseStatus, IStat stat,
            int newLevel, int prevLevel)
        {
            float percent = stat.Value / 100f;
            int times = newLevel - prevLevel;

            switch (stat.Type)
            {
                case StatType.AttackRange:
                    status.AttackRange += metaBaseStatus.AttackRange * percent * times;
                    return true;

                case StatType.AttackArea:
                    status.AttackArea += metaBaseStatus.AttackArea * percent * times;
                    return true;

                case StatType.AttackDamage:
                    status.AttackDamage += metaBaseStatus.AttackDamage * percent * times;
                    return true;

                case StatType.AttackCount:
                {
                    int newTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * newLevel);
                    int oldTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * prevLevel);
                    status.AttackCount += newTot - oldTot;
                    return true;
                }

                case StatType.AttackInterval:
                    // 값 −5 = 발사 속도 +5%. 상점 공속 카드와 같은 배율에 더한다.
                    status.AttackInterval = GetAttackIntervalAfterSpeedBonus(metaBaseStatus.AttackInterval, status.AttackInterval, -percent * times);
                    return true;

                case StatType.CriticalChance:
                    status.CriticalChance += stat.Value * times;
                    return true;

                case StatType.CriticalDamage:
                    status.CriticalDamage += stat.Value * times;
                    return true;

                default:
                    return false;
            }
        }

        public override void CopyProgressFrom(Train source)
        {
            base.CopyProgressFrom(source);
            if (source is not RangeTrain sourceRange) return;
            if (sourceRange.rangeTrainData == null || rangeTrainData == null) return;

            _currentRangeTrainStatus = _GetInheritedStatus(rangeTrainData.RangeTrainStatus,
                sourceRange.rangeTrainData.RangeTrainStatus, sourceRange._currentRangeTrainStatus);

            _statAttackDamageAccum = sourceRange._statAttackDamageAccum;


            if (_rangeProjectilePrefab != null)
            {
                float scale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                _rangeProjectilePrefab.transform.localScale = new Vector3(scale, scale, 1f);
                _rangeProjectilePrefab.Init(
                    _currentRangeTrainStatus.AttackDamage,
                    this,
                    null,
                    _currentRangeTrainStatus.AttackArea,
                    _currentRangeTrainStatus.CriticalChance,
                    _currentRangeTrainStatus.CriticalDamage);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (rangeTrainData != null)
            {
                Gizmos.DrawWireSphere(transform.position, rangeTrainData.RangeTrainStatus.AttackRange);
            }
        }
    }
}
