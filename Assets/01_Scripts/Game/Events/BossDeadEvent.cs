namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 보스 사망 이벤트: 보스가 죽으면 스폰 속도를 원래대로 복구
    /// </summary>
    public class BossDeadEvent
    {
        public Game.Boss Boss { get; }

        public BossDeadEvent(Game.Boss boss)
        {
            Boss = boss;
        }
    }
}

