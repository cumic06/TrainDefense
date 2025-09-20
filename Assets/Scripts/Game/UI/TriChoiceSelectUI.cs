using Cumic;
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
        private TriChoiceData _data;

        private void Awake()
        {
            _selectButton = GetComponent<Button>();
        }

        private void Start()
        {
            _selectButton.onClick.AddListener(OnSelectButtonClick);
        }

        public void SetData(TriChoiceData data)
        {
            if (data == null) return;

            _data = data;

            iconImage.sprite = data.Icon;
            nameText.text = data.ChoiceName;
            descriptionText.text = data.Description;
        }

        private void OnSelectButtonClick()
        {
            GameEventSystem.Publish(new TriChoiceSelectEvent(_data));
        }
    }
}