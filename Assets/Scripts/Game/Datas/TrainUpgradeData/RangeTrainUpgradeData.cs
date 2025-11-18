using UnityEngine;
using System;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class RangeTrainUpgradeData : ITrainUpgradeData, IDescribableData, IIconData
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

        [Header("Upgrade Stats")]
        [SerializeField]
        private TrainStatusData statusUpgrade;

        [Header("Range Train Specific Upgrade")]
        [SerializeField]
        private RangeTrainStatus rangeStatusUpgrade;
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
                        Debug.LogWarning($"RangeTrainUpgradeData [{id}]: Icon not found at 'Sprite/{iconId}'");
                    }
                }
                return icon;
            }
        }
        #endregion

        #region ITrainUpgradeData
        public string UpgradeName => name;
        public TrainStatusData StatusUpgrade => statusUpgrade;
        #endregion

        public RangeTrainStatus RangeStatusUpgrade => rangeStatusUpgrade;
    }
}
