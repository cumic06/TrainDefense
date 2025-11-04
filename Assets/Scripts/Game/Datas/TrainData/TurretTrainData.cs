using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TurretTrainData : TrainData
    {
        #region Fields
        [SerializeField]
        private TurretTrainStatus turretTrainStatus;
        [SerializeField]
        private string turretProjectilePrefabId;
        private GameObject turretProjectilePrefab;
        #endregion

        public TurretTrainStatus TurretTrainStatus => turretTrainStatus;

        [ShowInInspector, ReadOnly]
        public GameObject TurretProjectilePrefab
        {
            get
            {
                if (turretProjectilePrefab == null && !string.IsNullOrEmpty(turretProjectilePrefabId))
                {
                    turretProjectilePrefab = Resources.Load<GameObject>($"Prefabs/Projectiles/{turretProjectilePrefabId}");
                    if (turretProjectilePrefab == null)
                    {
                        Debug.LogWarning($"TurretTrainData [{Id}]: Projectile Prefab not found at 'Prefabs/{turretProjectilePrefabId}'");
                    }
                }
                return turretProjectilePrefab;
            }
        }

        [Obsolete("Use TurretProjectilePrefab property instead")]
        public Projectile TurretProjectilePrefabComponent => TurretProjectilePrefab?.GetComponent<Projectile>();
    }

    [Serializable]
    public struct TurretTrainStatus
    {
        public float AttackRange;
        public int AttackDamage;
        public int AttackCount;
        public float AttackDelay;
        public int TargetCount;
    }

    /// <summary>
    /// TurretTrain의 추가 업그레이드 데이터
    /// </summary>
    [Serializable]
    public class TurretTrainUpgradeExtension : TrainUpgradeExtension
    {
        public TurretTrainStatus TurretStatusUpgrade;
    }
}