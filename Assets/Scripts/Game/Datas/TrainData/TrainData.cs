using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [System.Serializable]
    public class TrainData
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
        private List<TrainUpgradeData> upgrades = new();
        #endregion

        public string Id => id;
        public string TrainName => trainName;
        public Sprite Icon => icon;
        public string Description => description;
        public TrainStatusData TrainStatusData => trainStatusData;
        public Train TrainPrefab => trainPrefab;
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