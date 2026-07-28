using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;

namespace TrainDefense.Game.UI
{
    public class ShopUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private float shopMoveXStartPos = -100;
        [SerializeField]
        private float shopMoveXEndPos;
        [SerializeField]
        private float shopMoveInterval = 1f;
        [SerializeField]
        private Image backgroundImage;
        [SerializeField]
        private ShopButtonUI shopButtonUI;
        [SerializeField]
        private GameObject groupCoin;

        [Header("Item Drop-In Settings")]
        [SerializeField]
        [Tooltip("버튼이 떨어지기 시작하는 높이(위쪽으로의 오프셋, px)")]
        private float itemDropHeight = 250f;
        [SerializeField]
        [Tooltip("버튼 한 개가 제자리로 떨어지는 시간(초)")]
        private float itemDropDuration = 0.35f;
        [SerializeField]
        [Tooltip("버튼이 순차적으로 등장하는 간격(초)")]
        private float itemDropStagger = 0.07f;

        [Header("Offer Slots")]
        [SerializeField]
        [Tooltip("역 상점에 제시되는 판매 슬롯 수. 구매한 상품은 슬롯에서 사라지고, 리롤로만 전체 재추첨된다")]
        private int offerSlotCount = 5;
        [SerializeField]
        private ShopOfferSlotUI offerSlotPrefab;
        [SerializeField]
        private ShopOfferPricing offerPricing = new();

        [Header("Reroll")]
        [SerializeField]
        [Tooltip("이번 역 상점의 슬롯 전체를 다시 추첨하는 버튼")]
        private Button rerollButton;
        [SerializeField]
        private TextMeshProUGUI rerollLabelText;
        [SerializeField]
        [Tooltip("첫 리롤 비용 = 이 값 × (누적 상점 방문 수 + 1). 수입이 커지는 후반에 리롤이 껌값이 되지 않게 진행도 비례")]
        private int rerollBaseCostPerStation = 20;
        [SerializeField]
        [Tooltip("리롤할 때마다 현재 비용에 더해지는 증가분 = 이 값 × (누적 상점 방문 수 + 1). 상점을 새로 열면 첫 비용으로 초기화")]
        private int rerollCostIncreasePerStation = 10;
        [SerializeField]
        [Tooltip("보유 코인이 부족할 때 리롤 비용 텍스트에 적용할 색상")]
        private Color rerollInsufficientColor = Color.red;
        [SerializeField]
        [Tooltip("리롤 비용 표기 크기(라벨 대비 배율)")]
        private float rerollCostFontScale = 0.5f;
        #endregion

        private RectTransform _rectTransform;
        private GridLayoutGroup _itemGrid;
        private RectTransform _itemGridRect;
        private RectTransform[] _itemRects;
        private Vector2[] _itemFinalPositions;

        private readonly List<ShopOfferSlotUI> _offerSlotUIs = new();

        private int _currentRerollCost;
        private string _rerollLabelPrefix;
        private Color _rerollLabelOriginalColor;

