using UnityEngine;


namespace TrainDefense.Game.Data
{
    [CreateAssetMenu(fileName = "TurretTrainUpgradeData", menuName = "Data/TrainUpgradeData/TurretTrainUpgradeData")]
    public class TurretTrainUpgradeData : TrainUpgradeData
    {
        [SerializeField]
        private TurretTrainStatus turretTrainStatusData;

        public TurretTrainStatus TurretTrainStatusData => turretTrainStatusData;
    }
}