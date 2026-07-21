namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 레벨업 카드에서 스탯 업그레이드(110xxx)를 선택했을 때 발행 — 구독자(TrainUpgradeManager)가 레벨 증가·적용을 담당.
    /// (옛 BuyShopItemEvent. 상점 고정 슬롯이 사라지고 무료 레벨업 카드가 유일한 발행처가 되면서 비용 개념과 함께 개명)
    /// </summary>
    public class StatUpgradeSelectEvent
    {
        private string _upgradeId;

        public string UpgradeId => _upgradeId;

        public StatUpgradeSelectEvent(string upgradeId)
        {
            _upgradeId = upgradeId;
        }
    }
}
