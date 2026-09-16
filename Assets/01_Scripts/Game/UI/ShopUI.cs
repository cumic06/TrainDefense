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
using TrainDefense.Localize;

namespace TrainDefense.Game.UI
{
    public class ShopUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        [Tooltip("상점 패널이 내려오기 시작하는 y. 참조 해상도가 1080이라 1200이면 화면 위 완전히 밖")]
        private float shopMoveStartPosY = 1200f;
        [SerializeField]
        [Tooltip("상점 패널이 멈추는 y(제자리)")]
        private float shopMoveEndPosY;
        [SerializeField]
        private float shopMoveInterval = 1f;
        [SerializeField]
        [Tooltip("상점 패널이 멈출 때 살짝 넘쳤다 돌아오는 정도. 1.1은 2200px 이동에서 61px(11ms)이라 눈에 안 보였다")]
        private float panelOvershoot = 4f;

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
        [SerializeField]
        [Tooltip("상점 배경과 코인 표시가 서서히 나타나고 사라지는 시간(초)")]
        private float uiFadeDuration = 0.25f;

        [Header("Offer Slots")]
        [SerializeField]
        [Tooltip("역 상점에 제시되는 판매 슬롯 수. 구매한 상품은 슬롯에서 사라지고, 리롤로만 전체 재추첨된다")]
        private int offerSlotCount = 5;
        [SerializeField]
        private ShopOfferSlotUI offerSlotPrefab;
        [SerializeField]
        private ShopOfferPricing offerPricing = new();

        [Header("Turret Slot")]
        [SerializeField]
        [Tooltip("헤더에 포탑 칸 수(보유/최대)를 표시하는 텍스트. 자리가 꽉 차면 회색 카드의 이유를 설명한다")]
        private TextMeshProUGUI turretSlotText;
        [SerializeField]
        [Tooltip("포탑 칸이 꽉 찼을 때 칸 수 텍스트에 적용할 색상")]
        private Color turretSlotFullColor = Color.red;

        [Header("Reroll")]
        [SerializeField]
        [Tooltip("이번 역 상점의 슬롯 전체를 다시 추첨하는 버튼")]
        private Button rerollButton;
        [SerializeField]
        private TextMeshProUGUI rerollLabelText;
        [SerializeField]
        [Tooltip("첫 리롤 비용 = 이 값 × (누적 상점 방문 수 + 1). 수입이 커지는 후반에 리롤이 껌값이 되지 않게 진행도 비례")]
        private int rerollBaseCostPerStation = 10;
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
        private CanvasGroup _coinGroup;
        private CanvasGroup _backgroundGroup;
        private CanvasGroup _panelGroup;
        private ShopTrainPreviewUI _trainPreview;
        private GridLayoutGroup _itemGrid;
        private RectTransform _itemGridRect;
        private RectTransform[] _itemRects;
        private Vector2[] _itemFinalPositions;

        private readonly List<ShopOfferSlotUI> _offerSlotUIs = new();

        private int _currentRerollCost;
        // 스킬 트리·영구 강화의 무료 새로고침 횟수. 상점이 열릴 때마다 채워지고, 남아 있는 동안은 코인 차감·비용 인상 없이 리롤한다.
        private int _freeRerollsLeft;
        private string _rerollLabelPrefix;
        private Color _rerollLabelOriginalColor;
        private Color _turretSlotOriginalColor;

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

            if (turretSlotText != null)
                _turretSlotOriginalColor = turretSlotText.color;

