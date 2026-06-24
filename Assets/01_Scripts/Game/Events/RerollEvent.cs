namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 삼중택일 리롤이 성공했을 때 발행. 리롤 사용 행동 분석에 사용한다.
    /// (TriChoiceManager 는 ChangeCoinUIEvent 만 발행해 리롤 여부를 식별할 수 없어 전용 이벤트를 둔다.)
    /// </summary>
    public class RerollEvent
    {
        public int Cost { get; }
        public bool IsFree { get; }

        public RerollEvent(int cost, bool isFree)
        {
            Cost = cost;
            IsFree = isFree;
        }
    }
}
