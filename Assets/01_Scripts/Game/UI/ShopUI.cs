using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
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
        #endregion

        private RectTransform _rectTransform;
        private GridLayoutGroup _itemGrid;
        private RectTransform _itemGridRect;
        private RectTransform[] _itemRects;
        private Vector2[] _itemFinalPositions;

        private List<ShopItemUI> _shopItemUIs = new();

        private bool isShopOpen = false;
        public bool IsShopOpen => isShopOpen;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _shopItemUIs = GetComponentsInChildren<ShopItemUI>().ToList();
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
        }

        private void OnDestroy()
        {
            shopButtonUI.OnClickShopButton -= _ShopOpenHandler;
            GameEventSystem.Unsubscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
        }

        private void OnUpgradeApplied(UpgradeAppliedEvent upgradeAppliedEvent)
        {
            foreach (var shopItemUI in _shopItemUIs)
            {
                shopItemUI.SetUp();
            }
        }

        private void _OnInspectionStart(InspectionStartEvent inspectionStartEvent)
        {
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

            foreach (var shopItemUI in _shopItemUIs)
            {
                shopItemUI.SetUp();
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

            // 업그레이드 버튼뿐 아니라 기차 수리 버튼(ShopRepairItemUI)까지 드롭인 연출에 포함되도록
            // Grid의 모든 자식을 대상으로 위치를 캐싱한다.
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

        private int GetCurrentMoney()
        {
            if (UserDataManager.Instance == null) return 0;

            return UserDataManager.Instance.Coin;
        }
    }
}