namespace TrainDefense.Game.Events
{
    public class AddCoinEvent
    {
        private int _coin;
        public int Coin => _coin;

        public AddCoinEvent(int coin)
        {
            _coin = coin;
        }
    }
}
