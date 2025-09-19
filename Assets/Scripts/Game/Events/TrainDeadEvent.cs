namespace TrainDefense.Game.Events
{
    public class TrainDeadEvent
    {
        private Train _train;

        public Train Train => _train;

        public TrainDeadEvent(Train train)
        {
            _train = train;
        }
    }
}