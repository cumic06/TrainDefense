using System.Text;
using TMPro;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class TrainDetailPopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI trainNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI skillsText;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Show(Train train)
        {
            if (train == null) return;

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

                int level = train.CurrentLevel + 1;
                sb.AppendLine($"Lv.{level}");

                if (data != null)
                {
                    var activeSkills = data.TrainSkillDatas;
                    if (activeSkills != null)
                    {
                        foreach (var skill in activeSkills)
                        {
                            sb.AppendLine($"[액티브] {skill.Name}");
                            if (!string.IsNullOrEmpty(skill.Description))
                                sb.AppendLine(skill.Description);
                        }
                    }

                    var passiveSkills = data.PassiveSkillDatas;
                    if (passiveSkills != null)
                    {
                        foreach (var skill in passiveSkills)
                        {
                            sb.AppendLine($"[패시브] {skill.Name}");
                            if (!string.IsNullOrEmpty(skill.Description))
                                sb.AppendLine(skill.Description);
                        }
                    }
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
