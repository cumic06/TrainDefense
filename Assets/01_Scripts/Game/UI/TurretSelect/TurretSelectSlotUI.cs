using System;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 포탑 선택창 하단 바의 개별 포탑 슬롯. 아이콘 표시 + 선택 하이라이트 토글 + 클릭 콜백만 담당한다.
    /// </summary>
    public class TurretSelectSlotUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Image iconImage;
        [SerializeField]
        private Button selectButton;
        [SerializeField]
        private GameObject selectedHighlight;
        #endregion

        private Action<TurretSelectSlotUI> _onClick;

        public string TurretId { get; private set; }

        public void Setup(string turretId, Sprite icon, Action<TurretSelectSlotUI> onClick)
        {
            TurretId = turretId;
            _onClick = onClick;

            if (iconImage != null)
                iconImage.sprite = icon;

            SetSelected(false);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(_HandleClick);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedHighlight != null)
                selectedHighlight.SetActive(isSelected);
        }

        private void _HandleClick()
        {
            _onClick?.Invoke(this);
        }
    }
}
