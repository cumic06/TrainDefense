using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using System.Collections;

namespace TrainDefense.Game
{
    public class RangeTrain : Train, ITrainable
    {
        #region Fields
        private RangeTrainData rangeTrainData => _trainData as RangeTrainData;
        #endregion

        private RangeTrainStatus _currentRangeTrainStatus;
        private Projectile _rangeProjectilePrefab;
        private Coroutine _rangeAttackCoroutine;

        protected override void Setup()
        {
            base.Setup();

            // struct 이므로 값 복사가 일어나며, DB 원본은 변경되지 않는다.
            _currentRangeTrainStatus = rangeTrainData.RangeTrainStatus;

            if (TrainData.DamageType == DamageType.Direct) return;

            SpawnRangeProjectile();

            if (rangeTrainData.AttackSoundType != SoundType.None)
            {
                SoundManager.Instance.PlaySFX(rangeTrainData.AttackSoundType, true);
            }
        }

        private void Update()
        {
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
                    _rangeProjectilePrefab.Init(rangeTrainData.RangeTrainStatus.AttackDamage, null, rangeTrainData.RangeTrainStatus.AttackRange);
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

                if (_rangeProjectilePrefab != null)
                {
                    _rangeProjectilePrefab.transform.localScale = new Vector3(_currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.AttackRange, 1);
                    _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, null, _currentRangeTrainStatus.AttackRange);
                }
            }
        }

        public override void StatusUpgrade(RangeTrainStatus upgradeData)
        {
            _currentRangeTrainStatus.AttackRange += upgradeData.AttackRange;
            _currentRangeTrainStatus.AttackDamage += upgradeData.AttackDamage;
            _currentRangeTrainStatus.AttackCount += upgradeData.AttackCount;
            _currentRangeTrainStatus.AttackInterval += upgradeData.AttackInterval;

            if (_rangeProjectilePrefab != null)
            {
                _rangeProjectilePrefab.transform.localScale = new Vector3(_currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.AttackRange, 1);
                _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, null, _currentRangeTrainStatus.AttackRange);
            }
        }

        protected override void ApplyStat(IStat stat)
        {
            base.ApplyStat(stat);
            if (stat == null) return;

            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentRangeTrainStatus.AttackRange += stat.Value;

                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.transform.localScale =
                            new Vector3(_currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.AttackRange, 1f);
                    }
                    break;

                case StatType.AttackDamage:
                    _currentRangeTrainStatus.AttackDamage += Mathf.RoundToInt(stat.Value);
                    if (_rangeProjectilePrefab != null)
                    {
                        _rangeProjectilePrefab.Init(_currentRangeTrainStatus.AttackDamage, null, _currentRangeTrainStatus.AttackRange);
                    }
                    break;

                case StatType.AttackCount:
                    _currentRangeTrainStatus.AttackCount += Mathf.RoundToInt(stat.Value);
                    break;

                case StatType.AttackInterval:
                    _currentRangeTrainStatus.AttackInterval += stat.Value;
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