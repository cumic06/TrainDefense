using System;
using System.Collections;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 범위 공격(지속형 장판 토글) 동작을 캡슐화하는 컴포넌트. 기존 RangeTrain의 공격 로직을 이관.
    /// Train(shell)이 라이프사이클/스탯 가상 메서드를 이 모듈로 위임한다.
    /// </summary>
    public class RangeAttackModule : MonoBehaviour, IAttackModule,
        IAttackEvents, IExternalProjectileSpawner, IForceAttacker, IShoveSuppressible, ISlowProvider
    {
        private Train _train;
        private RangeTrainData _data;
        private bool _initialized;

        private RangeTrainStatus _currentRangeTrainStatus;
        private Projectile _rangeProjectilePrefab;
        // AttackArea(데이터값)를 prefab의 base 콜라이더 radius로 나눠 localScale에 적용해야 월드 반경과 일치.
        private float _baseColliderRadius = 1f;
        private Coroutine _rangeAttackCoroutine;
        private float _attackCountdown;
        private bool _suppressMainProjectileShove;

        // ApplyStat 퍼센트 누적 손실 방지용 fractional accumulator (UtilMath.AccumulateIntDelta 참조)
        private float _statAttackDamageAccum;
        private float _statAttackCountAccum;

        public event Action<Monster> OnAttacked;

        public RangeTrainStatus BaseStatus => _data != null ? _data.RangeTrainStatus : default;
        public RangeTrainStatus CurrentStatus => _currentRangeTrainStatus;

        public float CurrentAttackRange => _currentRangeTrainStatus.AttackRange;
        // 레인지 포탑은 사거리가 아닌 공격 범위(자기 위치 중심 원)를 표시한다.
        public float RangeIndicatorRadius => _currentRangeTrainStatus.AttackArea;

        #region Lifecycle
        public void InitializeModule(Train owner)
        {
            _train = owner;
            _data = owner != null ? owner.TrainData as RangeTrainData : null;
            if (_data == null) return;

            // struct 이므로 값 복사가 일어나며, DB 원본은 변경되지 않는다.
            _currentRangeTrainStatus = _data.RangeTrainStatus;
            _attackCountdown = _currentRangeTrainStatus.AttackInterval;

            if (_train.TrainData.DamageType != DamageType.Direct)
            {
                SpawnRangeProjectile();
                PlayLoopSFX();

                GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
                GameEventSystem.Subscribe<EngageReadyEvent>(_OnEngageReady);
                GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
            }

            _initialized = true;
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);

            StopLoopSFX();
        }

        private void Update()
        {
            if (!_initialized || _train == null || _train.IsDead) return;
            RangeAttackHandler();
        }
        #endregion

        #region Sub/SFX
        private void _OnInspectionStart(InspectionStartEvent _) => StopLoopSFX();

        private void _OnEngageReady(EngageReadyEvent _) => ClearAttachedProjectiles();

        private void _OnEngageStart(EngageStartEvent _)
        {
            if (_train == null || _train.IsDead) return;

            if (_rangeProjectilePrefab == null)
                SpawnRangeProjectile();

            PlayLoopSFX();
        }

        private void PlayLoopSFX()
        {
            if (_data.AttackSoundType == SoundType.None) return;
            SoundManager.Instance.PlaySFX(_data.AttackSoundType, true);
        }

        private void StopLoopSFX()
        {
            if (_train == null || _train.TrainData == null) return;
            if (_train.TrainData.DamageType == DamageType.Direct) return;
            if (_data.AttackSoundType == SoundType.None) return;
            SoundManager.Instance.StopSFX(_data.AttackSoundType);
        }
        #endregion

        #region Attack
        private void RangeAttackHandler()
        {
            if (_attackCountdown <= 0)
            {
                if (_data.RangeProjectilePrefab != null)
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

                    if (_data.AttackSoundType != SoundType.None && _train.TrainData.DamageType == DamageType.Direct)
                    {
                        SoundManager.Instance.PlaySFX(_data.AttackSoundType);
                    }

                    _attackCountdown = _currentRangeTrainStatus.AttackInterval;
                    OnAttacked?.Invoke(null);

                    if (_train.TrainData.DamageType == DamageType.Direct)
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
            yield return new WaitForSeconds(_data.RangeTrainStatus.AttackInterval / _data.RangeTrainStatus.AttackInterval);

            if (_rangeProjectilePrefab != null)
            {
                _rangeProjectilePrefab.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 스킬 InstantAttack용: 쿨다운을 기다리지 않고 즉시 1회 공격을 강제한다.
        /// </summary>
        public bool ForceAttack()
        {
            if (_train == null || _train.IsDead) return false;
            if (_data == null || _data.RangeProjectilePrefab == null) return false;
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
            if (prefab == null || _train == null) return;
            var spawned = ResourceManager.Instance.Spawn(prefab, _train.transform.position, Quaternion.identity);
            if (spawned == null) return;
            float r = radius >= 0f ? radius : _currentRangeTrainStatus.AttackArea;
            if (target != null) spawned.transform.LookAt2D(target.TargetTransform);
            int damage = Mathf.RoundToInt(_currentRangeTrainStatus.AttackDamage * damageMul);
            spawned.ShoveScale = shoveScale;
            spawned.Init(
                damage,
                _train,
                target,
                r,
                _currentRangeTrainStatus.CriticalChance,
                _currentRangeTrainStatus.CriticalDamage);
        }

        private void SpawnRangeProjectile()
        {
            if (_data.RangeProjectilePrefab == null) return;

            // 기존 Projectile 제거
            if (_rangeProjectilePrefab != null)
            {
                ResourceManager.Instance.Destroy(_rangeProjectilePrefab.gameObject);
                _rangeProjectilePrefab = null;
            }
            Projectile projectile = _data.RangeProjectilePrefab.GetComponent<Projectile>();
            if (projectile == null) return;

            _rangeProjectilePrefab = ResourceManager.Instance.Spawn(projectile);
            _rangeProjectilePrefab.transform.SetParent(_train.transform);
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
            _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, _train, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
        }

        public void ClearAttachedProjectiles()
        {
            if (_rangeProjectilePrefab == null) return;

            ResourceManager.Instance.Destroy(_rangeProjectilePrefab.gameObject);
            _rangeProjectilePrefab = null;
        }
        #endregion

        #region Slow
        // 누적 둔화율(%, 기차 base + 강화)을 둔화 배율로 변환.
        public float GetSlowValue()
        {
            return 1f - _currentRangeTrainStatus.SlowRate / 100f;
        }
        #endregion

        #region Upgrade / Stats
        public void ApplyUpgrade(ITrainUpgradeData upgradeData, int prevLevel)
        {
            if (upgradeData is not RangeTrainUpgradeData rangeUpgradeData) return;

            // Train 초기 레벨은 -1, upgradeStats 배열은 0부터 시작.
            int upgradeLevelIndex = prevLevel + 1;
            var rangeStatus = rangeUpgradeData.GetRangeStatusUpgrade(upgradeLevelIndex);
            _currentRangeTrainStatus.AttackRange += rangeStatus.AttackRange;
            _currentRangeTrainStatus.AttackArea += rangeStatus.AttackArea;
            _currentRangeTrainStatus.AttackDamage += rangeStatus.AttackDamage;
            _currentRangeTrainStatus.AttackCount += rangeStatus.AttackCount;
            _currentRangeTrainStatus.AttackInterval += rangeStatus.AttackInterval;
            _currentRangeTrainStatus.CriticalChance += rangeStatus.CriticalChance;
            _currentRangeTrainStatus.CriticalDamage += rangeStatus.CriticalDamage;
            _currentRangeTrainStatus.SlowRate += rangeStatus.SlowRate;

            RefreshProjectileScaleAndStats();
        }

        public void StatusUpgrade(RangeTrainStatus upgradeData)
        {
            _currentRangeTrainStatus.AttackRange += upgradeData.AttackRange;
            _currentRangeTrainStatus.AttackArea += upgradeData.AttackArea;
            _currentRangeTrainStatus.AttackDamage += upgradeData.AttackDamage;
            _currentRangeTrainStatus.AttackCount += upgradeData.AttackCount;
            _currentRangeTrainStatus.AttackInterval += upgradeData.AttackInterval;
            _currentRangeTrainStatus.CriticalChance += upgradeData.CriticalChance;
            _currentRangeTrainStatus.CriticalDamage += upgradeData.CriticalDamage;
            _currentRangeTrainStatus.SlowRate += upgradeData.SlowRate;

            RefreshProjectileScaleAndStats();
        }

        public bool ApplyAttackStatByCurrentValue(IStat stat)
        {
            if (stat == null) return false;
            float percent = stat.Value / 100f;
            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentRangeTrainStatus.AttackRange += _currentRangeTrainStatus.AttackRange * percent;
                    return true;
                case StatType.AttackArea:
                    _currentRangeTrainStatus.AttackArea += _currentRangeTrainStatus.AttackArea * percent;
                    RefreshProjectileScaleOnly();
                    return true;
                case StatType.AttackDamage:
                    _currentRangeTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, _currentRangeTrainStatus.AttackDamage * percent);
                    RefreshProjectileStatsByRange();
                    return true;
                case StatType.AttackInterval:
                    // 공속은 상점과 동일하게 현재값 기준 역수 곱셈(DPS 선형, 0 이하 방지). percent 음수=공속 증가.
                    _currentRangeTrainStatus.AttackInterval *= 1f / (1f + (-percent));
                    return true;
                case StatType.SlowRate:
                    _currentRangeTrainStatus.SlowRate += _currentRangeTrainStatus.SlowRate * percent;
                    return true;
                default:
                    return false;
            }
        }

        public void ApplyAttackStat(IStat stat)
        {
            if (stat == null) return;
            var baseStatus = _data.RangeTrainStatus;
            float percent = stat.Value / 100f;
            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentRangeTrainStatus.AttackRange += baseStatus.AttackRange * percent;
                    break;

                case StatType.AttackArea:
                    _currentRangeTrainStatus.AttackArea += baseStatus.AttackArea * percent;
                    RefreshProjectileScaleOnly();
                    break;

                case StatType.AttackDamage:
                    _currentRangeTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, stat.Value * _data.AttackDamageMultiplier);
                    RefreshProjectileStatsByRange();
                    break;

                case StatType.AttackCount:
                    _currentRangeTrainStatus.AttackCount += UtilMath.AccumulateIntDelta(ref _statAttackCountAccum, baseStatus.AttackCount * percent);
                    break;

                case StatType.AttackInterval:
                    _currentRangeTrainStatus.AttackInterval += baseStatus.AttackInterval * percent;
                    break;

                case StatType.CriticalChance:
                    _currentRangeTrainStatus.CriticalChance += stat.Value;
                    RefreshProjectileStatsByRange();
                    break;

                case StatType.CriticalDamage:
                    _currentRangeTrainStatus.CriticalDamage += stat.Value;
                    RefreshProjectileStatsByRange();
                    break;
            }
        }

        public bool ApplyAttackStatLevelAware(IStat stat, int newLevel, int prevLevel)
        {
            if (stat == null) return false;
            var baseStatus = _data.RangeTrainStatus;
            float percent = stat.Value / 100f;
            int times = newLevel - prevLevel;

            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentRangeTrainStatus.AttackRange = _currentRangeTrainStatus.AttackRange / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    return true;

                case StatType.AttackArea:
                    _currentRangeTrainStatus.AttackArea = _currentRangeTrainStatus.AttackArea / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    RefreshProjectileScaleOnly();
                    return true;

                case StatType.AttackDamage:
                    _currentRangeTrainStatus.AttackDamage = _currentRangeTrainStatus.AttackDamage / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    RefreshProjectileStatsByRange();
                    return true;

                case StatType.AttackCount:
                {
                    int newTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * newLevel);
                    int oldTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * prevLevel);
                    _currentRangeTrainStatus.AttackCount += newTot - oldTot;
                    return true;
                }

                case StatType.AttackInterval:
                    _currentRangeTrainStatus.AttackInterval = _currentRangeTrainStatus.AttackInterval * (1f + (-percent) * prevLevel) / (1f + (-percent) * newLevel);
                    return true;

                case StatType.CriticalChance:
                    _currentRangeTrainStatus.CriticalChance += stat.Value * times;
                    RefreshProjectileStatsByRange();
                    return true;

                case StatType.CriticalDamage:
                    _currentRangeTrainStatus.CriticalDamage += stat.Value * times;
                    RefreshProjectileStatsByRange();
                    return true;

                default:
                    return false;
            }
        }

        public void CopyProgressFrom(IAttackModule source)
        {
            if (source is not RangeAttackModule srcRange) return;
            if (srcRange._data == null || _data == null) return;

            var srcBase = srcRange._data.RangeTrainStatus;
            var srcCurrent = srcRange._currentRangeTrainStatus;
            var newBase = _data.RangeTrainStatus;

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

            RefreshProjectileScaleAndStats();
        }

        // 투사체 스케일(AttackArea) + 데미지/크리 재초기화.
        private void RefreshProjectileScaleAndStats()
        {
            if (_rangeProjectilePrefab == null) return;
            float scale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
            _rangeProjectilePrefab.transform.localScale = new Vector3(scale, scale, 1f);
            _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, _train, null, _currentRangeTrainStatus.AttackArea, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
        }

        // 투사체 스케일만 갱신(AttackArea 변경 시).
        private void RefreshProjectileScaleOnly()
        {
            if (_rangeProjectilePrefab == null) return;
            float areaScale = _currentRangeTrainStatus.AttackArea / _baseColliderRadius;
            _rangeProjectilePrefab.transform.localScale = new Vector3(areaScale, areaScale, 1f);
        }

        // 투사체 데미지/크리 재초기화(AttackRange를 반경 인자로 사용 — 기존 RangeTrain.ApplyStat과 동일).
        private void RefreshProjectileStatsByRange()
        {
            if (_rangeProjectilePrefab == null) return;
            _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, _train, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
        }
        #endregion

        #region Display / SpawnPoint (IAttackModule)
        // 범위 기차는 전용 스폰포인트가 없어 기차 중심(Train.transform)을 사용 → null 반환(Train이 폴백).
        public Transform GetSkillSpawnPoint(int index) => null;

        public string GetStatSummary()
        {
            var s = _currentRangeTrainStatus;
            return $"DMG={s.AttackDamage} | RANGE={s.AttackRange} | AREA={s.AttackArea}";
        }

        public (string label, string value)[] GetStatDetailLines()
        {
            var s = _currentRangeTrainStatus;
            Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            return new[]
            {
                (L("Detail_Damage", "공격력"), $"{Mathf.RoundToInt(s.AttackDamage)}"),
                (L("Detail_Range", "사거리"), $"{s.AttackRange:F1}"),
                (L("Detail_Area", "범위"), $"{s.AttackArea:F1}"),
                (L("Detail_Speed", "공격속도"), $"{TrainStatLine.ToAttackSpeed(s.AttackInterval):F2}"),
                (L("Detail_CritChance", "크리티컬 확률"), $"{s.CriticalChance:F0}%"),
                (L("Detail_CritDamage", "크리티컬 데미지"), $"+{Projectile.BaseCriticalDamagePercent + s.CriticalDamage:F0}%"),
            };
        }
        #endregion
    }
}
