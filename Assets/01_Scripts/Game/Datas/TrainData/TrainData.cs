using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TrainData : IDescribableData, IIconData, IPrefabData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string name;
        [SerializeField]
        private string iconId;
        [SerializeField]
        private Sprite icon;
        [SerializeField]
        private DamageType damageType;
        [SerializeField]
        private SoundType attackSoundType;
        [SerializeField]
        [TextArea(2, 4)]
        private string description;
        [SerializeField]
        private TrainStatusData trainStatusData;
        [SerializeField]
        private string prefabId;
        private GameObject prefab;
        [SerializeField]
        private string[] passiveSkillDataIds;
        [NonSerialized]
        private TrainPassiveSkillData[] passiveSkillDatasCache;
        [NonSerialized]
        private bool passiveSkillDatasResolved;
        [SerializeField]
        private bool isMainTrain;
        [SerializeField]
        [Tooltip("엘리트 승격 조건(엘리트 포탑 데이터에만). 원본 포탑의 현재 스탯이 전부 만족해야 상점에 뜬다. 비어 있으면 레벨 조건으로 판정")]
        private ElitePromotionCondition[] elitePromotionConditions;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        #region IDescribableData
        public string Name => TrainDefense.Localize.LocalizeHelper.GetByKey(name, name).Replace("\\n", "\n");
        // txt에는 줄바꿈을 \n 문자열로 적고 여기서 실제 줄바꿈으로 변환 (TSV라 셀 안에 직접 못 넣음).
        public string Description => TrainDefense.Localize.LocalizeHelper.GetByKey(description, description).Replace("\\n", "\n");
        #endregion

        #region IIconData
        public string IconId => iconId;
        public Sprite Icon => icon;
        #endregion

        #region IPrefabData
        public string PrefabId => prefabId;
        [ShowInInspector, ReadOnly]
        public GameObject Prefab
        {
            get
            {
                if (prefab == null && !string.IsNullOrEmpty(prefabId))
                {
                    prefab = Resources.Load<GameObject>($"Prefabs/Trains/{prefabId}");
                    if (prefab == null)
                    {
                        Debug.LogWarning($"TrainData [{id}]: Prefab not found at 'Prefabs/{prefabId}'");
                    }
                }
                return prefab;
            }
        }
        #endregion

        public string TrainName => Name;
        public DamageType DamageType => damageType;
        public SoundType AttackSoundType => attackSoundType;
        public TrainStatusData TrainStatusData => trainStatusData;
        public string[] PassiveSkillDataIds => passiveSkillDataIds;

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
                if (db?.TrainSkillDataDB == null)
                {
                    passiveSkillDatasCache = System.Array.Empty<TrainPassiveSkillData>();
                    return passiveSkillDatasCache;
                }
                var result = new System.Collections.Generic.List<TrainPassiveSkillData>();
                foreach (var skillId in passiveSkillDataIds)
                {
                    if (string.IsNullOrEmpty(skillId)) continue;
                    var data = db.TrainSkillDataDB.trainPassiveSkillDataList.Find(s => s != null && s.Id == skillId);
                    if (data != null) result.Add(data);
                    else Debug.LogWarning($"TrainData [{id}]: PassiveSkillData '{skillId}' not found in DB");
                }
                passiveSkillDatasCache = result.ToArray();
                return passiveSkillDatasCache;
            }
        }
        public TrainPassiveSkillData PassiveSkillData => PassiveSkillDatas.Length > 0 ? PassiveSkillDatas[0] : null;

        [Obsolete("Use Prefab property instead")]
        public Train TrainPrefab => Prefab?.GetComponent<Train>();
        public bool IsMainTrain => isMainTrain;
        public ElitePromotionCondition[] ElitePromotionConditions => elitePromotionConditions ?? Array.Empty<ElitePromotionCondition>();
    }
}
