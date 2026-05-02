using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TrainDefense.Game.Datas;
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

         iconImage.sprite = choiceUIInfo.Icon;
         nameText.text = choiceUIInfo.Name;

         if (choiceOption is UpgradeTrainChoice upgradeTrainChoice)
         {
            if (skillNameText != null)
               skillNameText.gameObject.SetActive(false);

            var triChoiceManager = TriChoiceManager.Instance;
            if (triChoiceManager != null)
            {
               var selectedUpgrade = triChoiceManager.GetSelectedUpgrade(upgradeTrainChoice);
               if (selectedUpgrade != null)
               {
                  object[] formatArgs = GetUpgradeFormatArgs(selectedUpgrade);
                  try
                  {
                     descriptionText.text = string.Format(choiceUIInfo.Description, formatArgs);
                     upgradeImage.gameObject.SetActive(true);
                  }
                  catch (System.FormatException)
                  {
                     descriptionText.text = choiceUIInfo.Description;
                     Debug.LogWarning($"[TriChoiceSelectUI] Format Error: {choiceUIInfo.Description}");
                  }
               }
               else
               {
                  descriptionText.text = choiceUIInfo.Description;
               }
            }
            else
            {
               descriptionText.text = choiceUIInfo.Description;
               upgradeImage.gameObject.SetActive(false);
            }
         }
         else
         {
            descriptionText.text = choiceUIInfo.Description;
            upgradeImage.gameObject.SetActive(false);

            if (skillNameText != null)
            {
               var parts = new System.Collections.Generic.List<string>();
               if (!string.IsNullOrEmpty(choiceUIInfo.PassiveName))
                  parts.Add($"-{choiceUIInfo.PassiveName}-");
               if (!string.IsNullOrEmpty(choiceUIInfo.ActiveSkillName))
                  parts.Add($"-{choiceUIInfo.ActiveSkillName}-");

               bool hasContent = parts.Count > 0;
               skillNameText.gameObject.SetActive(hasContent);
               if (hasContent)
                  skillNameText.text = string.Join("\n", parts);
            }

            var skillDescriptions = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(choiceUIInfo.PassiveDescription))
               skillDescriptions.Add(choiceUIInfo.PassiveDescription);
            if (!string.IsNullOrEmpty(choiceUIInfo.ActiveSkillDescription))
               skillDescriptions.Add(choiceUIInfo.ActiveSkillDescription);

            if (skillDescriptions.Count > 0)
            {
               string skillDescription = string.Join("\n", skillDescriptions);
               if (!string.IsNullOrEmpty(choiceUIInfo.Description))
                  descriptionText.text = $"{choiceUIInfo.Description}\n{skillDescription}";
               else
                  descriptionText.text = skillDescription;
            }
         }
      }

      private object[] GetUpgradeFormatArgs(ITrainUpgradeData upgradeData)
      {
         int currentLevel = 0;
         if (_choiceOption is UpgradeTrainChoice upgradeChoice)
         {
            var trainManager = TrainManager.Instance;
            if (trainManager?.MainTrain != null)
            {
               var train = trainManager.MainTrain.CurrentTrains
                   .FirstOrDefault(t => t.TrainData.Id == upgradeChoice.TargetTrainId);
               if (train != null)
               {
                  currentLevel = train.CurrentLevel + 1;
               }
            }
         }

         var statusUpgrade = upgradeData.GetStatusUpgrade(currentLevel);
         var args = new System.Collections.Generic.List<object>();

         if (statusUpgrade.MaxHp != 0)
         {
            args.Add(statusUpgrade.MaxHp);
         }

         if (upgradeData is TurretTrainUpgradeData turretUpgrade)
         {
            var turretStatus = turretUpgrade.GetTurretStatusUpgrade(currentLevel);
            if (turretStatus.AttackDamage != 0)
               args.Add(turretStatus.AttackDamage);
            if (turretStatus.AttackRange != 0)
               args.Add(turretStatus.AttackRange);
            if (turretStatus.AttackArea != 0)
               args.Add(turretStatus.AttackArea);
            if (turretStatus.AttackCount != 0)
               args.Add(turretStatus.AttackCount);
            if (turretStatus.AttackInterval != 0)
               args.Add(turretStatus.AttackInterval);
            if (turretStatus.TargetCount != 0)
               args.Add(turretStatus.TargetCount);
         }
         else if (upgradeData is RangeTrainUpgradeData rangeUpgrade)
         {
            var rangeStatus = rangeUpgrade.GetRangeStatusUpgrade(currentLevel);
            if (rangeStatus.AttackDamage != 0)
               args.Add(rangeStatus.AttackDamage);
            if (rangeStatus.AttackRange != 0)
               args.Add(rangeStatus.AttackRange);
            if (rangeStatus.AttackArea != 0)
               args.Add(rangeStatus.AttackArea);
            if (rangeStatus.AttackCount != 0)
               args.Add(rangeStatus.AttackCount);
            if (rangeStatus.AttackInterval != 0)
               args.Add(rangeStatus.AttackInterval);
         }

         return args.ToArray();
      }

      private void OnSelectButtonClick()
      {
         if (_isSelected || _choiceOption == null || _triChoiceUI == null)
            return;

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
