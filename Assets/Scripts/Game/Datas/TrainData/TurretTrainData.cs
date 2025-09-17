using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game.Data
{
    [CreateAssetMenu(fileName = "TurretTrainData", menuName = "Data/TrainData/TurretTrainData")]
    public class TurretTrainData : TrainData
    {
        #region Fields
        [SerializeField]
        [BoxGroup("AttackSetting")]
        private int attackDamage;
        [SerializeField]
        [BoxGroup("AttackSetting")]
        private int attackCount;
        [SerializeField]
        [BoxGroup("AttackSetting")]
        private float attackDelay;
        [SerializeField]
        [BoxGroup("AttackSetting")]
        private float attackRange;
        [SerializeField]
        private Projectile turretProjectilePrefab;
        #endregion

        public int AttackDamage => attackDamage;
        public int AttackCount => attackCount;
        public float AttackDelay => attackDelay;
        public float AttackRange => attackRange;
        public Projectile TurretProjectilePrefab => turretProjectilePrefab;
    }
}