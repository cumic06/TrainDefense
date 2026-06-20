using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Achievement
{
    /// <summary>
    /// 업적 상세 패널. 선택된 업적의 제목·설명·진행도를 보여준다.
    /// 선택이 없으면 안내 문구(emptyHint)만 표시한다. (도감 CollectionDetailUI 패턴)
    /// </summary>
    public class AchievementDetailUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject unlockedMark;
        [SerializeField] private GameObject emptyHint;
        #endregion

        public void Show(AchievementEntry entry)
        {
            bool hasEntry = entry != null;

            if (emptyHint != null)
                emptyHint.SetActive(!hasEntry);

            if (!hasEntry)
            {
                if (titleText != null) titleText.text = string.Empty;
                if (descriptionText != null) descriptionText.text = string.Empty;
                if (progressText != null) progressText.text = string.Empty;
                if (progressFill != null) progressFill.fillAmount = 0f;
                if (unlockedMark != null) unlockedMark.SetActive(false);

                return;
            }

            if (titleText != null)
                titleText.text = entry.Title;

            if (descriptionText != null)
                descriptionText.text = entry.Description;

            if (progressText != null)
                progressText.text = entry.IsUnlocked
                    ? LocalizeHelper.GetByKey("Achievement_Unlocked", "달성")
                    : entry.ProgressText;

            if (progressFill != null)
                progressFill.fillAmount = entry.Progress;

            if (unlockedMark != null)
                unlockedMark.SetActive(entry.IsUnlocked);
        }
    }
}
