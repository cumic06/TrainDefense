using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Data
{
    [CreateAssetMenu(fileName = "TrainData", menuName = "Data/TrainData/TrainData")]
    public class TrainData : ScriptableObject
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string trainName;
        [SerializeField]
        private Sprite icon;
        [SerializeField]
        [TextArea(2, 4)]
        private string description;
        [SerializeField]
        private TrainStatusData trainStatusData;
        [SerializeField]
        private Train trainPrefab;
        [SerializeField]
        private bool isMainTrain;

        [Header("Upgrade Settings")]
        [SerializeField]
        private List<TrainUpgradeInfo> upgrades = new();
        #endregion

        public string Id => id;
        public string TrainName => trainName;
        public Sprite Icon => icon;
        public string Description => description;
        public TrainStatusData TrainStatusData => trainStatusData;
        public Train TrainPrefab => trainPrefab;
        public bool IsMainTrain => isMainTrain;
        public IReadOnlyList<TrainUpgradeInfo> Upgrades => upgrades;

        public TrainUpgradeInfo GetUpgrade(int level)
        {
            if (level < 0 || level >= upgrades.Count)
            {
                Debug.LogWarning($"Upgrade level {level} not found for {id}");
                return default;
            }
            return upgrades[level];
        }
    }

    [Serializable]
    public struct TrainStatusData
    {
        public int MaxHp;
    }

    [Serializable]
    public struct TrainUpgradeInfo
    {
        [Header("UI Info")]
        public Sprite Icon;
        public string UpgradeName;
        [TextArea(2, 4)]
        public string Description;

        [Header("Upgrade Stats")]
        public TrainStatusData StatusUpgrade;

        [Header("Extended Upgrade Data (Optional)")]
        [Tooltip("TurretTrain 등 추가 업그레이드 데이터가 필요한 경우 사용")]
        public TrainUpgradeExtension ExtensionData;
    }

    /// <summary>
    /// Train 타입별 추가 업그레이드 데이터를 담는 ScriptableObject
    /// </summary>
    [Serializable]
    public abstract class TrainUpgradeExtension
    {

    }
}