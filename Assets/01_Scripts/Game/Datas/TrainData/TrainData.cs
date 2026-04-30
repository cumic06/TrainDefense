using System;
using System.Linq;
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
        private string activeSkillDataId;
        [NonSerialized]
        private TrainSkillData activeSkillDataCache;
        [NonSerialized]
        private bool activeSkillDataResolved;
        [SerializeField]
        private string[] passiveSkillDataIds;
        [NonSerialized]
        private TrainPassiveSkillData[] passiveSkillDatasCache;
        [NonSerialized]
        private bool passiveSkillDatasResolved;
        private Sprite skillIcon;
        [SerializeField]
        private bool isMainTrain;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        #region IDescribableData
        public string Name => name;
        public string Description => description;
        #endregion

        #region IIconData
        public string IconId => iconId;
        public Sprite Icon
        {
            get
            {
                if (icon == null && !string.IsNullOrEmpty(iconId))
                {
                    // Resources.LoadAll은 지정된 경로의 모든 하위 폴더를 재귀적으로 검색합니다.
                    // 전체 Resources 폴더를 검색하도록 빈 문자열("")을 사용합니다.
                    icon = Resources.LoadAll<Sprite>("")
                                    .FirstOrDefault(item => item.name == iconId);

                    if (icon == null)
                    {
                        Debug.LogWarning($"TrainData [{id}]: Icon not found at 'Sprite/{iconId}'");
                    }
                }
                return icon;
            }
        }
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

        public string TrainName => name;
        public DamageType DamageType => damageType;
        public SoundType AttackSoundType => attackSoundType;
        public TrainStatusData TrainStatusData => trainStatusData;
        public string ActiveSkillDataId => activeSkillDataId;
        public TrainSkillData TrainSkillData
        {
            get
            {
                if (activeSkillDataResolved) return activeSkillDataCache;
                activeSkillDataResolved = true;
                var firstId = activeSkillDataId;
                if (string.IsNullOrEmpty(firstId))
                {
                    activeSkillDataCache = null;
                    return null;
                }
                var dbm = TrainDefense.Game.DatabaseManager.Instance;
                var db = dbm != null ? dbm.GetDB() : null;
                if (db == null || db.trainSkillDataList == null)
                {
                    activeSkillDataCache = null;
                    return null;
                }
                activeSkillDataCache = db.trainSkillDataList.Find(s => s != null && s.Id == firstId);
                if (activeSkillDataCache == null)
                    Debug.LogWarning($"TrainData [{id}]: ActiveSkillData '{firstId}' not found in DB");
                return activeSkillDataCache;
            }
        }
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
                foreach (var skillId in passiveSkillDataIds)
                {
                    if (string.IsNullOrEmpty(skillId)) continue;
                    var data = db.trainPassiveSkillDataList.Find(s => s != null && s.Id == skillId);
                    if (data != null) result.Add(data);
                    else Debug.LogWarning($"TrainData [{id}]: PassiveSkillData '{skillId}' not found in DB");
                }
                passiveSkillDatasCache = result.ToArray();
                return passiveSkillDatasCache;
            }
        }
        public TrainPassiveSkillData PassiveSkillData => PassiveSkillDatas.Length > 0 ? PassiveSkillDatas[0] : null;
        public Sprite SkillIcon
        {
            get
            {
                var skillIconId = TrainSkillData?.SkillIconId;
                if (skillIcon == null && !string.IsNullOrEmpty(skillIconId))
                {
                    skillIcon = Resources.LoadAll<Sprite>("")
                                    .FirstOrDefault(item => item.name == skillIconId);

                    if (skillIcon == null)
                    {
                        Debug.LogWarning($"TrainData [{id}]: SkillIcon not found for '{skillIconId}'");
                    }
                }
                return skillIcon;
            }
        }

        [Obsolete("Use Prefab property instead")]
        public Train TrainPrefab => Prefab?.GetComponent<Train>();
        public bool IsMainTrain => isMainTrain;
    }
}
