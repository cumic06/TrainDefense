namespace TrainDefense.Game.Events
{
    public class DecreaseCoinEvent
    {
        private int _coin;
        public int Coin => _coin;

        public DecreaseCoinEvent(int coin)
        {
            _coin = coin;
        }
    }
}