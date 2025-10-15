using UnityEngine;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public class RangeTrain : Train, ITrainable
    {
        #region Fields
        [SerializeField]
        private RangeTrainData rangeTrainData => trainData as RangeTrainData;
        #endregion

        private RangeAttackTrainStatus _currentRangeTrainStatus;
        private Projectile _rangeProjectilePrefab;

        protected override void Setup()
        {
            base.Setup();

            _currentRangeTrainStatus = rangeTrainData.RangeTrainStatus;
            
            RangeProjectile();
        }

        private void RangeProjectile()
        {
            if (rangeTrainData.RangeProjectilePrefab != null)
            {
                _rangeProjectilePrefab = ResourceManager.Instance.Spawn(rangeTrainData.RangeProjectilePrefab);
                _rangeProjectilePrefab.transform.SetParent(transform);
                _rangeProjectilePrefab.transform.localScale = new Vector3(rangeTrainData.RangeTrainStatus.AttackRange, rangeTrainData.RangeTrainStatus.AttackRange, 1);
                _rangeProjectilePrefab.transform.localPosition = Vector3.zero;
                _rangeProjectilePrefab.transform.localRotation = Quaternion.identity;
                _rangeProjectilePrefab.Init(rangeTrainData.RangeTrainStatus.AttackDamage);
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