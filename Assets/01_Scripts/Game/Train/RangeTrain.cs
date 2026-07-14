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
        #endregion

        protected RangeTrainStatus _currentRangeTrainStatus;
        protected Projectile _rangeProjectilePrefab;
        // AttackArea(데이터값)를 prefab의 base 콜라이더 radius로 나눠 localScale에 적용해야 월드 반경과 일치.
        private float _baseColliderRadius = 1f;
        private Coroutine _rangeAttackCoroutine;
        private float _attackCountdown;

        // ApplyStat 퍼센트 누적 손실 방지용 fractional accumulator (UtilMath.AccumulateIntDelta 참조)
        private float _statAttackDamageAccum;
        private float _statAttackCountAccum;

        // 상점 업그레이드의 스탯별 누적 배율. 중복선택(Upgrade)으로 더하는 flat 증가분에도 이 배율을 곱해
        // "(base + 중복선택합) × (1 + 상점%)" 가 강화 순서와 무관하게 성립하도록 한다.
        private readonly Dictionary<StatType, float> _shopMultiplier = new();
        private float _ShopMul(StatType type) => _shopMultiplier.TryGetValue(type, out var m) ? m : 1f;
        private void _MulShop(StatType type, float ratio) => _shopMultiplier[type] = _ShopMul(type) * ratio;

        private bool _suppressMainProjectileShove;

        // 매 프레임 게이트(_UpdateTickLoopSfx)에서 호출되므로, 전환 시점에만 Play/Stop이 나가도록 상태를 기억한다.
        // (TurretTrain은 공격 인터벌 시점에만 Play해서 이런 가드가 필요 없다)
        private bool _isLoopSfxPlaying;

        public event Action OnAttacked;

        protected override void Setup()
        {
            base.Setup();
            if (rangeTrainData == null) return;

            // struct 이므로 값 복사가 일어나며, DB 원본은 변경되지 않는다.
            _currentRangeTrainStatus = rangeTrainData.RangeTrainStatus;
            _ApplyPermanentUpgrade();
            _attackCountdown = _currentRangeTrainStatus.AttackInterval;

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

        // activeDuration초 동안 장판을 켜둔 뒤 끈다. (Direct = 기존 1초 유지, 버스트 냉기 = BurstDuration)
        private IEnumerator RangeProjectileCoroutine(float activeDuration)
        {
            yield return new WaitForSeconds(activeDuration);

            if (_rangeProjectilePrefab != null)
            {
                _rangeProjectilePrefab.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 스킬 InstantAttack용: 쿨다운을 기다리지 않고 즉시 1회 공격을 강제한다. (TurretTrain.ForceAttack 대칭)
        /// </summary>
        public bool ForceAttack()
        {
            if (_isDead) return false;
            if (rangeTrainData == null || rangeTrainData.RangeProjectilePrefab == null) return false;
            _attackCountdown = 0f;
            RangeAttackHandler();
            return true;
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

        // 버스트 지속시간. 장판 투사체 config에 BurstDuration이 설정된 포탑(냉기)만 > 0.
        private float _GetBurstDuration()
        {
            if (_rangeProjectilePrefab == null)
                return 0f;

            ProjectileData projectileData = _rangeProjectilePrefab.GetData();
            return projectileData != null ? projectileData.BurstDuration : 0f;
        }

        public override void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            int currentLevel = CurrentLevel; // 업그레이드 전 레벨 저장
            base.Upgrade(upgradeData);

            // RangeTrain 전용 업그레이드 데이터가 있다면 적용
            // Train 초기 레벨은 -1, upgradeStats 배열은 0부터 시작
            // 업그레이드 적용 시: 업그레이드 전 레벨 + 1 인덱스 사용
            if (upgradeData is RangeTrainUpgradeData rangeUpgradeData)
            {
                int upgradeLevelIndex = currentLevel + 1;
                var rangeStatus = rangeUpgradeData.GetRangeStatusUpgrade(upgradeLevelIndex);
                // 중복선택 증가분은 base와 동일하게 상점 배율을 받는다(Model B): (base+중복선택)×(1+상점%).
                _currentRangeTrainStatus.AttackRange += rangeStatus.AttackRange * _ShopMul(StatType.AttackRange);
                _currentRangeTrainStatus.AttackArea += rangeStatus.AttackArea * _ShopMul(StatType.AttackArea);
                _currentRangeTrainStatus.AttackDamage += rangeStatus.AttackDamage * _ShopMul(StatType.AttackDamage);
                _currentRangeTrainStatus.AttackCount += rangeStatus.AttackCount;
                _currentRangeTrainStatus.AttackInterval += rangeStatus.AttackInterval * _ShopMul(StatType.AttackInterval);
                _currentRangeTrainStatus.CriticalChance += rangeStatus.CriticalChance;
                _currentRangeTrainStatus.CriticalDamage += rangeStatus.CriticalDamage;
                _currentRangeTrainStatus.SlowRate += rangeStatus.SlowRate;

                if (_rangeProjectilePrefab != null)
                {
                    float scale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                    _rangeProjectilePrefab.transform.localScale = new Vector3(scale, scale, 1);
                    _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                }
            }
        }

        // 영구(메타) 업그레이드 중 TurretStat 보너스를 base 스탯에 가산한다. (struct라 값 복사 후 직접 가산)
        private void _ApplyPermanentUpgrade()
        {
            var manager = PermanentUpgradeManager.Instance;
            if (manager == null) return;

            _currentRangeTrainStatus.AttackDamage += manager.GetBonus(StatType.AttackDamage);
            _currentRangeTrainStatus.AttackRange += manager.GetBonus(StatType.AttackRange);
            _currentRangeTrainStatus.AttackArea += manager.GetBonus(StatType.AttackArea);
            _currentRangeTrainStatus.AttackInterval += manager.GetBonus(StatType.AttackInterval);
            _currentRangeTrainStatus.CriticalChance += manager.GetBonus(StatType.CriticalChance);
            _currentRangeTrainStatus.CriticalDamage += manager.GetBonus(StatType.CriticalDamage);
            _currentRangeTrainStatus.AttackCount += Mathf.RoundToInt(manager.GetBonus(StatType.AttackCount));
            _currentRangeTrainStatus.SlowRate += manager.GetBonus(StatType.SlowRate);
        }

        // 누적 둔화율(%, 기차 base + 강화)을 둔화 배율로 변환.
        public float GetSlowValue()
        {
            return 1f - _currentRangeTrainStatus.SlowRate / 100f;
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
        public RangeTrainStatus BaseStatus => rangeTrainData.RangeTrainStatus;

        public override string GetStatSummary() =>
            $"DMG={_currentRangeTrainStatus.AttackDamage} | RANGE={_currentRangeTrainStatus.AttackRange} | AREA={_currentRangeTrainStatus.AttackArea} | MaxHp={_currentMaxHp}";

        public override (string label, string value)[] GetStatDetails()
        {
            System.Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            return new[]
            {
                (L("Detail_HP", "HP"), $"{Mathf.RoundToInt(_currentMaxHp)}"),
                (L("Detail_Damage", "공격력"), $"{Mathf.RoundToInt(_currentRangeTrainStatus.AttackDamage)}"),
                (L("Detail_Range", "사거리"), $"{_currentRangeTrainStatus.AttackRange:F1}"),
                (L("Detail_Area", "범위"), $"{_currentRangeTrainStatus.AttackArea:F1}"),
                (L("Detail_Speed", "공격속도"), $"{_currentRangeTrainStatus.AttackInterval:F2}"),
                (L("Detail_CritChance", "크리티컬 확률"), $"{_currentRangeTrainStatus.CriticalChance:F0}%"),
                (L("Detail_CritDamage", "크리티컬 데미지"), $"+{Projectile.BaseCriticalDamagePercent + _currentRangeTrainStatus.CriticalDamage:F0}%"),
            };
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
                            _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                        break;
                    case StatType.AttackInterval:
                        // 공속은 상점과 동일하게 현재값 기준 역수 곱셈(DPS 선형, 0 이하 방지). percent 음수=공속 증가.
                        _currentRangeTrainStatus.AttackInterval *= 1f / (1f + (-percent));
                        break;
                    case StatType.SlowRate:
                        // 둔화율(%)을 현재값 기준 percent 증가
                        _currentRangeTrainStatus.SlowRate += _currentRangeTrainStatus.SlowRate * percent;
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
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                    }
                    break;

                case StatType.AttackCount:
                    _currentRangeTrainStatus.AttackCount += UtilMath.AccumulateIntDelta(ref _statAttackCountAccum, baseStatus.AttackCount * percent);
                    break;

                case StatType.AttackInterval:
                    _currentRangeTrainStatus.AttackInterval += baseStatus.AttackInterval * percent;
                    break;

                case StatType.CriticalChance:
                    _currentRangeTrainStatus.CriticalChance += stat.Value;
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                    }
                    break;

                case StatType.CriticalDamage:
                    _currentRangeTrainStatus.CriticalDamage += stat.Value;
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
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

            var baseStatus = rangeTrainData.RangeTrainStatus;
            float percent = stat.Value / 100f;
            int times = newLevel - prevLevel;

            switch (stat.Type)
            {
                case StatType.AttackRange:
                {
                    float shopRatio = (1f + percent * newLevel) / (1f + percent * prevLevel);
                    _currentRangeTrainStatus.AttackRange *= shopRatio;
                    _MulShop(StatType.AttackRange, shopRatio);
                    break;
                }

                case StatType.AttackArea:
                {
                    float shopRatio = (1f + percent * newLevel) / (1f + percent * prevLevel);
                    _currentRangeTrainStatus.AttackArea *= shopRatio;
                    _MulShop(StatType.AttackArea, shopRatio);
                    if (_rangeProjectilePrefab != null)
                    {
                        float areaScale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                        _rangeProjectilePrefab.transform.localScale = new Vector3(areaScale, areaScale, 1f);
                    }
                    break;
                }

                case StatType.AttackDamage:
                {
                    float shopRatio = (1f + percent * newLevel) / (1f + percent * prevLevel);
                    _currentRangeTrainStatus.AttackDamage *= shopRatio;
                    _MulShop(StatType.AttackDamage, shopRatio);
                    if (_rangeProjectilePrefab != null)
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null,
                            _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance,
                            _currentRangeTrainStatus.CriticalDamage);
                    break;
                }

                case StatType.AttackCount:
                {
                    int newTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * newLevel);
                    int oldTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * prevLevel);
                    _currentRangeTrainStatus.AttackCount += newTot - oldTot;
                    break;
                }

                case StatType.AttackInterval:
                {
                    float shopRatio = (1f + (-percent) * prevLevel) / (1f + (-percent) * newLevel);
                    _currentRangeTrainStatus.AttackInterval *= shopRatio;
                    _MulShop(StatType.AttackInterval, shopRatio);
                    break;
                }

                case StatType.CriticalChance:
                    _currentRangeTrainStatus.CriticalChance += stat.Value * times;
                    if (_rangeProjectilePrefab != null)
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null,
                            _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance,
                            _currentRangeTrainStatus.CriticalDamage);
                    break;

                case StatType.CriticalDamage:
                    _currentRangeTrainStatus.CriticalDamage += stat.Value * times;
                    if (_rangeProjectilePrefab != null)
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null,
                            _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance,
                            _currentRangeTrainStatus.CriticalDamage);
                    break;

                default:
                    base.ApplyStatLevelAware(stat, newLevel, prevLevel);
                    break;
            }
        }

        public override void CopyProgressFrom(Train source)
        {
            base.CopyProgressFrom(source);
            if (source is not RangeTrain srcRange) return;
            if (srcRange.rangeTrainData == null || rangeTrainData == null) return;

            var srcBase = srcRange.rangeTrainData.RangeTrainStatus;
            var srcCurrent = srcRange._currentRangeTrainStatus;
            var newBase = rangeTrainData.RangeTrainStatus;

            _currentRangeTrainStatus.AttackRange = newBase.AttackRange + (srcCurrent.AttackRange - srcBase.AttackRange);
            _currentRangeTrainStatus.AttackArea = newBase.AttackArea + (srcCurrent.AttackArea - srcBase.AttackArea);
            _currentRangeTrainStatus.AttackDamage = newBase.AttackDamage + (srcCurrent.AttackDamage - srcBase.AttackDamage);
            _currentRangeTrainStatus.AttackCount = newBase.AttackCount + (srcCurrent.AttackCount - srcBase.AttackCount);
            _currentRangeTrainStatus.AttackInterval = newBase.AttackInterval + (srcCurrent.AttackInterval - srcBase.AttackInterval);
            _currentRangeTrainStatus.CriticalChance = newBase.CriticalChance + (srcCurrent.CriticalChance - srcBase.CriticalChance);
            _currentRangeTrainStatus.CriticalDamage = newBase.CriticalDamage + (srcCurrent.CriticalDamage - srcBase.CriticalDamage);
            _currentRangeTrainStatus.SlowRate = newBase.SlowRate + (srcCurrent.SlowRate - srcBase.SlowRate);

            _statAttackDamageAccum = srcRange._statAttackDamageAccum;
            _statAttackCountAccum = srcRange._statAttackCountAccum;

            // 상점 누적 배율도 그대로 승계(엘리트 전환 후에도 중복선택 flat이 올바른 배율을 받도록).
            _shopMultiplier.Clear();
            foreach (var kv in srcRange._shopMultiplier) _shopMultiplier[kv.Key] = kv.Value;

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
