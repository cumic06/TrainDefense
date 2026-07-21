namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 스킬 포인트 보유량이 바뀔 때(획득/사용/환급) 발행. 레드닷 등 재화 의존 UI 갱신용.
    /// </summary>
    public class SkillPointChangedEvent
    {
        public int CurrentAmount { get; }

        public SkillPointChangedEvent(int currentAmount)
        {
            CurrentAmount = currentAmount;
        }
    }
}