            // 코드가 채우는 문구라 LocalizeText처럼 자동 갱신되지 않는다 — 언어가 바뀌면 직접 다시 그린다.
            Localization.OnLanguageChanged += _RefreshTurretSlotText;
        }

        private void OnDestroy()
        {
            shopButtonUI.OnClickShopButton -= _ShopOpenHandler;
            GameEventSystem.Unsubscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(_OnChangeCoin);
            Localization.OnLanguageChanged -= _RefreshTurretSlotText;
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
            _freeRerollsLeft = _GetFreeRerollCount();
            _RebuildOfferSlots();
            _RefreshRerollUI();
            _RefreshTurretSlotText();

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
                // 시작 버튼을 여기서 끄면 _CloseShop이 async void라 첫 await에 바로 돌아와,
                // 패널이 미끄러지기 시작하는 프레임에 버튼만 사라진다. 끄는 것은 _CloseShop이 한다.
                _CloseShop();
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

            _SetCoinGroupVisible(true);

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            // 슬라이드인 동안 버튼이 제자리에 보이지 않도록 미리 숨겨둠
            _PrepareItemsHidden();
            _PrepareButtonsHidden();

            // 1) 테이블·선로·기차를 먼저 깔고, 그 위로 상점 패널(카드)이 내려온다
            _SetBackgroundVisible(true, uiFadeDuration);

            // 기차는 배경 자식이라 배경이 꺼져 있는 동안 비활성이고, 그 상태에서는 OnEnable이 안 돌아
            // InspectionStartEvent를 놓친다. 배경을 켠 지금 직접 표시를 요청한다.
            _GetTrainPreview()?.ShowNow();

            // 닫을 때는 제자리에서 옅어지기만 하므로, 내려올 위치는 열 때 직접 세운다.
            _SetPanelVisible(true, 0f);
            _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, shopMoveStartPosY);

            // 끝에서 살짝 넘쳤다 돌아오게 해서 '슉' 미끄러지는 느낌을 없앤다.
            // 패널이 커서 기본 overshoot(1.7)은 과하므로 낮춰 쓴다.
            await _rectTransform.DOAnchorPosY(shopMoveEndPosY, shopMoveInterval)
                .SetEase(Ease.OutBack, panelOvershoot)
                .SetUpdate(true);

            // 2) 패널이 자리잡은 뒤 버튼들을 위에서 순차적으로 떨어뜨림
            _PlayItemsDropIn();
            _PlayButtonsPopIn();

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

        /// <summary>
        /// 시작·다시 뽑기 버튼은 패널 자식이라 슬라이드인에 그대로 딸려 내려온다.
        /// 카드처럼 미리 숨겨뒀다가 패널이 자리잡은 뒤 등장시킨다.
        /// 자리는 그대로 두고 크기만 쓴다(버튼 위치는 따로 정한 값이라 흔들지 않는다).
        /// </summary>
        private void _PrepareButtonsHidden()
        {
            foreach (var button in _GetPanelButtons())
            {
                button.DOKill();
                button.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// 카드가 다 떨어진 뒤 버튼을 제자리에서 튀어나오게 한다.
        /// </summary>
        private void _PlayButtonsPopIn()
        {
            float delay = _itemRects != null ? _itemRects.Length * itemDropStagger : 0f;

            foreach (var button in _GetPanelButtons())
            {
                button.DOScale(Vector3.one, itemDropDuration)
                    .SetDelay(delay)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
        }

        private IEnumerable<Transform> _GetPanelButtons()
        {
            if (shopButtonUI != null)
                yield return shopButtonUI.transform;

            if (rerollButton != null)
                yield return rerollButton.transform;
        }

        /// <summary>
        /// 상점 패널(카드·버튼)을 통째로 옅게 하거나 되돌린다. duration이 0이면 즉시 적용한다.
        /// </summary>
        private void _SetPanelVisible(bool visible, float duration)
        {
            if (_panelGroup == null)
            {
                _panelGroup = GetComponent<CanvasGroup>();
                if (_panelGroup == null)
                {
                    _panelGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _panelGroup.DOKill();

            // 나갈 때 제자리에서 옅어지기만 하므로 패널은 안 보일 때도 화면 위에 그대로 있다.
            // 알파만 내리면 보이지 않는 버튼이 클릭을 먹으므로 입력도 같이 끊는다.
            _panelGroup.interactable = visible;
            _panelGroup.blocksRaycasts = visible;

            if (duration <= 0f)
            {
                _panelGroup.alpha = visible ? 1f : 0f;
                return;
            }

            _panelGroup.DOFade(visible ? 1f : 0f, duration).SetUpdate(true);
        }

        /// <summary>
        /// 상점 배경을 서서히 띄우거나 지운다.
        /// ★ Image.DOFade가 아니라 CanvasGroup을 쓰는 이유: 배경 아래 선로·기차가 자식이라
        ///   Image 색만 바꾸면 배경만 옅어지고 선로가 어두운 화면에 그대로 남는다.
        /// </summary>
        private void _SetBackgroundVisible(bool visible, float duration)
        {
            if (backgroundImage == null)
            {
                return;
            }

            if (_backgroundGroup == null)
            {
                _backgroundGroup = backgroundImage.GetComponent<CanvasGroup>();
                if (_backgroundGroup == null)
                {
                    _backgroundGroup = backgroundImage.gameObject.AddComponent<CanvasGroup>();
                }
            }

            _backgroundGroup.DOKill();

            if (visible)
            {
                backgroundImage.gameObject.SetActive(true);
                _backgroundGroup.alpha = 0f;
                _backgroundGroup.DOFade(1f, duration).SetUpdate(true);
            }
            else
            {
                _backgroundGroup.DOFade(0f, duration).SetUpdate(true)
                    .OnComplete(() => backgroundImage.gameObject.SetActive(false));
            }
        }

        /// <summary>
        /// 배경(선로) 아래에 있는 기차 프리뷰를 찾아 캐시한다. 없으면 null.
        /// </summary>
        private ShopTrainPreviewUI _GetTrainPreview()
        {
            if (_trainPreview == null && backgroundImage != null)
            {
                _trainPreview = backgroundImage.GetComponentInChildren<ShopTrainPreviewUI>(true);
            }

            return _trainPreview;
        }

        /// <summary>
        /// 코인 표시를 서서히 띄우거나 지운다. CanvasGroup이 없으면 붙여서 쓴다.
        /// </summary>
        private void _SetCoinGroupVisible(bool visible)
        {
            if (groupCoin == null)
            {
                return;
            }

            if (_coinGroup == null)
            {
                _coinGroup = groupCoin.GetComponent<CanvasGroup>();
                if (_coinGroup == null)
                {
                    _coinGroup = groupCoin.AddComponent<CanvasGroup>();
                }
            }

            _coinGroup.DOKill();

            if (visible)
            {
                groupCoin.SetActive(true);
                _coinGroup.alpha = 0f;
                _coinGroup.DOFade(1f, uiFadeDuration).SetUpdate(true);
            }
            else
            {
                _coinGroup.DOFade(0f, uiFadeDuration).SetUpdate(true)
                    .OnComplete(() => groupCoin.SetActive(false));
            }
        }

        private async void _CloseShop()
        {
            if (!isShopOpen)
            {
                return;
            }

            isShopOpen = false;

            // 나갈 때는 움직이지 않고 패널·배경이 제자리에서 함께 옅어진다.
            // (버튼은 패널 자식이라 같이 사라지므로, 끄는 것은 다 옅어진 뒤에 한다)
            _SetPanelVisible(false, uiFadeDuration);
            _SetBackgroundVisible(false, uiFadeDuration);
            await UniTask.Delay(Mathf.RoundToInt(uiFadeDuration * 1000f), DelayType.UnscaledDeltaTime);

            shopButtonUI.gameObject.SetActive(false);
            _SetCoinGroupVisible(false);

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

            _RefreshTurretSlotText();
        }

        // 헤더의 포탑 칸 수 표시. 꽉 차면 색이 바뀌어, 포탑 카드가 회색인 이유를 설명한다.
        private void _RefreshTurretSlotText()
        {
            if (turretSlotText == null || TrainManager.Instance == null)
                return;

            int currentTrainCount = TrainManager.Instance.GetTrainCount();
            int maxTrainCount = TrainManager.Instance.GetMaxTrainCount();

            // 라벨은 시트에 키가 생기면 자동으로 번역된다(없으면 fallback).
            string label = LocalizeHelper.GetByKey("UI_Shop_TurretSlot", "포탑");

            turretSlotText.text = $"{label} {currentTrainCount}/{maxTrainCount}";
            turretSlotText.color = currentTrainCount >= maxTrainCount
                ? turretSlotFullColor
                : _turretSlotOriginalColor;
        }

        // 리롤: 코인을 차감하고 슬롯 전체를 다시 추첨한다. 비용은 리롤마다 역 수 비례 증가분만큼 오른다.
        private void _OnRerollButtonClick()
        {
            if (_freeRerollsLeft > 0)
            {
                // 무료 새로고침은 코인을 쓰지 않고 유료 비용도 올리지 않는다.
                _freeRerollsLeft--;
            }
            else
            {
                // 코인 부족 시엔 버튼 interactable이 이미 꺼져 있지만, 방어적으로 다시 확인한다.
                // 차감·검증 = UserDataManager 단일 경로 (상점 슬롯 구매와 동일).
                if (UserDataManager.Instance == null || !UserDataManager.Instance.TrySpendCoin(_currentRerollCost))
                    return;

                _currentRerollCost += rerollCostIncreasePerStation * _GetRerollStationMultiplier();
            }

            _RebuildOfferSlots();
            _RefreshRerollUI();
        }

        // 스킬 트리 노드와 영구 강화의 무료 새로고침 횟수 합. 둘 다 "레벨당 +1회"라 정수로 쓴다.
        private int _GetFreeRerollCount()
        {
            int count = 0;

            if (SkillTreeManager.Instance != null)
                count += Mathf.RoundToInt(SkillTreeManager.Instance.GetValue(SkillTreePassiveType.FreeReroll));

            if (PermanentUpgradeManager.Instance != null)
                count += Mathf.RoundToInt(PermanentUpgradeManager.Instance.GetValue(PermanentUpgradeType.FreeReroll));

            return count;
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
            bool hasFreeReroll = _freeRerollsLeft > 0;
            bool canReroll = hasFreeReroll || coin >= _currentRerollCost;

            if (rerollLabelText != null)
            {
                int costSizePercent = Mathf.RoundToInt(rerollCostFontScale * 100f);

                // 라벨은 매번 조회한다. 언어를 바꾼 뒤 상점을 다시 열어도 갱신되도록.
                // 키가 없거나 초기화 전이면 프리팹 원문(_rerollLabelPrefix)으로 떨어진다.
                string rerollLabel = LocalizeHelper.GetByKey("UI_Reroll", _rerollLabelPrefix);

                // 무료 횟수가 남아 있으면 비용 대신 "무료 ×N"을 보여준다.
                string costLine = hasFreeReroll
                    ? $"{LocalizeHelper.GetByKey("UI_Reroll_Free", "Free")} ×{_freeRerollsLeft}"
                    : $"<sprite name=\"Coin\"> {_currentRerollCost.ToCommaString()}$";

                rerollLabelText.text = $"{rerollLabel}\n<size={costSizePercent}%>{costLine}</size>";
                rerollLabelText.color = canReroll ? _rerollLabelOriginalColor : rerollInsufficientColor;
            }

            if (rerollButton != null)
                rerollButton.interactable = canReroll;
        }
    }
}