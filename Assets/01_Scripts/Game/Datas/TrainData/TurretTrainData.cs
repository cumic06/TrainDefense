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
        private string passiveSkillDataId;
        private GameObject turretProjectilePrefab;
        [NonSerialized]
        private TrainPassiveSkillData passiveSkillDataCache;
        [NonSerialized]
        private bool passiveSkillDataResolved;
        #endregion

        public TurretTrainStatus TurretTrainStatus => turretTrainStatus;

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