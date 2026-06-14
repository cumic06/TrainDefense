using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI.PermanentUpgrade
{
    /// <summary>
    /// 영구(메타) 업그레이드 팝업 컨트롤러. 보유 재화를 표시하고, DB의 영구 업그레이드 목록을
    /// 슬롯으로 채우며 구매를 처리한다. UIManager.ShowPopup("Popup_PermanentUpgrade")로 띄운다.
    /// </summary>
    public class PermanentUpgradePopupUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI currencyText;
        [SerializeField] private Transform slotContent;            // 슬롯이 배치될 컨테이너 (Layout Group 권장)
        [SerializeField] private PermanentUpgradeSlotUI slotPrefab;
        #endregion

        private readonly List<PermanentUpgradeSlotUI> _slots = new();

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void Start()
        {
            _BuildSlots();
            _RefreshCurrency();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
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
                slot.Init(data, _OnPurchase);
                _slots.Add(slot);
            }
        }

        private void _OnPurchase(PermanentUpgradeData data)
        {
            var manager = PermanentUpgradeManager.Instance;
            if (manager == null || data == null) return;

            if (!manager.TryPurchase(data.Id)) return;

            // 구매 성공 → 재화와 모든 슬롯 갱신 (재화가 줄어 다른 슬롯의 구매 가능 여부도 바뀜)
            _RefreshCurrency();
            foreach (var slot in _slots)
                slot.Refresh();
        }

        private void _RefreshCurrency()
        {
            var manager = PermanentUpgradeManager.Instance;
            if (currencyText != null && manager != null)
                currencyText.text = manager.Currency.ToString();
        }
    }
}
