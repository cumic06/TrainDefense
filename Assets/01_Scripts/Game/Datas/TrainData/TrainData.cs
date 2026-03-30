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
        public SoundType AttackSoundType => attackSoundType;
        public TrainStatusData TrainStatusData => trainStatusData;

        [Obsolete("Use Prefab property instead")]
        public Train TrainPrefab => Prefab?.GetComponent<Train>();
        public bool IsMainTrain => isMainTrain;
    }
}
