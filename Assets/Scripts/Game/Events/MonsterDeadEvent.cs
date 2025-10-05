namespace TrainDefense.Game.Events
{
    public class MonsterDeadEvent
    {
        private int _money;
        public int Money => _money;

        public MonsterDeadEvent(int money)
        {
            _money = money;
        }
    }
}