        private bool isShopOpen = false;
        public bool IsShopOpen => isShopOpen;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _itemGrid = GetComponentInChildren<GridLayoutGroup>(true);
            if (_itemGrid != null)
            {
                _itemGridRect = _itemGrid.GetComponent<RectTransform>();
            }
        }

        private void Start()
        {
            // 업그레이드가 실제로 적용된 이후에만 상점 UI를 갱신하기 위해 UpgradeAppliedEvent를 구독
            shopButtonUI.OnClickShopButton += _ShopOpenHandler;
            GameEventSystem.Subscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<ChangeCoinUIEvent>(_OnChangeCoin);

            if (rerollButton != null)
            {
                rerollButton.onClick.AddListener(_OnRerollButtonClick);

                if (rerollLabelText == null)
                    rerollLabelText = rerollButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (rerollLabelText != null)
            {
                _rerollLabelPrefix = rerollLabelText.text;
                _rerollLabelOriginalColor = rerollLabelText.color;
            }
        }

        private void OnDestroy()
        {
            shopButtonUI.OnClickShopButton -= _ShopOpenHandler;
            GameEventSystem.Unsubscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(_OnChangeCoin);
        }

        private void OnUpgradeApplied(UpgradeAppliedEvent upgradeAppliedEvent)
        {
            foreach (var slotUI in _offerSlotUIs)
            {
                if (slotUI != null && slotUI.gameObject.activeSelf)
                    slotUI.Refresh();
            }
        }

        private void _OnInspectionStart(InspectionStartEvent inspectionStartEvent)
        {
            // 역 도착 시점에 새로 추첨. (상점은 역당 1회 — 닫으면 바로 출발이라 재오픈은 없다)
            // 리롤 비용은 상점이 열릴 때마다(맵 선택 상점 포함) 기본값으로 초기화된다.
            _currentRerollCost = rerollBaseCostPerStation * _GetRerollStationMultiplier();
            _RebuildOfferSlots();
            _RefreshRerollUI();

            if (!isShopOpen)
            {
                _OpenShop();
                shopButtonUI.gameObject.SetActive(true);
            }
        }

        private void _ShopOpenHandler()
        {
            if (isShopOpen)
            {
                _CloseShop();
                shopButtonUI.gameObject.SetActive(false);
            }
            else
            {
                _OpenShop();
            }
        }

        private async void _OpenShop()
        {
            if (isShopOpen)
            {
                return;
            }

            gameObject.SetActive(true);

            if (groupCoin != null) groupCoin.SetActive(true);

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            // 슬라이드인 동안 버튼이 제자리에 보이지 않도록 미리 숨겨둠
            _PrepareItemsHidden();

            // 1) 기존 상점 패널 슬라이드인 연출 먼저
            await _rectTransform.DOAnchorPosX(shopMoveXEndPos, shopMoveInterval).SetUpdate(true);

            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(true);
            }

            // 2) 패널이 자리잡은 뒤 버튼들을 위에서 순차적으로 떨어뜨림
            _PlayItemsDropIn();

            isShopOpen = true;
        }

        /// <summary>
        /// 버튼들의 최종 배치 위치를 확정·캐싱한 뒤, 슬라이드인 동안 보이지 않도록
        /// 위쪽으로 올리고 scale 0으로 숨겨둔다.
        /// GridLayoutGroup이 위치를 자동 제어하므로 위치 확정 후 Grid를 꺼서
        /// 이후 DOTween이 위치를 제어하도록 한다.
        /// </summary>
        private void _PrepareItemsHidden()
        {
            if (_itemGrid == null || _itemGridRect == null || _itemGridRect.childCount == 0)
            {
                return;
            }

            // 1) GridLayoutGroup으로 각 버튼의 최종 배치 위치를 확정
            //    활성화 직후 호출되므로 상위 레이아웃까지 강제 갱신해 첫 진입에서도 정확한 좌표를 얻음
            _itemGrid.enabled = true;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_itemGridRect);

            // Grid의 모든 자식(판매 슬롯)을 대상으로 위치를 캐싱한다.
            int count = _itemGridRect.childCount;
            _itemRects = new RectTransform[count];
            _itemFinalPositions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                _itemRects[i] = (RectTransform)_itemGridRect.GetChild(i);
                _itemFinalPositions[i] = _itemRects[i].anchoredPosition;
            }

            // 2) Grid를 꺼서 트윈이 위치를 덮어쓰지 않도록 함
            _itemGrid.enabled = false;

            // 3) 위로 올린 뒤 scale 0으로 숨김(마스크가 없어 위에 떠 있으면 보이므로)
            for (int i = 0; i < count; i++)
            {
                _itemRects[i].DOKill();
                _itemRects[i].anchoredPosition = _itemFinalPositions[i] + Vector2.up * itemDropHeight;
                _itemRects[i].localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// 미리 숨겨둔 버튼들을 위쪽에서 하나씩 순차적으로 떨어뜨리는 등장 연출.
        /// </summary>
        private void _PlayItemsDropIn()
        {
            if (_itemRects == null)
            {
                return;
            }

            for (int i = 0; i < _itemRects.Length; i++)
            {
                var rect = _itemRects[i];
                var finalPos = _itemFinalPositions[i];

                rect.DOAnchorPos(finalPos, itemDropDuration)
                    .SetDelay(i * itemDropStagger)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .OnStart(() => rect.localScale = Vector3.one);
            }
        }

        private async void _CloseShop()
        {
            if (!isShopOpen)
            {
                return;
            }

            await _rectTransform.DOAnchorPosX(shopMoveXStartPos, shopMoveInterval).SetUpdate(true);

            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
            }

            isShopOpen = false;

            if (groupCoin != null) groupCoin.SetActive(false);

            // 스테이지 선택용 상점이라면 전투로 복귀하지 않고 스테이지 선택 UI로 전환되어야 하므로
            // InspectionEndEvent 발행 전에 대기 여부를 먼저 확인한다.
            bool stageSelectionPending = StageManager.Instance != null
                && StageManager.Instance.IsStageSelectionPending;

            GameEventSystem.Publish(new InspectionEndEvent());

            if (!stageSelectionPending)
            {
                // 상점 퇴장 연출(상점 오브젝트가 뒤로 슬라이드되며 사라짐)이 끝난 뒤 전투를 재개한다.
                if (TimelineManager.Instance != null)
                    TimelineManager.Instance.StartShopExitTimeline(() => GameEventSystem.Publish(new EngageStartEvent()));
                else
                    GameEventSystem.Publish(new EngageStartEvent());
            }
        }

        // 판매 슬롯(offerSlotCount개) 전체를 현재 상태 기준으로 새로 추첨한다. (역 도착 + 리롤)
        private void _RebuildOfferSlots()
        {
            if (TriChoiceManager.Instance == null)
                return;

            // 전체 재추첨이므로 카드별 랜덤 캐시(엘리트 부여 스킬 등)를 비우고 새로 뽑는다.
            // ★ 표시 중인 상품을 유지한 채 Clear하면 표시된 스킬과 실제 부여 스킬이 어긋나므로
            //   반드시 모든 슬롯을 다시 채우는 이 경로에서만 호출한다.
            TriChoiceManager.Instance.ClearSelectedChoiceData();

            _EnsureOfferSlots();

            var entries = TriChoiceManager.Instance.GetShopChoices(offerSlotCount);

            for (int i = 0; i < _offerSlotUIs.Count; i++)
            {
                if (i < entries.Count && entries[i]?.Option != null)
                    _offerSlotUIs[i].SetOffer(new ShopOffer(entries[i].Option, offerPricing));
                else
                    _offerSlotUIs[i].SetEmpty();
            }
        }

        // 슬롯 UI는 최초 1회만 프리팹에서 생성해 계속 재사용한다. (역마다 파괴/재생성하지 않음)
        private void _EnsureOfferSlots()
        {
            if (_offerSlotUIs.Count >= offerSlotCount)
                return;

            var slotPrefab = _GetOfferSlotPrefab();

            if (slotPrefab == null || _itemGridRect == null)
                return;

            while (_offerSlotUIs.Count < offerSlotCount)
            {
                var slotUI = Instantiate(slotPrefab, _itemGridRect);
                slotUI.Initialize(_OnOfferPurchased);
                _offerSlotUIs.Add(slotUI);
            }
        }

        private ShopOfferSlotUI _GetOfferSlotPrefab()
        {
            if (offerSlotPrefab != null)
                return offerSlotPrefab;

            // 프리팹 미배선 안전망. 정상 경로는 인스펙터 배선이므로 경고로 알린다.
            var loadedPrefab = Resources.Load<ShopOfferSlotUI>("Prefabs/UI/ShopOfferSlot");

            if (loadedPrefab == null)
                Debug.LogError("ShopUI: offerSlotPrefab이 배선되지 않았고 Resources/Prefabs/UI/ShopOfferSlot도 없습니다.");
            else
                Debug.LogWarning("ShopUI: offerSlotPrefab 미배선 — Resources 폴백을 사용합니다.");

            return loadedPrefab;
        }

        // 구매 직후: 구매한 슬롯은 비우고(교체 없음), 나머지 슬롯은 무효화·가격 변화를 반영한다.
        // 새 상품은 리롤(전체 재추첨)로만 채워진다.
        private void _OnOfferPurchased(ShopOfferSlotUI purchasedSlot)
        {
            purchasedSlot.SetEmpty();

            foreach (var slotUI in _offerSlotUIs)
            {
                if (slotUI == null || slotUI == purchasedSlot || !slotUI.gameObject.activeSelf)
                    continue;

                // 다른 구매로 무효해진 상품(예: 엘리트 승격으로 교체된 포탑의 강화, 상한 도달)은 비우지 않고
                // 구매만 막는다 — 눈앞에서 상품이 사라지면 오동작처럼 보인다. 리롤/새 상점부터는 아예 안 뜬다.
                if (!slotUI.HasValidOffer)
                    slotUI.SetUnavailable();
                else
                    slotUI.Refresh();
            }
        }

        // 리롤: 코인을 차감하고 슬롯 전체를 다시 추첨한다. 비용은 리롤마다 역 수 비례 증가분만큼 오른다.
        private void _OnRerollButtonClick()
        {
            // 코인 부족 시엔 버튼 interactable이 이미 꺼져 있지만, 방어적으로 다시 확인한다.
            // 차감·검증 = UserDataManager 단일 경로 (상점 슬롯 구매와 동일).
            if (UserDataManager.Instance == null || !UserDataManager.Instance.TrySpendCoin(_currentRerollCost))
                return;

            _currentRerollCost += rerollCostIncreasePerStation * _GetRerollStationMultiplier();

            _RebuildOfferSlots();
            _RefreshRerollUI();
        }

        // 리롤 비용의 진행도 비례 계수. 상품 가격 인상(ShopOfferPricing)과 같은 축(누적 상점 방문 수)을 쓴다.
        private int _GetRerollStationMultiplier()
        {
            var stageManager = StageManager.Instance;
            int totalInspectionPassedCount = stageManager != null ? stageManager.TotalInspectionPassedCount : 0;

            return totalInspectionPassedCount + 1;
        }

        private void _OnChangeCoin(ChangeCoinUIEvent changeCoinEvent)
        {
            // 코인 변경은 전투 중 처치마다 발행되는 고빈도 이벤트 — 상점이 닫혀 있으면 스킵.
            if (!isShopOpen)
                return;

            // 구독 순서에 의존하지 않도록 이벤트의 AfterCoin을 직접 기준으로 사용한다.
            _ApplyRerollUI(changeCoinEvent.AfterCoin);
        }

        private void _RefreshRerollUI()
        {
            _ApplyRerollUI(UserDataManager.Instance != null ? UserDataManager.Instance.Coin : 0);
        }

        // 현재 리롤 비용을 라벨에 표기한다. 코인이 부족하면 금액을 빨간색으로 표기하고 버튼을 비활성화한다.
        private void _ApplyRerollUI(int coin)
        {
            bool canReroll = coin >= _currentRerollCost;

            if (rerollLabelText != null)
            {
                int costSizePercent = Mathf.RoundToInt(rerollCostFontScale * 100f);
                rerollLabelText.text = $"{_rerollLabelPrefix}\n<size={costSizePercent}%><sprite name=\"Coin\"> {_currentRerollCost.ToCommaString()}$</size>";
                rerollLabelText.color = canReroll ? _rerollLabelOriginalColor : rerollInsufficientColor;
            }

            if (rerollButton != null)
                rerollButton.interactable = canReroll;
        }
    }
}