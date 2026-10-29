using Cumic;
using Cumic.Events;
using TMPro;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    // 상점 수리 — 역 도착 회복이 없는 난이도에서 코인을 내고 포탑을 회복·부활시킨다.
    // 한 번 누를 때마다 모든 포탑이 최대 체력의 일정 비율(repairStepRatio)만큼 회복되고, 부서진 포탑은 그 비율로 되살아난다.
    // 더 고치려면 다시 누른다. 가격은 상점 가격표(ShopOfferPricing)가 정한다. 버튼은 회복 없는 난이도의 역 상점에서만 보인다.
    // 버튼을 숨기므로 이 컴포넌트는 버튼이 아니라 늘 켜져 있는 상점 패널(ShopUI와 같은 오브젝트)에 붙인다(꺼진 오브젝트는 Start가 돌지 않는다).
    public class ShopRepairButtonUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Button repairButton;
        [SerializeField]
        private TextMeshProUGUI repairLabelText;
        [SerializeField]
        [Tooltip("한 번 누를 때 회복하는 양(최대 체력 대비 비율)")]
        [Range(0.05f, 1f)]
        private float repairStepRatio = 0.25f;
        [SerializeField]
        [Tooltip("보유 코인이 부족할 때 수리 비용 텍스트에 적용할 색상")]
        private Color repairInsufficientColor = Color.red;
        [SerializeField]
        [Tooltip("고칠 체력이 없어 버튼이 꺼졌을 때 라벨 전체에 적용할 색상 (DESIGN.md on-surface-muted)")]
        private Color repairNoDamageColor = new Color32(0x99, 0x9A, 0xA6, 0xFF);
        [SerializeField]
        [Tooltip("수리 비용 표기 크기(라벨 대비 배율)")]
        private float repairCostFontScale = 0.5f;
        #endregion

        private Color _repairLabelOriginalColor;
        private ShopUI _shopUI;

        private void Start()
        {
            if (repairButton != null)
            {
                repairButton.onClick.AddListener(_OnRepairButtonClick);

                if (repairLabelText == null)
                    repairLabelText = repairButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (repairLabelText != null)
                _repairLabelOriginalColor = repairLabelText.color;

            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<InspectionEndEvent>(_OnInspectionEnd);
            GameEventSystem.Subscribe<ChangeCoinUIEvent>(_OnChangeCoin);
            GameEventSystem.Subscribe<ShopStateChangedEvent>(_OnShopStateChanged);
            _SetButtonVisible(false);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<InspectionEndEvent>(_OnInspectionEnd);
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(_OnChangeCoin);
            GameEventSystem.Unsubscribe<ShopStateChangedEvent>(_OnShopStateChanged);
        }

        // 수리는 역 상점에서만 한다. 회복이 있는 난이도에선 고칠 게 없어 아예 띄우지 않는다.
        private void _OnInspectionStart(InspectionStartEvent _)
        {
            _SetButtonVisible(!StageManager.HasStationArrivalRecovery);
            _RefreshIfVisible();
        }

        // 전투 중엔 숨긴다 — 싸우는 도중 부활을 사지 못하게.
        private void _OnInspectionEnd(InspectionEndEvent _) => _SetButtonVisible(false);

        // 상점 구매(포탑 추가·강화)로 잃은 체력이 바뀔 수 있다.
        private void _OnShopStateChanged(ShopStateChangedEvent _) => _RefreshIfVisible();

        // 코인 변경은 전투 중 처치마다 발행되는 고빈도 이벤트 — 버튼이 숨어 있으면 스킵.
        private void _OnChangeCoin(ChangeCoinUIEvent changeCoinEvent)
        {
            if (!_IsButtonVisible())
                return;

            _ApplyRepairUI(changeCoinEvent.AfterCoin);
        }

        private void _SetButtonVisible(bool visible)
        {
            if (repairButton != null)
                repairButton.gameObject.SetActive(visible);
        }

        private bool _IsButtonVisible() => repairButton != null && repairButton.gameObject.activeSelf;

        private static MainTrain _GetMainTrain() => TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

        // 한 번 누를 때의 가격 = 포탑마다 이번에 채울 체력(잃은 체력과 한 번 회복량 중 작은 쪽)의 가격 합.
        private int _GetRepairCost()
        {
            var mainTrain = _GetMainTrain();
            int repairCost = 0;

            if (mainTrain == null || (_shopUI == null && !TryGetComponent(out _shopUI)))
                return 0;

            foreach (var train in mainTrain.CurrentTrains)
            {
                if (train != null)
                    repairCost += _shopUI.GetRepairPrice(Mathf.Min(train.MissingHp, train.MaxHp * repairStepRatio));
            }

            return repairCost;
        }

        private void _RefreshIfVisible()
        {
            if (!_IsButtonVisible())
                return;

            _ApplyRepairUI(UserDataManager.Instance != null ? UserDataManager.Instance.Coin : 0);
        }

        // 수리 비용을 라벨에 표기한다. 고칠 게 없거나 코인이 부족하면 버튼을 끄고, 부족하면 금액을 빨간색으로 표기한다.
        private void _ApplyRepairUI(int coin)
        {
            int repairCost = _GetRepairCost();
            bool hasDamage = repairCost > 0;
            bool canAfford = coin >= repairCost;

            if (repairLabelText != null)
            {
                int costSizePercent = Mathf.RoundToInt(repairCostFontScale * 100f);
                int stepPercent = Mathf.RoundToInt(repairStepRatio * 100f);

                // 코인 부족은 상점 카드처럼 금액만 빨갛게 — 효과(+25%)까지 물들이지 않는다.
                string costText = $"{repairCost.ToCommaString()}$";

                if (hasDamage && !canAfford)
                    costText = $"<color=#{ColorUtility.ToHtmlStringRGBA(repairInsufficientColor)}>{costText}</color>";

                // 글자 없이 숫자만 쓴다 — 수리 뜻은 렌치 아이콘이 전한다. 언어마다 폰트가 섞이지 않는다.
                repairLabelText.text = $"+{stepPercent}%\n<size={costSizePercent}%><sprite name=\"Coin\"> {costText}</size>";
                // 버튼 배경만 비활성 틴트가 걸리므로 고칠 게 없어 꺼진 것은 글자 전체를 회색으로 보인다.
                repairLabelText.color = hasDamage ? _repairLabelOriginalColor : repairNoDamageColor;
            }

            if (repairButton != null)
                repairButton.interactable = hasDamage && canAfford;
        }

        private void _OnRepairButtonClick()
        {
            var mainTrain = _GetMainTrain();
            int repairCost = _GetRepairCost();

            if (mainTrain == null || repairCost <= 0)
                return;

            // 차감·검증 = UserDataManager 단일 경로 (상점 슬롯 구매·리롤과 동일).
            if (UserDataManager.Instance == null || !UserDataManager.Instance.TrySpendCoin(repairCost))
                return;

            mainTrain.RepairTrainsByRatio(repairStepRatio);
            // 상점 상태 변경 — 런 세이브가 수리된 체력을 저장하고, 기차 프리뷰·이 버튼이 다시 그린다.
            GameEventSystem.Publish(new ShopStateChangedEvent());
            AnalyticsManager.LogCustomEvent("shop_repair", ("cost", repairCost));
        }
    }
}
