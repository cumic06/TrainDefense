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
        private RangeTrainStatus rangeTrainStatus;
        [SerializeField]
        private string rangeProjectilePrefabId;
        [SerializeField]
        private string passiveSkillsRaw;
        private GameObject rangeProjectilePrefab;
        #endregion

        public RangeTrainStatus RangeTrainStatus => rangeTrainStatus;
        public string PassiveSkillsRaw => passiveSkillsRaw;

        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        [ShowInInspector, ReadOnly]
        public GameObject RangeProjectilePrefab
        {
            get
            {
                if (rangeProjectilePrefab == null && !string.IsNullOrEmpty(rangeProjectilePrefabId))
                {
                    rangeProjectilePrefab = Resources.Load<GameObject>($"{PROJECTILE_PREFAB_PATH}{rangeProjectilePrefabId}");
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
}