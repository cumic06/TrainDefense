using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 역 상점에서 상품(포탑 구매/스탯 강화/엘리트 승격)을 골드로 구매했을 때 발행.
    /// 상점 구매는 TriChoiceSelectEvent를 타지 않으므로(레벨업 카드 전용 배선) 업적·분석용 전용 이벤트를 둔다.
    /// </summary>
    public class ShopOfferPurchasedEvent
    {
        public IChoiceOption Option { get; }
        public int Cost { get; }

        public ShopOfferPurchasedEvent(IChoiceOption option, int cost)
        {
            Option = option;
            Cost = cost;
        }
    }
}
