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
        private string[] passiveSkillDataIds;
        private GameObject turretProjectilePrefab;
        [NonSerialized]
        private TrainPassiveSkillData[] passiveSkillDatasCache;
        [NonSerialized]
        private bool passiveSkillDatasResolved;
        [SerializeField]
        private float attackDamageMultiplier = 1f;
        #endregion

        public TurretTrainStatus TurretTrainStatus => turretTrainStatus;
        public float AttackDamageMultiplier => attackDamageMultiplier;
        public TrainPassiveSkillData[] PassiveSkillDatas
        {
            get
            {
                if (passiveSkillDatasResolved) return passiveSkillDatasCache;
                passiveSkillDatasResolved = true;
                if (passiveSkillDataIds == null || passiveSkillDataIds.Length == 0)
                {
                    passiveSkillDatasCache = System.Array.Empty<TrainPassiveSkillData>();
                    return passiveSkillDatasCache;
                }
                var db = DatabaseManager.Instance?.GetDB();
                if (db?.trainPassiveSkillDataList == null)
                {
                    passiveSkillDatasCache = System.Array.Empty<TrainPassiveSkillData>();
                    return passiveSkillDatasCache;
                }
                var result = new System.Collections.Generic.List<TrainPassiveSkillData>();
                foreach (var id in passiveSkillDataIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    var data = db.trainPassiveSkillDataList.Find(s => s != null && s.Id == id);
                    if (data != null) result.Add(data);
                }
                passiveSkillDatasCache = result.ToArray();
                return passiveSkillDatasCache;
            }
        }
        public TrainPassiveSkillData PassiveSkillData => PassiveSkillDatas.Length > 0 ? PassiveSkillDatas[0] : null;

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