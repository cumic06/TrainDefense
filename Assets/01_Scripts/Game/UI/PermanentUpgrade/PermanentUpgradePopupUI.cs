using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI.PermanentUpgrade
{
    /// <summary>
    /// 영구(메타) 업그레이드 팝업. 보유 재화를 표시하고 슬롯 목록을 구성한다.
    /// 슬롯을 클릭하면 하단 상세 패널에 설명·비용·구매 버튼을 띄우고 구매를 처리한다.
    /// UIManager.ShowPopup("Popup_PermanentUpgrade")로 띄운다.
    /// </summary>
    public class PermanentUpgradePopupUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI eliteCoinText;
        [SerializeField] private Transform slotContent;            // 슬롯이 배치될 컨테이너 (Layout Group 권장)
        [SerializeField] private PermanentUpgradeSlotUI slotPrefab;

        [Header("상세 패널 (슬롯 클릭 시 표시)")]
        [SerializeField] private GameObject detailPanel;          // 선택 전에는 숨김
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailLevelText;
        [SerializeField] private TextMeshProUGUI detailCostText;
        [SerializeField] private Button purchaseButton;
        #endregion

        private readonly List<PermanentUpgradeSlotUI> _slots = new();
        private PermanentUpgradeData _selectedData;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(_OnClickPurchase);
        }

        private void Start()
        {
            _BuildSlots();
            _RefreshEliteCoin();
            _RefreshDetail();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (purchaseButton != null)
                purchaseButton.onClick.RemoveListener(_OnClickPurchase);
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        private void _BuildSlots()
        {
            if (DatabaseManager.Instance == null || slotPrefab == null || slotContent == null)
                return;

            foreach (var data in DatabaseManager.Instance.GetPermanentUpgradeDatas())
            {
                if (data == null) continue;

                PermanentUpgradeSlotUI slot = Instantiate(slotPrefab, slotContent);
                slot.Init(data, _OnSelectSlot);
                _slots.Add(slot);
            }
        }

        // 슬롯 클릭 → 선택. 하단 상세 패널을 갱신한다.
        private void _OnSelectSlot(PermanentUpgradeData data)
        {
            _selectedData = data;
            _RefreshDetail();
        }

        private void _OnClickPurchase()
        {
            var manager = PermanentUpgradeManager.Instance;
            if (manager == null || _selectedData == null) return;

            if (!manager.TryPurchase(_selectedData.Id)) return;

            // 구매 성공 → 재화·슬롯·상세 갱신 (재화가 줄어 구매 가능 여부도 바뀜)
            _RefreshEliteCoin();
            foreach (var slot in _slots)
                slot.Refresh();
            _RefreshDetail();
        }

        // 선택된 업그레이드의 이름·설명·비용·구매 가능 여부를 하단 패널에 표시한다.
        private void _RefreshDetail()
        {
            var manager = PermanentUpgradeManager.Instance;
            bool hasSelection = _selectedData != null;

            // 선택이 없으면 상세 패널 자체를 숨긴다
            if (detailPanel != null)
                detailPanel.SetActive(hasSelection);

            if (detailNameText != null)
                detailNameText.text = hasSelection ? _selectedData.Name : string.Empty;
            if (detailDescriptionText != null)
                detailDescriptionText.text = hasSelection ? _selectedData.Description : string.Empty;

            if (!hasSelection || manager == null)
            {
                if (detailLevelText != null) detailLevelText.text = string.Empty;
                if (detailCostText != null) detailCostText.text = string.Empty;
                if (purchaseButton != null) purchaseButton.interactable = false;
                return;
            }

            int level = manager.GetLevel(_selectedData.Id);
            bool isMax = manager.IsMaxLevel(_selectedData.Id);
            int cost = _selectedData.GetCostAtLevel(level);

            if (detailLevelText != null)
                detailLevelText.text = _selectedData.MaxUpgradeCount > 0 ? $"Lv {level}/{_selectedData.MaxUpgradeCount}" : $"Lv {level}";
            if (detailCostText != null)
                detailCostText.text = isMax ? "MAX" : cost.ToString();
            if (purchaseButton != null)
                purchaseButton.interactable = !isMax && manager.EliteCoin >= cost;
        }

        private void _RefreshEliteCoin()
        {
            var manager = PermanentUpgradeManager.Instance;
            if (eliteCoinText != null && manager != null)
                eliteCoinText.text = manager.EliteCoin.ToString();
        }
    }
}
