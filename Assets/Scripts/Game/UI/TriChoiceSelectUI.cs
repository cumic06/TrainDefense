using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TrainDefense.Game.Datas;

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
        private IChoiceOption _choiceOption;
        private TriChoiceUI _triChoiceUI;

        private bool _isSelected = false;

        private void Awake()
        {
            _selectButton = GetComponent<Button>();
        }

        private void Start()
        {
            _selectButton.onClick.AddListener(OnSelectButtonClick);
        }

        public void SetData(IChoiceOption choiceOption, TriChoiceUI triChoiceUI)
        {
            if (choiceOption == null) return;

            _choiceOption = choiceOption;
            _triChoiceUI = triChoiceUI;
            SetUI(choiceOption);

            _isSelected = false;
        }

        private void SetUI(IChoiceOption choiceOption)
        {
            var uiInfo = choiceOption.GetUIInfo();
            iconImage.sprite = uiInfo.Icon;
            nameText.text = uiInfo.Name;
            descriptionText.text = uiInfo.Description;
        }

        private void OnSelectButtonClick()
        {
            _triChoiceUI.OnChoiceSelected(_choiceOption);
            _isSelected = true;
        }

        public void SetButtonInteractable(bool interactable)
        {
            _selectButton.interactable = interactable;
        }

        #region Pointer Events
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
        #endregion
    }
}