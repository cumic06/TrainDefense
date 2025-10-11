using Cumic.Events;
using DG.Tweening;
using TMPro;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class TriChoiceSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

            _isSelected = false;
        }

        private void OnSelectButtonClick()
        {
            _choiceLeftCount--;

            Debug.Log($"_choiceLeftCount: {_choiceLeftCount}");

            TriChoiceSelectEvent eventData = new(_choiceOption, _choiceLeftCount);
            GameEventSystem.Publish(eventData);
            _isSelected = true;
        }

        private bool _isSelected = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isSelected) return;
            transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isSelected) return;
            transform.DOScale(1f, 0.1f).SetEase(Ease.InBack).SetUpdate(true);
        }
    }
}