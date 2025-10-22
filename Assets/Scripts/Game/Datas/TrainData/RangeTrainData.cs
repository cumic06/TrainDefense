using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [System.Serializable]
    public class RangeTrainData : TrainData
    {
        #region Fields
        [SerializeField]
        private RangeAttackTrainStatus rangeTrainStatus;
        [SerializeField]
        private Projectile rangeProjectilePrefab;
        #endregion

        public RangeAttackTrainStatus RangeTrainStatus => rangeTrainStatus;
        public Projectile RangeProjectilePrefab => rangeProjectilePrefab;
    }
    
    [Serializable]
    public class RangeTrainUpgradeExtension : TrainUpgradeExtension
    {
        public RangeAttackTrainStatus RangeStatusUpgrade;
    }

    [Serializable]
    public struct RangeAttackTrainStatus
    {
        public float AttackRange;
        public int AttackDamage;
        public int AttackCount;
        public float AttackInterval;
    }
}