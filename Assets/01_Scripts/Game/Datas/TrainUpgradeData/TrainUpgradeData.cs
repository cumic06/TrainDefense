using UnityEngine;
using System;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Train 업그레이드 데이터 (기본 Train 및 MainTrain용)
    /// </summary>
    [Serializable]
    public class TrainUpgradeData : ITrainUpgradeData, IDescribableData, IIconData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string iconId;
        [Header("UI Info")]
        private Sprite icon;

        [SerializeField]
        private string name;

        [SerializeField]
        [TextArea(2, 4)]
        private string description;

        [SerializeField]
        [Tooltip("업그레이드 스탯 배열 (인덱스 = 레벨, 예: [0] = 레벨 0, [1] = 레벨 1)")]
        private TrainUpgradeStats[] upgradeStats;
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
                        Debug.LogWarning($"TrainUpgradeData [{id}]: Icon not found at 'Sprite/{iconId}'");
                    }
                }
                return icon;
            }
        }
        #endregion

        #region ITrainUpgradeData
        public string UpgradeName => name;
        public TrainStatusData GetStatusUpgrade(int level)
        {
            var stats = GetStatsForLevel(level);
            return stats?.StatusUpgrade ?? default;
        }
        public int MaxLevel => upgradeStats?.Length ?? 0;
        #endregion

        private TrainUpgradeStats GetStatsForLevel(int targetLevel)
        {
            if (upgradeStats == null || upgradeStats.Length == 0) return null;
            if (targetLevel < 0 || targetLevel >= upgradeStats.Length) return null;
            return upgradeStats[targetLevel];
        }
    }
}
