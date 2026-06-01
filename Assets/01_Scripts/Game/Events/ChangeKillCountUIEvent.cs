namespace TrainDefense.Game.Events
{
    public class ChangeKillCountUIEvent
    {
        public int KillCount { get; }

        public ChangeKillCountUIEvent(int killCount)
        {
            KillCount = killCount;
        }
    }
}
