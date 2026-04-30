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
        private string[] passiveSkillDataIds;
        private GameObject rangeProjectilePrefab;
        [NonSerialized]
        private TrainPassiveSkillData[] passiveSkillDatasCache;
        [NonSerialized]
        private bool passiveSkillDatasResolved;
        [SerializeField]
        private float attackDamageMultiplier = 1f;
        #endregion

        public RangeTrainStatus RangeTrainStatus => rangeTrainStatus;
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