using System;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using System.Collections;
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

        private bool _suppressMainProjectileShove;

        public event Action OnAttacked;

        protected override void Setup()
        {
            base.Setup();
            if (rangeTrainData == null) return;

            // struct 이므로 값 복사가 일어나며, DB 원본은 변경되지 않는다.
            _currentRangeTrainStatus = rangeTrainData.RangeTrainStatus;
            _attackCountdown = _currentRangeTrainStatus.AttackInterval;

            if (TrainData.DamageType == DamageType.Direct) return;

            SpawnRangeProjectile();

            PlayLoopSFX();

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

        private void _OnEngageReady(EngageReadyEvent _)
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

            PlayLoopSFX();
        }

        private void PlayLoopSFX()
        {
            if (rangeTrainData.AttackSoundType == SoundType.None) return;
            SoundManager.Instance.PlaySFX(rangeTrainData.AttackSoundType, true);
        }

        private void StopLoopSFX()
        {
            if (TrainData == null) return;
            if (TrainData.DamageType == DamageType.Direct) return;
            if (rangeTrainData.AttackSoundType == SoundType.None) return;
            SoundManager.Instance.StopSFX(rangeTrainData.AttackSoundType);
        }

        protected override void Update()
        {
            base.Update();
            if (_isDead) return;
            RangeAttackHandler();
        }

        private void RangeAttackHandler()
        {
            if (_attackCountdown <= 0)
            {
                if (rangeTrainData.RangeProjectilePrefab != null)
                {
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.gameObject.SetActive(true);
                        _rangeProjectilePrefab.SuppressShoveEffect = _suppressMainProjectileShove;
                    }
                    else
                    {
                        SpawnRangeProjectile();
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
                        _rangeAttackCoroutine = StartCoroutine(RangeProjectileCoroutine());
                    }
                }
            }
            else
            {
                _attackCountdown -= Time.deltaTime;
            }
        }

        private IEnumerator RangeProjectileCoroutine()
        {
            yield return new WaitForSeconds(rangeTrainData.RangeTrainStatus.AttackInterval / rangeTrainData.RangeTrainStatus.AttackInterval);

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
                }
            }
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
                _currentRangeTrainStatus.AttackRange += rangeStatus.AttackRange;
                _currentRangeTrainStatus.AttackArea += rangeStatus.AttackArea;
                _currentRangeTrainStatus.AttackDamage += rangeStatus.AttackDamage;
                _currentRangeTrainStatus.AttackCount += rangeStatus.AttackCount;
                _currentRangeTrainStatus.AttackInterval += rangeStatus.AttackInterval;
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

        public float CurrentAttackRange => _currentRangeTrainStatus.AttackRange;
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
                (L("Detail_Speed", "공격속도"), $"{_currentRangeTrainStatus.AttackInterval:F2}s"),
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
                    _currentRangeTrainStatus.AttackRange = _currentRangeTrainStatus.AttackRange / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    break;

                case StatType.AttackArea:
                    _currentRangeTrainStatus.AttackArea = _currentRangeTrainStatus.AttackArea / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    if (_rangeProjectilePrefab != null)
                    {
                        float areaScale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
                        _rangeProjectilePrefab.transform.localScale = new Vector3(areaScale, areaScale, 1f);
                    }
                    break;

                case StatType.AttackDamage:
                {
                    _currentRangeTrainStatus.AttackDamage = _currentRangeTrainStatus.AttackDamage / (1f + percent * prevLevel) * (1f + percent * newLevel);
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
                    _currentRangeTrainStatus.AttackInterval = _currentRangeTrainStatus.AttackInterval * (1f + (-percent) * prevLevel) / (1f + (-percent) * newLevel);
                    break;

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
