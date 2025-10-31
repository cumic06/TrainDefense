using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [System.Serializable]
    public class RangeTrainData : TrainData
    {
        #region Fields
        [SerializeField]
        private RangeAttackTrainStatus rangeTrainStatus;
        [SerializeField]
        private string rangeProjectilePrefabId;
        private GameObject rangeProjectilePrefab;
        #endregion

        public RangeAttackTrainStatus RangeTrainStatus => rangeTrainStatus;
        
        [ShowInInspector, ReadOnly]
        public GameObject RangeProjectilePrefab
        {
            get
            {
                if (rangeProjectilePrefab == null && !string.IsNullOrEmpty(rangeProjectilePrefabId))
                {
                    rangeProjectilePrefab = Resources.Load<GameObject>($"Prefabs/Projectiles/{rangeProjectilePrefabId}");
                    if (rangeProjectilePrefab == null)
                    {
                        Debug.LogWarning($"RangeTrainData [{Id}]: Projectile Prefab not found at 'Prefabs/{rangeProjectilePrefabId}'");
                    }
                }
                return rangeProjectilePrefab;
            }
        }
        
        [Obsolete("Use RangeProjectilePrefab property instead")]
        public Projectile RangeProjectilePrefabComponent => RangeProjectilePrefab?.GetComponent<Projectile>();
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