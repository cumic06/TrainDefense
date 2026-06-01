using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;
using TrainDefense.Game;
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

         if (choiceOption is UpgradeTrainChoice upgradeTrainChoice)
         {
            if (skillNameText != null)
               skillNameText.gameObject.SetActive(false);

            var triChoiceManager = TriChoiceManager.Instance;
            var selectedUpgrade = triChoiceManager?.GetSelectedUpgrade(upgradeTrainChoice);
            if (selectedUpgrade != null)
            {
               descriptionText.text = $"<line-height=120%><size=80%>{GetUpgradeDescription(selectedUpgrade)}</size>";
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
               {
                  string passiveLabel = LocalizeHelper.GetByKey("skill_type_passive", "패시브");
                  parts.Add($"{choiceUIInfo.PassiveName}\n<size=70%><alpha=#99>[{passiveLabel}]</size>");
               }
               if (!string.IsNullOrEmpty(choiceUIInfo.ActiveSkillName))
               {
                  string activeLabel = LocalizeHelper.GetByKey("skill_type_active", "액티브");
                  parts.Add($"{choiceUIInfo.ActiveSkillName}\n<size=70%><alpha=#99>[{activeLabel}]</size>");
               }

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

            if (choiceOption is AddTrainChoice addTrainChoice)
            {
               string statText = GetTrainStatsDescription(addTrainChoice.TrainDataId);
               if (!string.IsNullOrEmpty(statText))
                  descriptionText.text += $"\n<size=50%>\n</size><line-height=70%><size=70%><color=#D7D3B3>{statText}</color></size>";
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
         Train currentTrain = null;
         if (_choiceOption is UpgradeTrainChoice upgradeChoice)
         {
            var trainManager = TrainManager.Instance;
            if (trainManager?.MainTrain != null)
            {
               currentTrain = trainManager.MainTrain.CurrentTrains
                   .FirstOrDefault(t => t.TrainData.Id == upgradeChoice.TargetTrainId);
               if (currentTrain != null)
               {
                  currentLevel = currentTrain.CurrentLevel + 1;
               }
            }
         }

         var lines = new System.Collections.Generic.List<string>();

         // 카드에 표시할 "현재값"은 상점 효과(배율)를 제외한, 포탑 업그레이드(트라이초이스)만 반영된 스탯.
         // = base 스탯 + 0~현재레벨까지 각 레벨 업그레이드 증가량의 합.
         // (train.CurrentStatus에는 상점 배율이 섞여 분리 불가하므로 upgradeData의 누적 메서드 사용)
         if (upgradeData is TurretTrainUpgradeData turretUpgrade && currentTrain is TurretTrain turretTrain)
         {
            var baseStatus = turretTrain.BaseStatus;
            var accumulated = turretUpgrade.GetAccumulatedTurretStatusUpgrade(turretTrain.CurrentLevel);
            float currentDamage = baseStatus.AttackDamage + accumulated.AttackDamage;
            float currentArea = baseStatus.AttackArea + accumulated.AttackArea;
            float currentInterval = baseStatus.AttackInterval + accumulated.AttackInterval;
            float currentRange = baseStatus.AttackRange + accumulated.AttackRange;
            int currentTargetCount = baseStatus.TargetCount + accumulated.TargetCount;

            var nextUpgrade = turretUpgrade.GetTurretStatusUpgrade(currentLevel);
            float currentSpeed = ToAttackSpeed(currentInterval);
            float speedDelta = ToAttackSpeed(currentInterval + nextUpgrade.AttackInterval) - currentSpeed;

            // 처음 선택 카드(GetTrainStatsDescription)와 같은 스탯 목록을 전체 화살표(Upgrade_, "현재 → 다음")로 표시.
            AddUpgradeOrStatLine(lines, "Upgrade_AttackDamage", "Stat_AttackDamage", currentDamage, nextUpgrade.AttackDamage);
            AddUpgradeOrStatLine(lines, "Upgrade_AttackSpeed", "Stat_AttackSpeed", currentSpeed, speedDelta);
            AddUpgradeOrStatLine(lines, "Upgrade_AttackRange", "Stat_AttackRange", currentRange, nextUpgrade.AttackRange);
            if (currentArea > 0f && turretTrain.TrainData is TurretTrainData turretData && UsesAttackArea(turretData))
               AddUpgradeOrStatLine(lines, "Upgrade_AttackArea", "Stat_AttackArea", currentArea, nextUpgrade.AttackArea);
            if (currentTargetCount > 1)
               AddUpgradeOrStatLine(lines, "Upgrade_TargetCount", "Stat_TargetCount", currentTargetCount, nextUpgrade.TargetCount);
         }
         else if (upgradeData is RangeTrainUpgradeData rangeUpgrade && currentTrain is RangeTrain rangeTrain)
         {
            var baseStatus = rangeTrain.BaseStatus;
            var accumulated = rangeUpgrade.GetAccumulatedRangeStatusUpgrade(rangeTrain.CurrentLevel);
            float currentDamage = baseStatus.AttackDamage + accumulated.AttackDamage;
            float currentArea = baseStatus.AttackArea + accumulated.AttackArea;
            float currentInterval = baseStatus.AttackInterval + accumulated.AttackInterval;

            var nextUpgrade = rangeUpgrade.GetRangeStatusUpgrade(currentLevel);
            float currentSpeed = ToAttackSpeed(currentInterval);
            float speedDelta = ToAttackSpeed(currentInterval + nextUpgrade.AttackInterval) - currentSpeed;

            AddUpgradeOrStatLine(lines, "Upgrade_AttackDamage", "Stat_AttackDamage", currentDamage, nextUpgrade.AttackDamage);
            AddUpgradeOrStatLine(lines, "Upgrade_AttackSpeed", "Stat_AttackSpeed", currentSpeed, speedDelta);
            AddUpgradeOrStatLine(lines, "Upgrade_AttackArea", "Stat_AttackArea", currentArea, nextUpgrade.AttackArea);
         }

         return string.Join("\n", lines);
      }

      // 새 포탑 선택 카드용: 해당 포탑의 base 스탯을 줄 단위로 반환.
      private string GetTrainStatsDescription(string trainDataId)
      {
         var trainData = DatabaseManager.Instance?.GetTrainData(trainDataId);
         if (trainData == null) return null;

         var lines = new System.Collections.Generic.List<string>();
         if (trainData is TurretTrainData turretData)
         {
            var s = turretData.TurretTrainStatus;
            AddStatValueLine(lines, "Stat_AttackDamage", s.AttackDamage);
            AddStatValueLine(lines, "Stat_AttackSpeed", ToAttackSpeed(s.AttackInterval));
            AddStatValueLine(lines, "Stat_AttackRange", s.AttackRange);
            if (s.AttackArea > 0f && UsesAttackArea(turretData))
               AddStatValueLine(lines, "Stat_AttackArea", s.AttackArea);
            if (s.TargetCount > 1)
               AddStatValueLine(lines, "Stat_TargetCount", s.TargetCount);
         }
         else if (trainData is RangeTrainData rangeData)
         {
            var s = rangeData.RangeTrainStatus;
            AddStatValueLine(lines, "Stat_AttackDamage", s.AttackDamage);
            AddStatValueLine(lines, "Stat_AttackSpeed", ToAttackSpeed(s.AttackInterval));
            AddStatValueLine(lines, "Stat_AttackArea", s.AttackArea);
         }
         return string.Join("\n", lines);
      }

      // 공격 딜레이(초)를 초당 공격 횟수(공격속도)로 변환. 시스템 값이 아닌 UI 표시 전용.
      private static float ToAttackSpeed(float interval) => interval > 0f ? 1f / interval : 0f;

      // 값을 공백 PadLeft → txt의 <mspace>(고정폭) 안에서 자릿수가 달라도 우측 끝이 정렬됨. 소수는 첫째 자리까지.
      private const int StatValueWidth = 3;
      private static string AlignStatValue(float value)
         => value.ToString("0.#").PadLeft(StatValueWidth, ' ');

      // 포탑이 AttackArea 스탯을 실제 폭발 반경으로 쓰는지 판정 (빔 길이·파티클 비율은 제외).
      private static bool UsesAttackArea(TurretTrainData turretData)
      {
         var prefab = turretData?.TurretProjectilePrefab;
         if (prefab == null) return false;
         if (!prefab.TryGetComponent<Projectile>(out var projectile)) return false;
         var data = projectile.GetData();
         return data != null && data.ScaleByArea && data.ScaleRangeType == ScaleByRangeType.Area && data.IsSpawnTriggerHandle;
      }

      // 업그레이드 카드용: 값이 바뀌는 스탯(delta≠0)만 "현재값 → 다음값"(upgradeKey) 화살표로 표시.
      // 변화 없는 스탯은 statKey로 현재값만 표시. 해당 키가 없으면 조용히 건너뛴다.
      private static void AddUpgradeOrStatLine(System.Collections.Generic.List<string> lines, string upgradeKey, string statKey, float currentValue, float delta)
      {
         if (delta != 0)
         {
            string upgradeTemplate = Localization.GetByKey(upgradeKey);
            if (!string.IsNullOrEmpty(upgradeTemplate))
            {
               // 현재값({0})은 우측 정렬(PadLeft), 바뀔 값({1})은 좌측 정렬(PadLeft 없이 raw).
               lines.Add(SafeFormat(upgradeTemplate, new object[] { AlignStatValue(currentValue), (currentValue + delta).ToString("0.#") }));
               return;
            }
         }

         string statTemplate = Localization.GetByKey(statKey);
         if (!string.IsNullOrEmpty(statTemplate))
            lines.Add(SafeFormat(statTemplate, new object[] { AlignStatValue(currentValue) }));
      }

      // 단일 스탯 값을 "레이블 값" 한 줄로 추가 (새 포탑 카드용).
      private static void AddStatValueLine(System.Collections.Generic.List<string> lines, string statKey, float value)
      {
         string template = Localization.GetByKey(statKey);
         if (string.IsNullOrEmpty(template))
            return;

         lines.Add(SafeFormat(template, new object[] { AlignStatValue(value) }));
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
