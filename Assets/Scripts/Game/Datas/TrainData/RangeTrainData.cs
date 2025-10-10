using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [CreateAssetMenu(fileName = "RangeTrainData", menuName = "Data/TrainData/RangeTrainData")]
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
    public struct RangeAttackTrainStatus
    {
        public float AttackRange;
        public int AttackDamage;
        public int AttackCount;
        public float AttackInterval;
    }
}
