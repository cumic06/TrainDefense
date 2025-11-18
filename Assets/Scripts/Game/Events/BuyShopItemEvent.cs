namespace TrainDefense.Game.Events
{
    public class BuyShopItemEvent
    {
        private int _needMoney;
        private string _upgradeId;
        
        public int NeedMoney => _needMoney;
        public string UpgradeId => _upgradeId;

        public BuyShopItemEvent(int needMoney, string upgradeId)
        {
            _needMoney = needMoney;
            _upgradeId = upgradeId;
        }
    }
}