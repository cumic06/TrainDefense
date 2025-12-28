namespace TrainDefense.Game.Events
{
    public class ChangeCoinUIEvent
    {
        private int _beforeCoin;
        private int _afterCoin;
        public int BeforeCoin => _beforeCoin;
        public int AfterCoin => _afterCoin;

        public ChangeCoinUIEvent(int beforeCoin, int afterCoin)
        {
            _beforeCoin = beforeCoin;
            _afterCoin = afterCoin;
        }
    }
}

