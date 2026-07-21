using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 역 상점의 판매 슬롯 1칸 — 상품(ShopOffer) 표시와 구매를 담당한다.
    /// 구매 즉시 슬롯 상품이 교체되므로 홀드 연속 구매는 오구매 위험이 있어 단발 클릭만 받는다.
    /// </summary>
    public class ShopOfferSlotUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Button buyButton;
        [SerializeField]
        private Image itemIconImage;
        [SerializeField]
        private TextMeshProUGUI itemNameText;
        [SerializeField]
        private TextMeshProUGUI itemDescriptionText;
        [SerializeField]
        private TextMeshProUGUI needMoneyText;

        [Header("Price Color")]
        [Tooltip("보유 코인이 부족할 때 가격 텍스트에 적용할 색상")]
        [SerializeField]
        private Color insufficientColor = Color.red;
        #endregion

        private ShopOffer _offer;
        private System.Action<ShopOfferSlotUI> _onPurchased;
        private Color _priceOriginalColor;
        private bool _initialized;

        public ShopOffer CurrentOffer => _offer;
        public bool HasValidOffer => _offer != null && _offer.IsValid;

        // 생성 직후 1회 호출. onPurchased = 구매 성공 직후 슬롯 교체를 담당하는 상점 콜백.
        public void Initialize(System.Action<ShopOfferSlotUI> onPurchased)
        {
            _onPurchased = onPurchased;

            if (_initialized)
                return;

            _initialized = true;
            _priceOriginalColor = needMoneyText.color;
            buyButton.onClick.AddListener(_OnBuyButtonClick);
            GameEventSystem.Subscribe<ChangeCoinUIEvent>(_OnChangeCoin);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(_OnChangeCoin);
        }

        // 상품 교체 + 즉시 표시 갱신.
        public void SetOffer(ShopOffer offer)
        {
            _offer = offer;
            gameObject.SetActive(true);
            Refresh();
        }

        // 팔 상품이 소진된 슬롯은 비운다.
        public void SetEmpty()
        {
            _offer = null;
            gameObject.SetActive(false);
        }

        // 가격·설명 재표시 (다른 슬롯 구매로 레벨/코인이 변한 뒤 호출).
        public void Refresh()
        {
            if (_offer == null)
                return;

            var info = TriChoiceManager.Instance != null
                ? TriChoiceManager.Instance.GetChoiceUIInfo(_offer.Option)
                : null;

            if (info != null)
            {
                itemNameText.text = info.Name;
                itemDescriptionText.text = info.Description;

                if (itemIconImage != null)
                {
                    itemIconImage.sprite = info.Icon;
                    itemIconImage.gameObject.SetActive(info.Icon != null);
                }
            }

            needMoneyText.text = $"<sprite name=\"Coin\"> {_offer.CurrentPrice.ToCommaString()}$";
            _ApplyPriceColor(UserDataManager.Instance != null ? UserDataManager.Instance.Coin : 0);
        }

        private void _OnBuyButtonClick()
        {
            if (_offer == null || !_offer.IsValid)
                return;

            int cost = _offer.CurrentPrice;

            // 코인이 부족하면 가격 텍스트가 이미 빨간색이므로 구매만 막는다. (차감·검증 = UserDataManager 단일 경로)
            if (UserDataManager.Instance == null || !UserDataManager.Instance.TrySpendCoin(cost))
                return;

            SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ItemBuy, ignoreSuppress: true);

            // 상점 구매는 TriChoiceSelectEvent를 발행하지 않는다(레벨업 카드 전용 배선이라 부작용 위험).
            // 업적·분석은 전용 이벤트(ShopOfferPurchasedEvent)로 알린다.
            _offer.Option.Execute();
            GameEventSystem.Publish(new ShopOfferPurchasedEvent(_offer.Option, cost));
            _onPurchased?.Invoke(this);
        }

        private void _OnChangeCoin(ChangeCoinUIEvent changeCoinEvent)
        {
            // 코인 변경은 전투 중 처치마다 발행되는 고빈도 이벤트 — 슬롯이 꺼져 있으면(상점 미표시) 스킵.
            if (!gameObject.activeInHierarchy)
                return;

            // 구독 순서에 의존하지 않도록 이벤트의 AfterCoin을 직접 기준으로 사용한다.
            _ApplyPriceColor(changeCoinEvent.AfterCoin);
        }

        private void _ApplyPriceColor(int coin)
        {
            if (_offer == null)
                return;

            needMoneyText.color = coin >= _offer.CurrentPrice ? _priceOriginalColor : insufficientColor;
        }
    }
}
