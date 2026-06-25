namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 튜토리얼 시퀀스가 시작될 때 발행. 튜토리얼 단계별 이탈률 분석에 사용한다.
    /// </summary>
    public class TutorialStartEvent
    {
        public string SequenceId { get; }

        public TutorialStartEvent(string sequenceId)
        {
            SequenceId = sequenceId;
        }
    }
}
