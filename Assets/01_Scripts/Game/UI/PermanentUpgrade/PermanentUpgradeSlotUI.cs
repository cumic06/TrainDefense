using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI.PermanentUpgrade
{
    /// <summary>
    /// 영구 업그레이드 목록의 한 칸. 아이콘·이름·레벨만 표시하고, 클릭 시 선택을 알린다.
    /// 설명·비용·구매 버튼은 팝업(PermanentUpgradePopupUI) 하단 상세 패널이 담당한다.
    /// </summary>
    public class PermanentUpgradeSlotUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject redDot;               // 현재 재화로 구매 가능하면 켜지는 뱃지
        #endregion

        private PermanentUpgradeData _data;
        private Action<PermanentUpgradeData> _onSelect;

        public PermanentUpgradeData Data => _data;

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(_OnClickSelect);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(_OnClickSelect);
        }

        public void Init(PermanentUpgradeData data, Action<PermanentUpgradeData> onSelect)
        {
            _data = data;
            _onSelect = onSelect;

            if (iconImage != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
            }
            if (nameText != null) nameText.text = data.Name;

            Refresh();
        }

        /// <summary>현재 레벨과 구매 가능 레드닷을 갱신한다.</summary>
        public void Refresh()
        {
            var manager = PermanentUpgradeManager.Instance;
            if (manager == null || _data == null) return;

            int level = manager.GetLevel(_data.Id);
            if (levelText != null)
                levelText.text = _data.MaxUpgradeCount > 0 ? $"Lv {level}/{_data.MaxUpgradeCount}" : $"Lv {level}";

            if (redDot != null)
                redDot.SetActive(manager.IsAffordable(_data.Id));
        }

        private void _OnClickSelect()
        {
            _onSelect?.Invoke(_data);
        }
    }
}
