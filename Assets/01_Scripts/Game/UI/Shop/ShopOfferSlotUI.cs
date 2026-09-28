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
    /// 구매하면 슬롯이 비워지고(교체 없음), 새 상품은 리롤(전체 재추첨)로만 채워진다.
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
        private TrainDetailPopupUI _detailPopup;
        private LongPressHandler _longPressHandler;

        public ShopOffer CurrentOffer => _offer;
        public bool HasValidOffer => _offer != null && _offer.IsValid;

        // 생성 직후 1회 호출. onPurchased = 구매 성공 직후 슬롯 교체를 담당하는 상점 콜백.
        // detailPopup = 포탑 카드를 꾹 눌렀을 때 스탯을 띄울 상점 공용 팝업.
        public void Initialize(System.Action<ShopOfferSlotUI> onPurchased, TrainDetailPopupUI detailPopup)
        {
            _onPurchased = onPurchased;
            _detailPopup = detailPopup;

            if (_initialized)
                return;

            _initialized = true;
            _priceOriginalColor = needMoneyText.color;
            buyButton.onClick.AddListener(_OnBuyButtonClick);
            _longPressHandler = buyButton.gameObject.AddComponent<LongPressHandler>();
            _longPressHandler.Initialize(_OnLongPress, _OnLongPressRelease);
            GameEventSystem.Subscribe<ChangeCoinUIEvent>(_OnChangeCoin);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(_OnChangeCoin);
        }

        // 상품 교체 + 즉시 표시 갱신. (슬롯은 재사용되므로 품절 상태를 반드시 되돌린다)
        public void SetOffer(ShopOffer offer)
        {
            _offer = offer;
            buyButton.interactable = true;
            gameObject.SetActive(true);
            Refresh();
        }

        // 팔 상품이 소진된 슬롯은 비운다.
        public void SetEmpty()
        {
            _offer = null;
            gameObject.SetActive(false);
        }

        // 다른 구매로 무효해진 상품은 화면에 남기되 구매만 막는다 — 눈앞에서 사라지면 오동작처럼 보인다.
        // 실제 제거는 리롤/새 상점의 재추첨이 담당한다(무효 상품은 후보에서 걸러짐).
        public void SetUnavailable()
        {
            buyButton.interactable = false;
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
            // 스탯을 보려고 꾹 누른 뒤 뗀 것은 구매가 아니다.
            if (_longPressHandler != null && _longPressHandler.IsLongPressed)
                return;

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

        // 포탑 카드만 스탯을 띄운다: 강화 카드 = 대상 포탑의 지금 → 구매 후 스탯, 새 포탑 = 생성 직후 스탯, 승격 = 승격 후 스탯.
        private void _OnLongPress(Vector2 screenPosition, Camera eventCamera)
        {
            if (_detailPopup == null || _offer == null)
                return;

            var databaseManager = DatabaseManager.Instance;

            switch (_offer.Option)
            {
                case TrainStatUpgradeChoice statUpgradeChoice when statUpgradeChoice.TargetTrain != null:
                    _detailPopup.ShowUpgradePreview(statUpgradeChoice.TargetTrain, statUpgradeChoice.GetUpgradedStatDetails);
                    break;
                case AddTrainChoice addTrainChoice when databaseManager != null:
                    _detailPopup.ShowPreview(databaseManager.GetTrainData(addTrainChoice.TrainDataId), null);
                    break;
                case EliteTrainChoice eliteTrainChoice when databaseManager != null:
                    _detailPopup.ShowPreview(databaseManager.GetTrainData(eliteTrainChoice.EliteTrainDataId), eliteTrainChoice.FindBaseTrain());
                    break;
                default:
                    return;
            }

            // 이름은 카드에 적혀 있다.
            _detailPopup.SetNameVisible(false);

            // 카드 오른쪽 아래(카드 판과 기차 그림의 구분선에 걸치게)에 띄운다 — 카드가 화면 위쪽이라 위에는 자리가 없다.
            _detailPopup.PlaceBeside((RectTransform)transform);
        }

        private void _OnLongPressRelease()
        {
            if (_detailPopup != null && _detailPopup.gameObject.activeSelf)
                _detailPopup.Hide();
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
