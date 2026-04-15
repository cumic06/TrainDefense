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
        [SerializeField]
        private string passiveSkillsRaw;
        private GameObject turretProjectilePrefab;
        #endregion

        public TurretTrainStatus TurretTrainStatus => turretTrainStatus;
        public string PassiveSkillsRaw => passiveSkillsRaw;

        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        [ShowInInspector, ReadOnly]
        public GameObject TurretProjectilePrefab
        {
            get
            {
                if (turretProjectilePrefab == null && !string.IsNullOrEmpty(turretProjectilePrefabId))
                {
                    turretProjectilePrefab = Resources.Load<GameObject>($"{PROJECTILE_PREFAB_PATH}{turretProjectilePrefabId}");
                    if (turretProjectilePrefab == null)
                    {
                        Debug.LogWarning($"TurretTrainData [{Id}]: Projectile Prefab not found at '{PROJECTILE_PREFAB_PATH}{turretProjectilePrefabId}'");
                    }
                }
                return turretProjectilePrefab;
            }
        }

        [Obsolete("Use TurretProjectilePrefab property instead")]
        public Projectile TurretProjectilePrefabComponent => TurretProjectilePrefab?.GetComponent<Projectile>();
    }
}