namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 스킬 노드 습득(레벨업)이 성공했을 때 발행. UI 갱신·습득 연출·분석 전용 — 상태 변경 구독 금지.
    /// </summary>
    public class SkillNodeAcquiredEvent
    {
        public string NodeId { get; }
        public int Cost { get; }
        public int NewLevel { get; }

        public SkillNodeAcquiredEvent(string nodeId, int cost, int newLevel)
        {
            NodeId = nodeId;
            Cost = cost;
            NewLevel = newLevel;
        }
    }
}
