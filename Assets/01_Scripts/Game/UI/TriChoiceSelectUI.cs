using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;
using System.Linq;
using System.Text.RegularExpressions;

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
      #endregion

      private Button _selectButton;
      private Image _cardImage;
      private IChoiceOption _choiceOption;
      private TriChoiceUI _triChoiceUI;

      private bool _isSelected = false;

      private void Awake()
      {
         _selectButton = GetComponent<Button>();
         _cardImage = GetComponent<Image>();
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
         iconImage.gameObject.SetActive(choiceUIInfo.Icon != null);
         _cardImage.material = (choiceOption is EliteTrainChoice) ? eliteOutlineMaterial : null;
         nameText.text = choiceUIInfo.Name;

         if (choiceOption is UpgradeTrainChoice upgradeTrainChoice)
         {
            if (skillNameText != null)
               skillNameText.gameObject.SetActive(false);

            var triChoiceManager = TriChoiceManager.Instance;
            var selectedUpgrade = triChoiceManager?.GetSelectedUpgrade(upgradeTrainChoice);
            if (selectedUpgrade != null)
            {
               descriptionText.text = GetUpgradeDescription(selectedUpgrade);
               upgradeImage.gameObject.SetActive(true);
            }
            else
            {
               descriptionText.text = "";
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

      private static string SafeFormat(string format, object[] args)
      {
         if (string.IsNullOrEmpty(format)) return format;
         try
         {
            return string.Format(format, args);
         }
         catch (System.FormatException)
         {
            Debug.LogWarning($"[TriChoiceSelectUI] Format mismatch: {format} (args={args.Length})");
            return StripFormatPlaceholders(format);
         }
      }

      private static string StripFormatPlaceholders(string text)
      {
         if (string.IsNullOrEmpty(text)) return text;
         return Regex.Replace(text, @"\{[0-9]+\}", "-");
      }

      private string GetUpgradeDescription(ITrainUpgradeData upgradeData)
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

         var lines = new System.Collections.Generic.List<string>();

         var statusUpgrade = upgradeData.GetStatusUpgrade(currentLevel);
         AddStatLine(lines, "Upgrade_MaxHp", statusUpgrade.MaxHp);

         if (upgradeData is TurretTrainUpgradeData turretUpgrade)
         {
            var turretStatus = turretUpgrade.GetTurretStatusUpgrade(currentLevel);
            AddStatLine(lines, "Upgrade_AttackDamage", turretStatus.AttackDamage);
            AddStatLine(lines, "Upgrade_AttackRange", turretStatus.AttackRange);
            AddStatLine(lines, "Upgrade_AttackArea", turretStatus.AttackArea);
            AddStatLine(lines, "Upgrade_AttackCount", turretStatus.AttackCount);
            AddStatLine(lines, "Upgrade_AttackInterval", turretStatus.AttackInterval);
            AddStatLine(lines, "Upgrade_TargetCount", turretStatus.TargetCount);
         }
         else if (upgradeData is RangeTrainUpgradeData rangeUpgrade)
         {
            var rangeStatus = rangeUpgrade.GetRangeStatusUpgrade(currentLevel);
            AddStatLine(lines, "Upgrade_AttackDamage", rangeStatus.AttackDamage);
            AddStatLine(lines, "Upgrade_AttackRange", rangeStatus.AttackRange);
            AddStatLine(lines, "Upgrade_AttackArea", rangeStatus.AttackArea);
            AddStatLine(lines, "Upgrade_AttackCount", rangeStatus.AttackCount);
            AddStatLine(lines, "Upgrade_AttackInterval", rangeStatus.AttackInterval);
         }

         return string.Join("\n", lines);
      }

      // 스탯 키가 LocalizeSource에 존재하고 값이 0이 아닐 때만 한 줄로 추가.
      // 키가 없으면(아직 정의 안 된 스탯) 조용히 건너뛴다.
      private static void AddStatLine(System.Collections.Generic.List<string> lines, string statKey, float value)
      {
         if (value == 0)
            return;

         string template = Localization.GetByKey(statKey);
         if (string.IsNullOrEmpty(template))
            return;

         lines.Add(SafeFormat(template, new object[] { value }));
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
