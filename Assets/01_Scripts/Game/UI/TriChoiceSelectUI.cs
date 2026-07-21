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
               // 끝의 빈 줄이 세로 중앙 정렬 계산에 포함돼 내용이 반 줄가량 위로 올라간다(살짝 위 배치용).
               descriptionText.text = $"<line-height=120%>{GetUpgradeDescription(selectedUpgrade)}\n";
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
                  currentLevel = currentTrain.CurrentLevel;
               }
            }
         }

         var lines = new System.Collections.Generic.List<string>();

         // 값이 바뀌는 스탯만 "레이블 +증가량"으로 표시. (현재값 표기는 상점 배율 제외라 실전투값과 달라 혼동 + 긴 언어 오버플로우로 폐기)
         // 공격 속도만 간격(초) 감소를 속도 증가율(%)로 환산 — 현재 간격이 필요해 누적 메서드 사용.
         if (upgradeData is TurretTrainUpgradeData turretUpgrade && currentTrain is TurretTrain turretTrain)
         {
            var baseStatus = turretTrain.BaseStatus;
            var accumulated = turretUpgrade.GetAccumulatedTurretStatusUpgrade(turretTrain.CurrentLevel);
            float currentInterval = baseStatus.AttackInterval + accumulated.AttackInterval;
            float currentRange = baseStatus.AttackRange + accumulated.AttackRange;
            float currentArea = baseStatus.AttackArea + accumulated.AttackArea;

            var nextUpgrade = turretUpgrade.GetTurretStatusUpgrade(currentLevel);

            AddUpgradeDeltaLine(lines, "Upgrade_AttackDamage", nextUpgrade.AttackDamage);
            AddUpgradeDeltaLine(lines, "Upgrade_AttackSpeed", nextUpgrade.AttackInterval, delta => FormatAttackSpeedDelta(currentInterval, delta));
            AddUpgradeDeltaLine(lines, "Upgrade_AttackRange", nextUpgrade.AttackRange, delta => FormatSizeDelta(currentRange, delta));
            // 범위 업글이 있는 포탑(화염 파티클·레이저 빔 굵기·미사일/포격 폭발)만 델타≠0으로 표시됨 — 별도 게이트 불필요.
            AddUpgradeDeltaLine(lines, "Upgrade_AttackArea", nextUpgrade.AttackArea, delta => FormatSizeDelta(currentArea, delta));
            AddUpgradeDeltaLine(lines, "Upgrade_TargetCount", nextUpgrade.TargetCount);
            AddUpgradeDeltaLine(lines, "Upgrade_AttackCount", nextUpgrade.AttackCount);
            AddUpgradeDeltaLine(lines, "Upgrade_BurstDuration", nextUpgrade.BurstDuration);
         }
         else if (upgradeData is RangeTrainUpgradeData rangeUpgrade && currentTrain is RangeTrain rangeTrain)
         {
            var baseStatus = rangeTrain.BaseStatus;
            var accumulated = rangeUpgrade.GetAccumulatedRangeStatusUpgrade(rangeTrain.CurrentLevel);
            float currentInterval = baseStatus.AttackInterval + accumulated.AttackInterval;
            float currentArea = baseStatus.AttackArea + accumulated.AttackArea;

            var nextUpgrade = rangeUpgrade.GetRangeStatusUpgrade(currentLevel);

            AddUpgradeDeltaLine(lines, "Upgrade_AttackDamage", nextUpgrade.AttackDamage);
            AddUpgradeDeltaLine(lines, "Upgrade_AttackSpeed", nextUpgrade.AttackInterval, delta => FormatAttackSpeedDelta(currentInterval, delta));
            AddUpgradeDeltaLine(lines, "Upgrade_AttackArea", nextUpgrade.AttackArea, delta => FormatSizeDelta(currentArea, delta));
            AddUpgradeDeltaLine(lines, "Upgrade_Slow", nextUpgrade.SlowRate);
            AddUpgradeDeltaLine(lines, "Upgrade_BurstDuration", nextUpgrade.BurstDuration);
         }

         return string.Join("\n", lines);
      }

      // 포탑이 AttackArea 스탯을 실제 폭발 반경으로 쓰는지 판정 (빔 길이·파티클 비율은 제외).
      // 업그레이드 카드용: 값이 바뀌는 스탯(delta≠0)만 "레이블 +증가량"(upgradeKey)으로 표시.
      // 변화 없는 스탯은 표시하지 않는다. 해당 키가 없으면 조용히 건너뛴다.
      // formatter가 주어지면 증가량 표기를 위임(예: 공격 속도의 % 환산), 없으면 "+0.#" 서식.
      private static void AddUpgradeDeltaLine(System.Collections.Generic.List<string> lines, string upgradeKey, float delta, System.Func<float, string> formatter = null)
      {
         if (delta == 0)
            return;

         string upgradeTemplate = Localization.GetByKey(upgradeKey);
         if (string.IsNullOrEmpty(upgradeTemplate))
            return;

         string deltaText = formatter != null ? formatter(delta) : $"{(delta > 0 ? "+" : "")}{delta:0.#}";
         if (string.IsNullOrEmpty(deltaText))
            return;

         lines.Add(SafeFormat(upgradeTemplate, new object[] { deltaText }));
      }

      // 공격 간격(초) 감소를 공격 속도 증가율(%)로 환산해 표기. 예: 0.25→0.2초 = +25%
      private static string FormatAttackSpeedDelta(float currentInterval, float deltaInterval)
      {
         float nextInterval = currentInterval + deltaInterval;
         if (currentInterval <= 0f || nextInterval <= 0f)
            return null;

         float percent = (currentInterval / nextInterval - 1f) * 100f;
         return $"{(percent > 0 ? "+" : "")}{percent:0.#}%";
      }

      // 범위/사거리 증가를 현재값 대비 %로 표기 — 반경(크기) 기준이라 유저가 보는 원 크기 변화와 일치한다.
      private static string FormatSizeDelta(float currentValue, float delta)
      {
         if (currentValue <= 0f)
            return null;

         float percent = delta / currentValue * 100f;
         return $"{(percent > 0 ? "+" : "")}{percent:0.#}%";
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
