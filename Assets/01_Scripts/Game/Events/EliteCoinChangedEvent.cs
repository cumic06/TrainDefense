namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 엘리트 재화 보유량이 바뀔 때(획득/사용) 발행. 레드닷 등 재화 의존 UI 갱신용.
    /// </summary>
    public class EliteCoinChangedEvent
    {
        public int CurrentAmount { get; }

        public EliteCoinChangedEvent(int currentAmount)
        {
            CurrentAmount = currentAmount;
        }
    }
}
