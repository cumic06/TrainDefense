namespace TrainDefense.Game.Events
{
    public class IncreaseCoinEvent
    {
        private int _coin;
        public int Coin => _coin;

        public IncreaseCoinEvent(int coin)
        {
            _coin = coin;
        }
    }
}
