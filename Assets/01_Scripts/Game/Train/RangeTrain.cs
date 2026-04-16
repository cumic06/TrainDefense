using System;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using System.Collections;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game
{
    public class RangeTrain : Train, ITrainable
    {
        #region Fields
        private RangeTrainData rangeTrainData => _trainData as RangeTrainData;
        #endregion

        protected RangeTrainStatus _currentRangeTrainStatus;
        protected Projectile _rangeProjectilePrefab;
        private Coroutine _rangeAttackCoroutine;

        public event Action OnAttacked;

        protected override void Setup()
        {
            base.Setup();

            // struct 이므로 값 복사가 일어나며, DB 원본은 변경되지 않는다.
            _currentRangeTrainStatus = rangeTrainData.RangeTrainStatus;

            _skillModule.RegisterPassiveFromData(rangeTrainData?.PassiveSkillData);

            if (TrainData.DamageType == DamageType.Direct) return;

            SpawnRangeProjectile();

            PlayLoopSFX();

            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);

            StopLoopSFX();
        }

        private void _OnInspectionStart(InspectionStartEvent _) => StopLoopSFX();

        private void _OnEngageStart(EngageStartEvent _)
        {
            if (_isDead) return;
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
            if (_currentRangeTrainStatus.AttackInterval <= 0)
            {
                if (rangeTrainData.RangeProjectilePrefab != null)
                {
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.gameObject.SetActive(true);
                    }
                    else
                    {
                        SpawnRangeProjectile();
                    }

                    if (rangeTrainData.AttackSoundType != SoundType.None && TrainData.DamageType == DamageType.Direct)
                    {
                        SoundManager.Instance.PlaySFX(rangeTrainData.AttackSoundType);
                    }

                    _currentRangeTrainStatus.AttackInterval = rangeTrainData.RangeTrainStatus.AttackInterval;
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
                _currentRangeTrainStatus.AttackInterval -= Time.deltaTime;
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

        public void SpawnExternalProjectile(Projectile prefab, float radius)
        {
            if (prefab == null) return;
            var spawned = ResourceManager.Instance.Spawn(prefab, transform.position, Quaternion.identity);
            if (spawned == null) return;
            float r = radius >= 0f ? radius : _currentRangeTrainStatus.AttackRange;
            spawned.Init(
                _currentRangeTrainStatus.AttackDamage,
                this,
                null,
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
                    _rangeProjectilePrefab.transform.localScale = new Vector3(rangeTrainData.RangeTrainStatus.AttackRange, rangeTrainData.RangeTrainStatus.AttackRange, 1);
                    _rangeProjectilePrefab.transform.localPosition = Vector3.zero;
                    _rangeProjectilePrefab.transform.localRotation = Quaternion.identity;
                    _rangeProjectilePrefab.Init(rangeTrainData.RangeTrainStatus.AttackDamage, this, null, rangeTrainData.RangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
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
                _currentRangeTrainStatus.AttackDamage += rangeStatus.AttackDamage;
                _currentRangeTrainStatus.AttackCount += rangeStatus.AttackCount;
                _currentRangeTrainStatus.AttackInterval += rangeStatus.AttackInterval;
                _currentRangeTrainStatus.CriticalChance += rangeStatus.CriticalChance;
                _currentRangeTrainStatus.CriticalDamage += rangeStatus.CriticalDamage;

                if (_rangeProjectilePrefab != null)
                {
                    _rangeProjectilePrefab.transform.localScale = new Vector3(_currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.AttackRange, 1);
                    _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                }
            }
        }

        public override void StatusUpgrade(RangeTrainStatus upgradeData)
        {
            _currentRangeTrainStatus.AttackRange += upgradeData.AttackRange;
            _currentRangeTrainStatus.AttackDamage += upgradeData.AttackDamage;
            _currentRangeTrainStatus.AttackCount += upgradeData.AttackCount;
            _currentRangeTrainStatus.AttackInterval += upgradeData.AttackInterval;
            _currentRangeTrainStatus.CriticalChance += upgradeData.CriticalChance;
            _currentRangeTrainStatus.CriticalDamage += upgradeData.CriticalDamage;

            if (_rangeProjectilePrefab != null)
            {
                _rangeProjectilePrefab.transform.localScale = new Vector3(_currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.AttackRange, 1);
                _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
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

                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.transform.localScale =
                            new Vector3(_currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.AttackRange, 1f);
                    }
                    break;

                case StatType.AttackDamage:
                    _currentRangeTrainStatus.AttackDamage += Mathf.RoundToInt(baseStatus.AttackDamage * percent);
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, this, null, _currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.CriticalChance, _currentRangeTrainStatus.CriticalDamage);
                    }
                    break;

                case StatType.AttackCount:
                    _currentRangeTrainStatus.AttackCount += Mathf.RoundToInt(baseStatus.AttackCount * percent);
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