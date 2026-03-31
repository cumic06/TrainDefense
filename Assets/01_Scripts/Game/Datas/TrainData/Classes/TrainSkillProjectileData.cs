using System;
using TrainDefense.Game;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TrainSkillProjectileData
    {
        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        [SerializeField]
        private string projectilePrefabId;
        [SerializeField]
        private int damage;
        [SerializeField]
        private float range;
        [SerializeField]
        private int projectileCount;

        [NonSerialized]
        private GameObject projectilePrefab;

        public string ProjectilePrefabId => projectilePrefabId;
        public int Damage => damage;
        public float Range => range;
        public int ProjectileCount => projectileCount;
        public GameObject ProjectilePrefabObject
        {
            get
            {
                if (projectilePrefab == null && !string.IsNullOrEmpty(projectilePrefabId))
                {
                    projectilePrefab = Resources.Load<GameObject>($"{PROJECTILE_PREFAB_PATH}{projectilePrefabId}");
                    if (projectilePrefab == null)
                    {
                        Debug.LogWarning($"TrainSkillProjectileData: Projectile Prefab not found at '{PROJECTILE_PREFAB_PATH}{projectilePrefabId}'");
                    }
                }

                return projectilePrefab;
            }
        }

        public Projectile ProjectilePrefab => ProjectilePrefabObject?.GetComponent<Projectile>();
    }
}
