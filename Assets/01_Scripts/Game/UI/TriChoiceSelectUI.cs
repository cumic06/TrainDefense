using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;
using TrainDefense.Game;
using System.Linq;

namespace TrainDefense.Game.UI
{
   public class TriChoiceSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
   {
      #region Fields
      [SerializeField]
      private TextMeshProUGUI nameText;
      [SerializeField]
      private TextMeshProUGUI descriptionText;
      [SerializeField]
      private Image iconImage;
      [SerializeField]
      private Image newImage;
      [SerializeField]
      private Image upgradeImage;
      [SerializeField]
      private TextMeshProUGUI skillNameText;
      [SerializeField]
      private Material eliteOutlineMaterial;
      [SerializeField]
      private GameObject eliteObject;
      #endregion

      private Button _selectButton;
      private Image _cardImage;
      private IChoiceOption _choiceOption;
      private TriChoiceUI _triChoiceUI;

      private bool _isSelected = false;
      private float _baseDescFontSize;

      private void Awake()
      {
         _selectButton = GetComponent<Button>();
         _cardImage = GetComponent<Image>();
         _baseDescFontSize = descriptionText.fontSize;
      }

      private void Start()
      {
         _selectButton.onClick.AddListener(OnSelectButtonClick);
      }

      public void SetData(IChoiceOption choiceOption, ChoiceUIInfo choiceUIInfo, TriChoiceUI triChoiceUI)
      {
         if (choiceOption == null)
            return;

         _choiceOption = choiceOption;
         _triChoiceUI = triChoiceUI;

         SetUI(choiceOption, choiceUIInfo);
      }

      private void SetUI(IChoiceOption choiceOption, ChoiceUIInfo choiceUIInfo)
      {
         if (string.IsNullOrEmpty(choiceUIInfo.Name))
         {
            Debug.LogError($"ChoiceOption [{choiceOption.Id}]: Name is null");
            return;
         }

         // 영어는 글이 길어 description 폰트만 줄임
         descriptionText.fontSize = Localization.CurrentLanguage == SystemLanguage.English
            ? _baseDescFontSize * 0.85f : _baseDescFontSize;

         iconImage.sprite = choiceUIInfo.Icon;
         iconImage.gameObject.SetActive(choiceUIInfo.Icon != null);
         bool isElite = choiceOption is EliteTrainChoice;
         _cardImage.material = isElite ? eliteOutlineMaterial : null;
         if (eliteObject != null)
            eliteObject.SetActive(isElite);
         nameText.text = choiceUIInfo.Name;

         descriptionText.text = choiceUIInfo.Description;
         upgradeImage.gameObject.SetActive(false);

         if (skillNameText != null)
         {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(choiceUIInfo.PassiveName))
               parts.Add(choiceUIInfo.PassiveName);

            bool hasContent = parts.Count > 0;
            skillNameText.gameObject.SetActive(hasContent);
            if (hasContent)
               skillNameText.text = string.Join("\n", parts);
         }

         var skillDescriptions = new System.Collections.Generic.List<string>();
         if (!string.IsNullOrEmpty(choiceUIInfo.PassiveDescription))
            skillDescriptions.Add(choiceUIInfo.PassiveDescription);

         if (skillDescriptions.Count > 0)
         {
            string skillDescription = string.Join("\n", skillDescriptions);
            if (!string.IsNullOrEmpty(choiceUIInfo.Description))
               descriptionText.text = $"{choiceUIInfo.Description}\n{skillDescription}";
            else
               descriptionText.text = skillDescription;
         }
      }

      private void OnSelectButtonClick()
      {
         if (_isSelected || _choiceOption == null || _triChoiceUI == null)
            return;

         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_TriChoiceSelect, ignoreSuppress: true);

         if (HapticManager.Instance != null)
         {
            HapticManager.Instance.Play(HapticFeedbackType.LightImpact);
         }

         _selectButton.interactable = false;
         SetSelected(true);
         _triChoiceUI.OnChoiceSelected(_choiceOption).Forget();
      }

      public void SetButtonInteractable(bool interactable)
      {
         _selectButton.interactable = interactable;
      }

      public void SetSelected(bool selected)
      {
         _isSelected = selected;
      }

      public void SetNewText(bool isNew)
      {
         newImage.gameObject.SetActive(isNew);
      }

      #region Pointer Events
      public void OnPointerEnter(PointerEventData eventData)
      {
         if (_isSelected)
            return;

         transform.DOKill();
         transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutBack).SetUpdate(true);
      }

      public void OnPointerExit(PointerEventData eventData)
      {
         if (_isSelected)
            return;

         transform.DOKill();
         transform.DOScale(1f, 0.1f).SetEase(Ease.InBack).SetUpdate(true);
      }
      #endregion
   }
}
