namespace TrainDefense.Game.Events
{
    public class BuyShopItemEvent
    {
        private int _needMoney;
        public int NeedMoney => _needMoney;

        public BuyShopItemEvent(int needMoney)
        {
            _needMoney = needMoney;
        }
    }
}