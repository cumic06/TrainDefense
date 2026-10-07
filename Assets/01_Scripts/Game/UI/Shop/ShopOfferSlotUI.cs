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

        [Header("Grade Border")]
        [Tooltip("카드 테두리를 덧칠하는 이미지(테두리 모양만 흰색으로 그려진 스프라이트). 등급 색으로 물들인다. 1등급·등급 없는 카드는 끈다")]
        [SerializeField]
        private Image gradeBorderImage;

        [Header("Price Color")]
        [Tooltip("보유 코인이 부족할 때 가격 텍스트에 적용할 색상")]
        [SerializeField]
        private Color insufficientColor = Color.red;
        #endregion

        // 개조 카드의 패시브 효과 줄 — 이름 줄보다 한 단계 작고 옅게.
        private const int PASSIVE_EFFECT_SIZE_PERCENT = 85;
        private const string PASSIVE_EFFECT_ALPHA_HEX = "#CC";

        // 등급별 테두리 색. 1등급은 카드 원래 진갈색 테두리를 그대로 둔다(가장 흔한 카드가 조용해야 높은 등급이 눈에 띈다).
        private static readonly Color Grade2BorderColor = new Color(0.25f, 0.65f, 0.21f);
        private static readonly Color Grade3BorderColor = new Color(0.18f, 0.48f, 0.88f);
        private static readonly Color Grade4BorderColor = new Color(0.61f, 0.31f, 0.84f);
        private static readonly Color Grade5BorderColor = new Color(0.94f, 0.54f, 0.11f);

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

            _ApplyGradeBorder(info != null ? info.Grade : 0);

            if (info != null)
            {
                itemNameText.text = info.Name;
                itemDescriptionText.text = info.Description;

                // 뽑힌 패시브도 적는다 — 엘리트는 고유 설명이 없어 이게 무엇인지 알 유일한 단서다. (꾹 누른 상세 창은 스탯만 띄운다)
                // 패시브 이름을 큰 줄로, 효과는 한 단계 작고 옅게 — 두 줄이 같은 무게면 무엇이 이름인지 안 읽힌다.
                if (!string.IsNullOrEmpty(info.PassiveName))
                {
                    string passiveText = string.IsNullOrEmpty(info.PassiveDescription)
                        ? info.PassiveName
                        : $"{info.PassiveName}\n<size={PASSIVE_EFFECT_SIZE_PERCENT}%><alpha={PASSIVE_EFFECT_ALPHA_HEX}>{info.PassiveDescription}</size>";
                    itemDescriptionText.text = string.IsNullOrEmpty(info.Description)
                        ? passiveText
                        : $"{info.Description}\n{passiveText}";
                }

                if (itemIconImage != null)
                {
                    itemIconImage.sprite = info.Icon;
                    itemIconImage.gameObject.SetActive(info.Icon != null);
                }
            }

            needMoneyText.text = $"<sprite name=\"Coin\"> {_offer.CurrentPrice.ToCommaString()}$";
            _ApplyPriceColor(UserDataManager.Instance != null ? UserDataManager.Instance.Coin : 0);
        }

        // 강화 카드 등급을 카드 테두리 색으로 보여 준다. 개조·포탑 구매 카드(등급 0)와 1등급은 기본 테두리.
        private void _ApplyGradeBorder(int grade)
        {
            if (gradeBorderImage == null)
                return;

            bool hasGradeColor = grade >= 2;
            gradeBorderImage.gameObject.SetActive(hasGradeColor);

            if (!hasGradeColor)
                return;

            gradeBorderImage.color = grade switch
            {
                2 => Grade2BorderColor,
                3 => Grade3BorderColor,
                4 => Grade4BorderColor,
                _ => Grade5BorderColor,
            };
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
