using System;
using System.Collections.Generic;
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
        private string[] activeSkillDataIds;
        [NonSerialized]
        private TrainSkillData[] activeSkillDatasCache;
        [NonSerialized]
        private bool activeSkillDatasResolved;
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
        public string Name => TrainDefense.Localize.LocalizeHelper.ProtectWordBreak(TrainDefense.Localize.LocalizeHelper.GetByKey(name, name).Replace("\\n", "\n"));
        // txt에는 줄바꿈을 \n 문자열로 적고 여기서 실제 줄바꿈으로 변환 (TSV라 셀 안에 직접 못 넣음).
        // ProtectWordBreak로 한글 단어 중간 줄바꿈도 방지한다.
        public string Description => TrainDefense.Localize.LocalizeHelper.ProtectWordBreak(TrainDefense.Localize.LocalizeHelper.GetByKey(description, description).Replace("\\n", "\n"));
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
        public string ActiveSkillDataId => activeSkillDataIds != null && activeSkillDataIds.Length > 0 ? activeSkillDataIds[0] : null;
        public string[] ActiveSkillDataIds => activeSkillDataIds;
        public string[] PassiveSkillDataIds => passiveSkillDataIds;
        public TrainSkillData TrainSkillData => TrainSkillDatas.Length > 0 ? TrainSkillDatas[0] : null;
        public TrainSkillData[] TrainSkillDatas
        {
            get
            {
                if (activeSkillDatasResolved) return activeSkillDatasCache;
                activeSkillDatasResolved = true;
                if (activeSkillDataIds == null || activeSkillDataIds.Length == 0)
                {
                    activeSkillDatasCache = System.Array.Empty<TrainSkillData>();
                    return activeSkillDatasCache;
                }
                var dbm = TrainDefense.Game.DatabaseManager.Instance;
                var db = dbm != null ? dbm.GetDB() : null;
                if (db == null || db.TrainSkillDataDB == null)
                {
                    activeSkillDatasCache = System.Array.Empty<TrainSkillData>();
                    return activeSkillDatasCache;
                }
                var result = new System.Collections.Generic.List<TrainSkillData>();
                foreach (var skillId in activeSkillDataIds)
                {
                    if (string.IsNullOrEmpty(skillId)) continue;
                    var data = db.TrainSkillDataDB.trainActiveSkillDataList.Find(s => s != null && s.Id == skillId);
                    if (data != null) result.Add(data);
                    else Debug.LogWarning($"TrainData [{id}]: ActiveSkillData '{skillId}' not found in DB");
                }
                activeSkillDatasCache = result.ToArray();
                return activeSkillDatasCache;
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

        // 트라이초이스 "새 기차 선택" 카드에 표시할 base 스탯 줄. 타입별 데이터가 override.
        public virtual IEnumerable<TrainStatLine> GetStatLines() => Enumerable.Empty<TrainStatLine>();
    }
}
