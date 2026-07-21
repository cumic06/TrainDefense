using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// RangeTrain 업그레이드 스탯 데이터
    /// </summary>
    [Serializable]
    public class RangeTrainUpgradeStats
    {
        [Header("Upgrade Stats")]
        [SerializeField]
        private TrainStatusData statusUpgrade;

        [Header("Range Train Specific Upgrade")]
        [SerializeField]
        private RangeTrainStatus rangeStatusUpgrade;

        public TrainStatusData StatusUpgrade => statusUpgrade;
        public RangeTrainStatus RangeStatusUpgrade => rangeStatusUpgrade;

        public RangeTrainUpgradeStats() { }

        // 런타임 단일 스탯 강화(상점 개별 강화)용
        public RangeTrainUpgradeStats(RangeTrainStatus rangeStatus)
        {
            rangeStatusUpgrade = rangeStatus;
        }
    }
}

