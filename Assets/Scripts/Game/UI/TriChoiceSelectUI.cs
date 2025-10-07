using Cumic;
using Cumic.Events;
using TMPro;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class TriChoiceSelectUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private TextMeshProUGUI nameText;
        [SerializeField]
        private Image iconImage;
        [SerializeField]
        private TextMeshProUGUI descriptionText;
        #endregion

        private Button _selectButton;
        private ChoiceOption _choiceOption;
        private int _choiceLeftCount;

        private void Awake()
        {
            _selectButton = GetComponent<Button>();
        }

        private void Start()
        {
            _selectButton.onClick.AddListener(OnSelectButtonClick);
        }

        public void SetData(ChoiceOption choiceOption, int choiceLeftCount)
        {
            if (choiceOption == null) return;

            _choiceOption = choiceOption;
            _choiceLeftCount = choiceLeftCount;

            var uiInfo = choiceOption.GetUIInfo();
            iconImage.sprite = uiInfo.Icon;
            nameText.text = uiInfo.Name;
            descriptionText.text = uiInfo.Description;
        }

        private void OnSelectButtonClick()
        {
            _choiceLeftCount--;

            Debug.Log($"_choiceLeftCount: {_choiceLeftCount}");

            TriChoiceSelectEvent eventData = new(_choiceOption, _choiceLeftCount);
            GameEventSystem.Publish(eventData);
        }
    }
}