namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 스킬트리 리스펙(전체 초기화 + 전액 환급)이 완료됐을 때 발행. UI 갱신·연출 전용.
    /// </summary>
    public class SkillTreeResetEvent
    {
        public int RefundedPoints { get; }

        public SkillTreeResetEvent(int refundedPoints)
        {
            RefundedPoints = refundedPoints;
        }
    }
}
