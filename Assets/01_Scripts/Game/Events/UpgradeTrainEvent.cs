using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    public class UpgradeTrainEvent
    {
        private Train _train;
        private ITrainUpgradeData _upgradeData;

        public Train Train => _train;
        public ITrainUpgradeData UpgradeData => _upgradeData;

        public UpgradeTrainEvent(Train train, ITrainUpgradeData upgradeData)
        {
            _train = train;
            _upgradeData = upgradeData;
        }
    }
}
