using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// RangeTrain의 추가 업그레이드 데이터
    /// </summary>
    [Serializable]
    public class RangeTrainUpgradeExtension : TrainUpgradeExtension
    {
        public RangeTrainStatus RangeStatusUpgrade;
    }
}
