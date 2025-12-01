using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// TurretTrain의 추가 업그레이드 데이터
    /// </summary>
    [Serializable]
    public class TurretTrainUpgradeExtension : TrainUpgradeExtension
    {
        public TurretTrainStatus TurretStatusUpgrade;
    }
}
