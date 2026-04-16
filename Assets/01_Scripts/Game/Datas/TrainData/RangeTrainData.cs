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
        private string passiveSkillDataId;
        private GameObject rangeProjectilePrefab;
        [NonSerialized]
        private TrainPassiveSkillData passiveSkillDataCache;
        [NonSerialized]
        private bool passiveSkillDataResolved;
        #endregion

        public RangeTrainStatus RangeTrainStatus => rangeTrainStatus;
        public TrainPassiveSkillData PassiveSkillData
        {
            get
            {
                if (passiveSkillDataResolved) return passiveSkillDataCache;
                passiveSkillDataResolved = true;
                if (string.IsNullOrEmpty(passiveSkillDataId))
                {
                    passiveSkillDataCache = null;
                    return null;
                }
                var dbm = DatabaseManager.Instance;
                var db = dbm?.GetDB();
                if (db?.trainPassiveSkillDataList == null)
                {
                    passiveSkillDataCache = null;
                    return null;
                }
                passiveSkillDataCache = db.trainPassiveSkillDataList.Find(s => s != null && s.Id == passiveSkillDataId);
                return passiveSkillDataCache;
            }
        }

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