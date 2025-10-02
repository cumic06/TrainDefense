using System;
using UnityEngine;

namespace TrainDefense.Game.Data
{
    [CreateAssetMenu(fileName = "TurretTrainData", menuName = "Data/TrainData/TurretTrainData")]
    public class TurretTrainData : TrainData
    {
        #region Fields
        [SerializeField]
        private TurretTrainStatus turretTrainStatus;
        [SerializeField]
        private Projectile turretProjectilePrefab;
        #endregion

        public TurretTrainStatus TurretTrainStatus => turretTrainStatus;
        public Projectile TurretProjectilePrefab => turretProjectilePrefab;
    }

    [Serializable]
    public struct TurretTrainStatus
    {
        public int AttackDamage;
        public int AttackCount;
        public float AttackDelay;
        public float AttackRange;
    }

    /// <summary>
    /// TurretTrain의 추가 업그레이드 데이터
    /// </summary>
    [Serializable]
    public class TurretTrainUpgradeExtension : TrainUpgradeExtension
    {
        [SerializeField]
        private TurretTrainStatus turretStatusUpgrade;

        public TurretTrainStatus TurretStatusUpgrade => turretStatusUpgrade;
    }
}