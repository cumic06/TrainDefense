namespace TrainDefense.Game.Events
{
    public class TrainLevelUpEvent
    {
        private Train _train;
        private int _level;

        public Train Train => _train;
        public int Level => _level;

        public TrainLevelUpEvent(Train train, int level)
        {
            _train = train;
            _level = level;
        }
    }
}