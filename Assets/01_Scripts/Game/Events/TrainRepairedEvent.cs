namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 기차 수리가 실행됐을 때 발행(현재 발행처 = 만렙 보상 긴급 수리 카드). 수리 행동 분석에 사용한다.
    /// (수리 구매는 ChangeCoinUIEvent 만으로는 구매 종류를 식별할 수 없어 전용 이벤트를 둔다.)
    /// </summary>
    public class TrainRepairedEvent
    {
        public int Cost { get; }
        public int StationCount { get; }

        public TrainRepairedEvent(int cost, int stationCount)
        {
            Cost = cost;
            StationCount = stationCount;
        }
    }
}
