namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 트레인이 스폰될 때 발행되는 이벤트. 도감 발견 추적에 사용한다.
    /// </summary>
    public class TrainSpawnedEvent
    {
        private string _trainId;

        public string TrainId => _trainId;

        public TrainSpawnedEvent(string trainId)
        {
            _trainId = trainId;
        }
    }
}
