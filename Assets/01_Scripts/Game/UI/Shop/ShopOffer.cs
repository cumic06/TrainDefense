using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 역 상점 판매 상품 = 선택지 + 가격 정책.
    /// 가격은 매 접근 시 재계산한다(강화 구매로 포탑 레벨이 오르면 다른 슬롯 가격도 즉시 반영).
    /// </summary>
    public class ShopOffer
    {
        private readonly IChoiceOption _option;
        private readonly ShopOfferPricing _pricing;

        public ShopOffer(IChoiceOption option, ShopOfferPricing pricing)
        {
            _option = option;
            _pricing = pricing;
        }

        public IChoiceOption Option => _option;
        public int CurrentPrice => _pricing != null ? _pricing.GetPrice(_option) : 0;
        public bool IsValid => _option != null && _option.IsValid();
    }
}
