using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Achievement
{
    /// <summary>
    /// 업적 그리드의 한 칸. 제목·진행도 바·달성 배지를 표시하고, 클릭하면 자신을 콜백으로 넘겨
    /// 팝업이 상세 패널 갱신과 선택 하이라이트를 처리하게 한다. (도감 CollectionSlotUI 패턴)
    /// </summary>
    public class AchievementSlotUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Button slotButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject unlockedBadge;
        [SerializeField] private GameObject selectedFrame;
        #endregion

        private AchievementEntry _entry;
        private Action<AchievementSlotUI> _onSelect;

        public AchievementEntry Entry => _entry;

        private void Awake()
        {
            if (slotButton != null)
                slotButton.onClick.AddListener(_OnClick);
        }

        private void OnDestroy()
        {
            if (slotButton != null)
                slotButton.onClick.RemoveListener(_OnClick);
        }

        public void Init(AchievementEntry entry, Action<AchievementSlotUI> onSelect)
        {
            _entry = entry;
            _onSelect = onSelect;

            if (titleText != null)
                titleText.text = entry.Title;

            if (progressText != null)
                progressText.text = entry.IsUnlocked
                    ? LocalizeHelper.GetByKey("Achievement_Unlocked", "달성")
                    : entry.ProgressText;

            if (progressFill != null)
                progressFill.fillAmount = entry.Progress;

            if (unlockedBadge != null)
                unlockedBadge.SetActive(entry.IsUnlocked);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedFrame != null)
                selectedFrame.SetActive(selected);
        }

        private void _OnClick()
        {
            _onSelect?.Invoke(this);
        }
    }
}
