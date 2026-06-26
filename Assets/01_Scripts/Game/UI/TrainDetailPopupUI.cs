using System.Text;
using TMPro;
using TrainDefense.Localize;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class TrainDetailPopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI trainNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI skillsText;

        private Train _train;

        private void OnEnable()
        {
            Localization.OnInitialized += _RefreshLocalizedTexts;
            Localization.OnLanguageChanged += _RefreshLocalizedTexts;
        }

        private void OnDisable()
        {
            Localization.OnInitialized -= _RefreshLocalizedTexts;
            Localization.OnLanguageChanged -= _RefreshLocalizedTexts;
        }

        // 떠 있는 동안 초기화 완료·언어 변경 시, 마지막으로 표시한 train으로 다시 그린다.
        private void _RefreshLocalizedTexts()
        {
            if (_train != null)
                Show(_train);
        }

        public void Show(Train train)
        {
            if (train == null) return;
            _train = train;

            var data = train.TrainData;
            if (data != null)
            {
                if (trainNameText != null)
                    trainNameText.text = data.Name;

                if (descriptionText != null)
                    descriptionText.text = data.Description;
            }

            if (statsText != null)
            {
                var sb = new StringBuilder();
                var stats = train.GetStatDetails();
                foreach (var (label, value) in stats)
                    sb.AppendLine($"{label}: {value}");
                statsText.text = sb.ToString().TrimEnd();
            }

            if (skillsText != null)
            {
                var sb = new StringBuilder();

                // TrainData 전체 스킬이 아니라, 이 인스턴스에 실제 적용된 스킬만 표시(마스크 반영).
                string activeLabel = LocalizeHelper.GetByKey("skill_type_active", "액티브");
                string passiveLabel = LocalizeHelper.GetByKey("skill_type_passive", "패시브");

                foreach (var (isActive, name, description) in train.GetAppliedSkillDisplays())
                {
                    sb.AppendLine(isActive ? $"[{activeLabel}] {name}" : $"[{passiveLabel}] {name}");
                    if (!string.IsNullOrEmpty(description))
                        sb.AppendLine(description);
                }

                skillsText.text = sb.ToString().TrimEnd();
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
