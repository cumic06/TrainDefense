using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// TurretTrain 업그레이드 스탯 데이터
    /// </summary>
    [Serializable]
    public class TurretTrainUpgradeStats
    {
        [Header("Upgrade Stats")]
        [SerializeField]
        private TrainStatusData statusUpgrade;

        [Header("Turret Train Specific Upgrade")]
        [SerializeField]
        private TurretTrainStatus turretStatusUpgrade;

        [Header("Passive Skill (Optional)")]
        [SerializeField]
        private string passiveSkillDataId;

        public TrainStatusData StatusUpgrade => statusUpgrade;
        public TurretTrainStatus TurretStatusUpgrade => turretStatusUpgrade;
        public string PassiveSkillDataId => passiveSkillDataId;

        public TurretTrainUpgradeStats() { }

        // 런타임 단일 스탯 강화(상점 개별 강화)용
        public TurretTrainUpgradeStats(TurretTrainStatus turretStatus)
        {
            turretStatusUpgrade = turretStatus;
        }
    }
}

