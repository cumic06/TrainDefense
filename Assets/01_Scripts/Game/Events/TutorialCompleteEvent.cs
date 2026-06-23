namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 튜토리얼 시퀀스가 완료(또는 스킵으로 종료)될 때 발행. 튜토리얼 이탈/완주 분석에 사용한다.
    /// </summary>
    public class TutorialCompleteEvent
    {
        public string SequenceId { get; }

        public TutorialCompleteEvent(string sequenceId)
        {
            SequenceId = sequenceId;
        }
    }
}
