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
        // 포탑 소유 장판 인스턴스들. 한 번씩 터지는 파동은 공격마다 꺼져 있는 것을 하나 골라 켜므로, 앞 파동이 퍼지는 중에도
        // 새 파동이 겹쳐 나간다(하나만 두고 켜기만 하면 이미 켜진 동안의 공격은 파동도 피해도 없이 사라졌다).
        // 지속형(틱)은 첫 인스턴스 하나를 계속 켜 둔다.
        private readonly List<Projectile> _rangeProjectiles = new();
        // AttackRange(데이터값)를 prefab의 base 콜라이더 radius로 나눠 localScale에 적용해야 월드 반경과 일치.
        // 범위 포탑은 자기 중심 원 전체를 때리므로 사거리가 곧 공격 반경이다(AttackArea는 쓰지 않음).
        private float _baseColliderRadius = 1f;
        private float _attackCountdown;


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

            _SpawnRangeProjectile();

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
            if (ResourceManager.Instance != null)
            {
                foreach (var projectile in _rangeProjectiles)
                {
                    if (projectile == null) continue;
                    // 터렛 소유 등록을 풀어야 공용 풀로 실제 반환된다.
                    ResourceManager.Instance.UnregisterPersistent(projectile.gameObject);
                    ResourceManager.Instance.Destroy(projectile.gameObject);
                }
            }

            _rangeProjectiles.Clear();
        }

        private void _OnEngageStart(EngageStartEvent _)
        {
            if (_isDead) return;

            if (_rangeProjectiles.Count == 0)
                _SpawnRangeProjectile();
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

            // OnDestroy에서도 불린다 — 앱 종료 때는 SoundManager가 먼저 파괴돼 있을 수 있다.
            if (SoundManager.Instance == null) return;

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

            bool attacking = false;
            foreach (var projectile in _rangeProjectiles)
            {
                if (projectile != null && projectile.gameObject.activeInHierarchy && projectile.HasTickTargets)
                {
                    attacking = true;
                    break;
                }
            }

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
                    bool isOneShotWave = TrainData.DamageType == DamageType.Direct;
                    // 한 번씩 터지는 파동은 꺼져 있는 인스턴스를 골라 켠다(켜질 때 파동이 처음부터 다시 시작). 지속형은 첫 인스턴스를 계속 켠다.
                    Projectile projectile = isOneShotWave ? _GetInactiveRangeProjectile() : _GetFirstRangeProjectile();

                    if (projectile != null)
                    {
                        projectile.gameObject.SetActive(true);
                        projectile.SuppressShoveEffect = _suppressMainProjectileShove;
                    }

                    if (rangeTrainData.AttackSoundType != SoundType.None && isOneShotWave)
                    {
                        SoundManager.Instance.PlaySFX(rangeTrainData.AttackSoundType);
                    }

                    _attackCountdown = _currentRangeTrainStatus.AttackInterval;
                    _combatRecord.Attacks++;
                    OnAttacked?.Invoke();

                    if (turretModel != null)
                        TurretCombatFx.PlayAttackPunch(turretModel.transform, _turretmodelScale);

                    if (isOneShotWave && projectile != null)
                        StartCoroutine(_DeactivateRangeProjectile(projectile, 1f));
                }
            }
            else
            {
                _attackCountdown -= Time.deltaTime;
            }
        }

        private bool _HasMonsterInArea()
        {
            Physics2D.OverlapCircle(transform.position, _currentRangeTrainStatus.AttackRange, _areaContactFilter, _areaColliders);
            foreach (var areaCollider in _areaColliders)
            {
                if (areaCollider.TryGetComponent<Monster>(out _))
                    return true;
            }

            return false;
        }

        // activeDuration초 동안 장판을 켜둔 뒤 끈다. (Direct = 1초 유지)
        private IEnumerator _DeactivateRangeProjectile(Projectile projectile, float activeDuration)
        {
            yield return new WaitForSeconds(activeDuration);

            if (projectile != null)
                projectile.gameObject.SetActive(false);
        }

        // 공격력·치명타·사거리가 바뀌면 모든 장판 인스턴스에 다시 넣는다.
        private void _ReinitRangeProjectiles()
        {
            foreach (var projectile in _rangeProjectiles)
            {
                if (projectile != null)
                    projectile.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
            }
        }

        private Projectile _GetFirstRangeProjectile()
        {
            if (_rangeProjectiles.Count == 0)
                _SpawnRangeProjectile();

            return _rangeProjectiles.Count > 0 ? _rangeProjectiles[0] : null;
        }

        private Projectile _GetInactiveRangeProjectile()
        {
            foreach (var projectile in _rangeProjectiles)
            {
                if (projectile != null && !projectile.gameObject.activeSelf)
                    return projectile;
            }

            return _SpawnRangeProjectile();
        }

        // 장판 크기를 사거리에 맞춘다. 크기를 스스로 정하는 투사체(퍼지는 파동)는 건드리지 않는다.
        // (파동은 처음 켜질 때의 크기를 기준으로 퍼지므로, 여기서 바꾸면 소환 직후 한 프레임 깜빡인다)
        private void _ApplyRangeScale()
        {
            float scale = _currentRangeTrainStatus.AttackRange / _baseColliderRadius;

            foreach (var projectile in _rangeProjectiles)
            {
                if (projectile == null || projectile.ControlsOwnScale) continue;

                projectile.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void SetSuppressMainProjectileShove(bool suppress)
        {
            _suppressMainProjectileShove = suppress;

            foreach (var projectile in _rangeProjectiles)
            {
                if (projectile != null)
                    projectile.SuppressShoveEffect = suppress;
            }
        }

        public void SpawnExternalProjectile(Projectile prefab, float radius, IProjectileTarget target = null, float damageMul = 1f, float shoveScale = 1f)
        {
            if (prefab == null) return;
            var spawned = ResourceManager.Instance.Spawn(prefab, transform.position, Quaternion.identity);
            if (spawned == null) return;
            float r = radius >= 0f ? radius : _currentRangeTrainStatus.AttackRange;
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

        // 장판 인스턴스를 하나 더 만들어 목록에 넣는다. 켜진 상태로 만들어지므로 호출한 쪽이 바로 쓰거나 끈다.
        private Projectile _SpawnRangeProjectile()
        {
            if (rangeTrainData.RangeProjectilePrefab == null) return null;

            Projectile prefab = rangeTrainData.RangeProjectilePrefab.GetComponent<Projectile>();
            if (prefab == null) return null;

            Projectile spawned = ResourceManager.Instance.Spawn(prefab);
            if (spawned == null) return null;

            // 포탑이 켜고 끄며 계속 쓰는 장판이라 투사체 자체 수명(DestroyDelay)이 먼저 끝나도 공용 풀로 돌아가지 않고 꺼지기만 하게
            // 터렛 소유로 등록한다(TurretTrain NonMovement 풀과 같은 방식). 풀로 새면 원점에서 터지거나 다른 포탑과 공유된다.
            ResourceManager.Instance.RegisterPersistent(spawned.gameObject);
            spawned.transform.SetParent(transform);
            var circle = spawned.GetComponentInChildren<CircleCollider2D>();
            _baseColliderRadius = circle != null && circle.radius > 0f ? circle.radius : 1f;
            spawned.transform.localPosition = Vector3.zero;
            spawned.transform.localRotation = Quaternion.identity;
            spawned.SuppressShoveEffect = _suppressMainProjectileShove;
            spawned.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
            _rangeProjectiles.Add(spawned);
            _ApplyRangeScale();

            return spawned;
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

                _ApplyRangeScale();
                _ReinitRangeProjectiles();
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
            };
        }

        // 몬스터가 완전히 멈추면 무한 생존이 되므로 이속을 최소 10%는 남긴다. 어떤 최대값도 이 위로는 못 간다.
        private const float MAX_TOTAL_SLOW_RATE = 90f;
        // 둔화율이 강화로 오를 수 있는 기본 최대값(%). 이 값 이상이면 상점이 둔화 카드를 더 내지 않고, 넘기는 강화는 최대값으로 잘린다.
        private const float DEFAULT_MAX_SLOW_RATE = 60f;

        private float _maxSlowRate = DEFAULT_MAX_SLOW_RATE;

        /// <summary>이 포탑의 둔화율 최대값(%). 개조(SlowCapPassive)가 올린다. 완전 정지 방지 상한(90)은 넘지 못한다.</summary>
        public float MaxSlowRate
        {
            get => _maxSlowRate;
            set
            {
                _maxSlowRate = Mathf.Min(value, MAX_TOTAL_SLOW_RATE);
                _ClampSlowRate(ref _currentRangeTrainStatus);
            }
        }

        // 둔화율은 최대값 위로 자라지 않는다 — 넘기는 강화·패시브는 최대값으로 잘린다(상세창도 그 값을 보여 준다).
        private void _ClampSlowRate(ref RangeTrainStatus status)
        {
            status.SlowRate = Mathf.Min(status.SlowRate, _maxSlowRate);
        }

        // 누적 둔화율(%, 기차 base + 강화)을 둔화 배율로 변환.
        public float GetSlowValue()
        {
            return 1f - Mathf.Min(_currentRangeTrainStatus.SlowRate, MAX_TOTAL_SLOW_RATE) / 100f;
        }

        public override float CurrentAttackRange => _currentRangeTrainStatus.AttackRange;

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
                default: return base.GetCurrentStatValue(statType);
            }
        }

        public override void RestoreAttackInterval(float attackInterval) => _currentRangeTrainStatus.AttackInterval = attackInterval;

        // 개조 패시브가 곱한 배율 = 지금 기준값 ÷ 개조 없이 계산한 기준값.
        public override float GetFinalValueMultiplier(StatType statType)
        {
            var unmodifiedStatus = _GetMetaAppliedStatus(rangeTrainData.RangeTrainStatus);
            float unmodifiedValue = statType switch
            {
                StatType.AttackDamage => unmodifiedStatus.AttackDamage,
                StatType.AttackRange => unmodifiedStatus.AttackRange,
                StatType.AttackArea => unmodifiedStatus.AttackArea,
                _ => 0f,
            };
            float currentValue = statType switch
            {
                StatType.AttackDamage => _metaBaseStatus.AttackDamage,
                StatType.AttackRange => _metaBaseStatus.AttackRange,
                StatType.AttackArea => _metaBaseStatus.AttackArea,
                _ => 0f,
            };

            return unmodifiedValue > 0f ? currentValue / unmodifiedValue : 1f;
        }
        public RangeTrainStatus BaseStatus => rangeTrainData.RangeTrainStatus;
        /// <summary>메타까지 적용된 판 시작 스탯. 상점 카드 증가량의 기준값 (TurretTrain.MetaBaseStatus와 동일한 역할).</summary>
        public RangeTrainStatus MetaBaseStatus => _metaBaseStatus;
        private RangeTrainStatus _metaBaseStatus;

        public override string GetStatSummary() =>
            $"DMG={_currentRangeTrainStatus.AttackDamage} | RANGE={_currentRangeTrainStatus.AttackRange} | AREA={_currentRangeTrainStatus.AttackArea} | MaxHp={_currentMaxHp}";

        public override (string label, string value)[] GetStatDetails()
            => _BuildStatDetails(_currentMaxHp, _currentRangeTrainStatus);

        public override (string label, string value)[] GetStatGrowthDetails()
        {
            if (rangeTrainData == null)
                return GetStatDetails();

            return _BuildStatDetails(_currentMaxHp, _currentRangeTrainStatus, TrainData.TrainStatusData.MaxHp, rangeTrainData.RangeTrainStatus);
        }

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
            _ClampSlowRate(ref status);
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

        // baseStatus가 있으면(인게임 롱프레스) 각 값 옆에 데이터 기본값 대비 강화량을 붙인다.
        private static (string label, string value)[] _BuildStatDetails(float maxHp, RangeTrainStatus status,
            float baseMaxHp = 0f, RangeTrainStatus? baseStatus = null)
        {
            System.Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            var b = baseStatus.GetValueOrDefault();
            string G(string text, float delta, int decimals, bool isLowerBetter = false, string suffix = "")
                => baseStatus.HasValue ? AppendGrowthText(text, delta, decimals, isLowerBetter, suffix) : text;

            var details = new System.Collections.Generic.List<(string label, string value)>
            {
                (L("Detail_HP", "HP"), G($"{Mathf.RoundToInt(maxHp)}", maxHp - baseMaxHp, 0)),
                (L("Detail_Damage", "공격력"), G($"{status.AttackDamage:0.#}", status.AttackDamage - b.AttackDamage, 1)),
                (L("Detail_Range", "사거리"), G($"{status.AttackRange:F1}", status.AttackRange - b.AttackRange, 1)),
                (L("Detail_Speed", "공격속도"), G($"{status.AttackInterval:F2}", status.AttackInterval - b.AttackInterval, 2, true)),
            };

            // 둔화율은 둔화 포탑(냉기)만 표시 — 상점 둔화율 강화 카드가 변화를 보여 줄 줄. 라벨은 도감과 같은 키.
            if (status.SlowRate > 0f)
                details.Add((L("Collection_Slow", "둔화"), G($"{status.SlowRate:F0}%", status.SlowRate - b.SlowRate, 0, false, "%")));

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

        // 지금 값과 기준값(MetaBaseStatus)에 같은 배율을 곱한다. 상점·레벨업 증가량이 기준값 × %라서, 개조 뒤 받는 강화에도 배율이 그대로 따라붙는다.
        public override void ApplyStatsToFinalValue(IStat[] stats)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
            {
                if (stat == null) continue;
                float multiplier = PercentToFinalValueMultiplier(stat.Type, stat.Value / 100f);
                switch (stat.Type)
                {
                    case StatType.AttackRange:
                        _currentRangeTrainStatus.AttackRange *= multiplier;
                        _metaBaseStatus.AttackRange *= multiplier;
                        _ApplyRangeScale();
                        break;
                    case StatType.AttackArea:
                        _currentRangeTrainStatus.AttackArea *= multiplier;
                        _metaBaseStatus.AttackArea *= multiplier;
                        break;
                    case StatType.AttackDamage:
                        _currentRangeTrainStatus.AttackDamage *= multiplier;
                        _metaBaseStatus.AttackDamage *= multiplier;
                        _ReinitRangeProjectiles();
                        break;
                    case StatType.AttackInterval:
                        // 기준 간격도 같이 늘려야 상점 공속 카드의 발사 속도 배율(기준 ÷ 지금)이 개조 전과 같게 이어진다.
                        _currentRangeTrainStatus.AttackInterval *= multiplier;
                        _metaBaseStatus.AttackInterval *= multiplier;
                        break;
                    case StatType.SlowRate:
                        // 둔화율은 이미 %라 적힌 값을 %p 그대로 더한다(15 → 30%에서 45%). 상점 둔화 카드와 같은 방식.
                        _currentRangeTrainStatus.SlowRate += stat.Value;
                        _ClampSlowRate(ref _currentRangeTrainStatus);
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

            // 사거리·범위·공격력·공속·둔화율은 ApplyStatsToFinalValue가 처리하고, 나머지만 여기로 온다.
            switch (stat.Type)
            {
                // 정수 스탯(공격 횟수)은 % 아니라 flat +N (TurretTrain과 동일)
                case StatType.AttackCount:
                    _currentRangeTrainStatus.AttackCount += Mathf.RoundToInt(stat.Value);
                    break;

                case StatType.CriticalChance:
                    _currentRangeTrainStatus.CriticalChance += stat.Value;
                    _ReinitRangeProjectiles();
                    break;

                case StatType.CriticalDamage:
                    _currentRangeTrainStatus.CriticalDamage += stat.Value;
                    _ReinitRangeProjectiles();
                    break;
            }
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
                case StatType.AttackRange:
                    _ApplyRangeScale();
                    break;

                case StatType.AttackArea:
                    break;

                case StatType.AttackDamage:
                    _ReinitRangeProjectiles();
                    break;

                case StatType.CriticalChance:
                case StatType.CriticalDamage:
                    _ReinitRangeProjectiles();
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

            _ApplyRangeScale();
            _ReinitRangeProjectiles();
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
