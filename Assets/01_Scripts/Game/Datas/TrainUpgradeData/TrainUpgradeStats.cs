using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 기본 Train 업그레이드 스탯 데이터
    /// </summary>
    [Serializable]
    public class TrainUpgradeStats
    {
        [Header("Upgrade Stats")]
        [SerializeField]
        private TrainStatusData statusUpgrade;

        public TrainStatusData StatusUpgrade => statusUpgrade;
    }
}