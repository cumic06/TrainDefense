namespace TrainDefense.Game.Events
{
    public class MonsterDeadEvent
    {
        public string MonsterId { get; }
        public bool IsElite { get; }

        public MonsterDeadEvent(string monsterId, bool isElite)
        {
            MonsterId = monsterId;
            IsElite = isElite;
        }
    }
}
