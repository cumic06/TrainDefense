namespace TrainDefense.Game.Events
{
    public class MonsterDeadEvent
    {
        private int _coin;
        public int Coin => _coin;

        public MonsterDeadEvent(int coin)
        {
            _coin = coin;
        }
    }
}