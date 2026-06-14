using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI.PermanentUpgrade
{
    /// <summary>
    /// 영구 업그레이드 목록의 한 칸. 이름·설명·레벨·비용을 표시하고, 구매 버튼으로 구매를 요청한다.
    /// 구매 처리/갱신은 팝업(PermanentUpgradePopupUI)이 콜백으로 담당한다.
    /// </summary>
    public class PermanentUpgradeSlotUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button purchaseButton;
        #endregion

        private PermanentUpgradeData _data;
        private Action<PermanentUpgradeData> _onPurchase;

        public PermanentUpgradeData Data => _data;

        private void Awake()
        {
            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(_OnClickPurchase);
        }

        private void OnDestroy()
        {
            if (purchaseButton != null)
                purchaseButton.onClick.RemoveListener(_OnClickPurchase);
        }

        public void Init(PermanentUpgradeData data, Action<PermanentUpgradeData> onPurchase)
        {
            _data = data;
            _onPurchase = onPurchase;

            if (iconImage != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
            }
            if (nameText != null) nameText.text = data.Name;
            if (descriptionText != null) descriptionText.text = data.Description;

            Refresh();
        }

        /// <summary>현재 레벨·비용·구매 가능 여부를 갱신한다.</summary>
        public void Refresh()
        {
            var manager = PermanentUpgradeManager.Instance;
            if (manager == null || _data == null) return;

            int level = manager.GetLevel(_data.Id);
            bool isMax = manager.IsMaxLevel(_data.Id);

            if (levelText != null)
                levelText.text = _data.MaxUpgradeCount > 0 ? $"Lv {level}/{_data.MaxUpgradeCount}" : $"Lv {level}";

            int cost = _data.GetCostAtLevel(level);
            if (costText != null)
                costText.text = isMax ? "MAX" : cost.ToString();

            if (purchaseButton != null)
                purchaseButton.interactable = !isMax && manager.Currency >= cost;
        }

        private void _OnClickPurchase()
        {
            _onPurchase?.Invoke(_data);
        }
    }
}
