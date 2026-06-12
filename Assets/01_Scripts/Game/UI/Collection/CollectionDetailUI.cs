using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Collection
{
    /// <summary>
    /// 도감 오른쪽 상세 패널. 선택된 유닛의 아이콘/이름/설명/스탯을 보여준다.
    /// 미발견 유닛은 검은 실루엣 + "???" + 안내 문구로 표시한다.
    /// </summary>
    public class CollectionDetailUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private GameObject emptyHint;
        [SerializeField] private Color lockedColor = Color.black;
        [SerializeField] private string undiscoveredDescription = "아직 발견하지 못한 유닛입니다.";
        #endregion

        private readonly StringBuilder _statBuilder = new();

        public void Show(CollectionEntry entry)
        {
            if (entry == null)
            {
                _ShowEmpty();

                return;
            }

            if (emptyHint != null)
                emptyHint.SetActive(false);

            bool discovered = entry.IsDiscovered;

            if (iconImage != null)
            {
                iconImage.sprite = entry.Icon;
                iconImage.enabled = entry.Icon != null;
                iconImage.color = discovered ? Color.white : lockedColor;
            }

            if (nameText != null)
                nameText.text = discovered ? entry.Name : "???";

            if (descriptionText != null)
                descriptionText.text = discovered ? entry.Description : LocalizeHelper.GetByKey("Collection_Undiscovered", undiscoveredDescription);

            if (statsText != null)
                statsText.text = discovered ? _BuildStatsText(entry) : string.Empty;
        }

        private void _ShowEmpty()
        {
            if (iconImage != null)
                iconImage.enabled = false;

            if (nameText != null)
                nameText.text = string.Empty;

            if (descriptionText != null)
                descriptionText.text = string.Empty;

            if (statsText != null)
                statsText.text = string.Empty;

            if (emptyHint != null)
                emptyHint.SetActive(true);
        }

        private string _BuildStatsText(CollectionEntry entry)
        {
            _statBuilder.Clear();

            foreach (CollectionStatLine line in entry.StatLines)
            {
                _statBuilder.AppendLine($"{line.Label} : {line.Value}");
            }

            return _statBuilder.ToString();
        }
    }
}
