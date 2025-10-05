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

        private void Awake()
        {
            _selectButton = GetComponent<Button>();
        }

        private void Start()
        {
            _selectButton.onClick.AddListener(OnSelectButtonClick);
        }

        public void SetData(ChoiceOption choiceOption)
        {
            if (choiceOption == null) return;

            _choiceOption = choiceOption;

            var uiInfo = choiceOption.GetUIInfo();
            iconImage.sprite = uiInfo.Icon;
            nameText.text = uiInfo.Name;
            descriptionText.text = uiInfo.Description;
        }

        private void OnSelectButtonClick()
        {
            GameEventSystem.Publish(new TriChoiceSelectEvent(_choiceOption));
            GameEventSystem.Publish(new EngageStartEvent());
        }
    }
}