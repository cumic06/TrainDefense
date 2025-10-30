using UnityEngine;
using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// RangeTrain 전용 업그레이드 데이터
    /// </summary>
    [Serializable]
    public class RangeTrainUpgradeData : ITrainUpgradeData
    {
        #region Fields
        [SerializeField]
        private string id;
        [Header("UI Info")]
        [SerializeField]
        private Sprite icon;

        [SerializeField]
        private string upgradeName;

        [SerializeField]
        [TextArea(2, 4)]
        private string description;

        [Header("Upgrade Stats")]
        [SerializeField]
        private TrainStatusData statusUpgrade;

        [Header("Range Train Specific Upgrade")]
        [SerializeField]
        private RangeAttackTrainStatus rangeStatusUpgrade;
        #endregion

        public string Id => id;
        public Sprite Icon => icon;
        public string UpgradeName => upgradeName;
        public string Description => description;
        public TrainStatusData StatusUpgrade => statusUpgrade;
        public RangeAttackTrainStatus RangeStatusUpgrade => rangeStatusUpgrade;
    }
}

