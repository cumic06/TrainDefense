using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using System.Collections;

namespace TrainDefense.Game
{
    public class RangeTrain : Train, ITrainable
    {
        #region Fields
        [SerializeField]
        private RangeTrainData rangeTrainData => _trainData as RangeTrainData;
        [SerializeField]
        private bool isExplosionProjectile;
        #endregion

        private RangeTrainStatus _currentRangeTrainStatus;
        private Projectile _rangeProjectilePrefab;
        private Coroutine _rangeAttackCoroutine;

        protected override void Setup()
        {
            base.Setup();

            // struct 이므로 값 복사가 일어나며, DB 원본은 변경되지 않는다.
            _currentRangeTrainStatus = rangeTrainData.RangeTrainStatus;

            if (isExplosionProjectile) return;
            SpawnRangeProjectile();
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

                    _currentRangeTrainStatus.AttackInterval = rangeTrainData.RangeTrainStatus.AttackInterval;

                    if (isExplosionProjectile)
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
                Projectile projectile = rangeTrainData.RangeProjectilePrefab?.GetComponent<Projectile>();

                if (projectile != null)
                {
                    _rangeProjectilePrefab = ResourceManager.Instance.Spawn(projectile);
                    _rangeProjectilePrefab.transform.SetParent(transform);
                    _rangeProjectilePrefab.transform.localScale = new Vector3(rangeTrainData.RangeTrainStatus.AttackRange, rangeTrainData.RangeTrainStatus.AttackRange, 1);
                    _rangeProjectilePrefab.transform.localPosition = Vector3.zero;
                    _rangeProjectilePrefab.transform.localRotation = Quaternion.identity;
                    _rangeProjectilePrefab.Init(rangeTrainData.RangeTrainStatus.AttackDamage);
                }
            }
        }

        public override void Upgrade(ITrainUpgradeData upgradeData)
        {
            base.Upgrade(upgradeData);

            if (upgradeData == null) return;

            // RangeTrain 전용 업그레이드 데이터가 있다면 적용
            if (upgradeData is RangeTrainUpgradeData rangeUpgradeData)
            {
                _currentRangeTrainStatus.AttackRange += rangeUpgradeData.RangeStatusUpgrade.AttackRange;
                _currentRangeTrainStatus.AttackDamage += rangeUpgradeData.RangeStatusUpgrade.AttackDamage;
                _currentRangeTrainStatus.AttackCount += rangeUpgradeData.RangeStatusUpgrade.AttackCount;
                _currentRangeTrainStatus.AttackInterval += rangeUpgradeData.RangeStatusUpgrade.AttackInterval;

                if (_rangeProjectilePrefab != null)
                {
                    _rangeProjectilePrefab.transform.localScale = new Vector3(_currentRangeTrainStatus.AttackRange, _currentRangeTrainStatus.AttackRange, 1);
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
            }
        }

        protected override void ApplyStat(IStat stat)
        {
            base.ApplyStat(stat);
            if (stat == null) return;

            Debug.Log($"ApplyStat: {stat.Type} {stat.Value}");

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