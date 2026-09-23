namespace TrainDefense.Game.Events
{
    public class StationPassedEvent
    {
        /// <summary>이번 판에서 지나온 역 수(첫 역이 1). 메타 재화 역 보상이 이 번호로 계산된다.</summary>
        public int PassedStationCount { get; }

        public StationPassedEvent(int passedStationCount)
        {
            PassedStationCount = passedStationCount;
        }
    }
}
