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
        private Sprite icon;
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

        [Header("Upgrade Settings")]
        [SerializeField]
        private List<TrainUpgradeData> upgrades = new();
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
        [ShowInInspector, ReadOnly]
        public Sprite Icon
        {
            get
            {
                if (icon == null && !string.IsNullOrEmpty(iconId))
                {
                    icon = Resources.Load<Sprite>($"Sprite/{iconId}");
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
        public TrainStatusData TrainStatusData => trainStatusData;
        
        [Obsolete("Use Prefab property instead")]
        public Train TrainPrefab => Prefab?.GetComponent<Train>();
        public bool IsMainTrain => isMainTrain;
        public IReadOnlyList<ITrainUpgradeData> Upgrades => upgrades.Cast<ITrainUpgradeData>().ToList();

        public ITrainUpgradeData GetUpgrade(int level)
        {
            if (level < 0 || level >= upgrades.Count)
            {
                Debug.LogWarning($"Upgrade level {level} not found for {id}");
                return null;
            }
            return upgrades[level];
        }
    }

    [Serializable]
    public struct TrainStatusData
    {
        public int MaxHp;
    }

    /// <summary>
    /// Train 타입별 추가 업그레이드 데이터
    /// </summary>
    [System.Serializable]
    public abstract class TrainUpgradeExtension
    {

    }
}
