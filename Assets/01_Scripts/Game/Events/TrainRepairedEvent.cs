namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 상점 '기차 수리' 구매가 성공했을 때 발행. 수리 구매 행동 분석에 사용한다.
    /// (ShopRepairItemUI 는 ChangeCoinUIEvent 만 발행해 구매 종류를 식별할 수 없어 전용 이벤트를 둔다.)
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
