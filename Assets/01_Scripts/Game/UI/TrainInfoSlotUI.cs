using System.Collections;
using Cumic.Events;
using TMPro;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
   [RequireComponent(typeof(Button))]
   public class TrainInfoSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
   {
      #region Verialbes
      #region Field
      [SerializeField]
      private Button slotButton;
      [SerializeField]
      private Image healthImage;
      [SerializeField]
      private Image iconImage;
      [SerializeField]
      private Image skillIcon;
      [SerializeField]
      private Image skillCooldownImage;
      [SerializeField]
      private TextMeshProUGUI skillCooldownText;

      [SerializeField]
      private Image trainLevelImage;
      [SerializeField]
      private TextMeshProUGUI trainLevelText;

      [SerializeField]
      private TrainDetailPopupUI detailPopup;
      #endregion

      private Train _train;
      private const float LongPressDuration = 0.5f;
      private Coroutine _longPressCoroutine;
      private bool _longPressFired;
      #endregion

      private void Awake()
      {
         if (slotButton == null)
         {
            slotButton = GetComponent<Button>();
         }

         slotButton.onClick.AddListener(_OnClickSlot);
      }

      private void Start()
      {
         _SubscribeEvents();
         _RefreshHealthUI();
         _RefreshSkillUI();
      }

      private void Update()
      {
         _UpdateSkillCooldownUI();
      }

      private void OnDestroy()
      {
         if (slotButton != null)
         {
            slotButton.onClick.RemoveListener(_OnClickSlot);
         }

         if (_longPressCoroutine != null)
         {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
         }

         _UnsubscribeEvents();
      }

      public void OnPointerDown(PointerEventData eventData)
      {
         _longPressFired = false;
         _longPressCoroutine = StartCoroutine(_LongPressRoutine());
      }

      public void OnPointerUp(PointerEventData eventData)
      {
         if (_longPressCoroutine != null)
         {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
         }

         if (_longPressFired)
            detailPopup?.Hide();
      }

      private IEnumerator _LongPressRoutine()
      {
         yield return new WaitForSecondsRealtime(LongPressDuration);
         _longPressFired = true;
         detailPopup?.Show(_train);
      }

      #region Event
      private void _SubscribeEvents()
      {
         GameEventSystem.Subscribe<HitEvent>(_SetHp);
         GameEventSystem.Subscribe<TrainDeadEvent>(_SetDead);
         GameEventSystem.Subscribe<TrainLevelUpEvent>(_SetLevelUp);
         GameEventSystem.Subscribe<ReplaceTrainEvent>(_OnReplaceTrain);
      }

      private void _UnsubscribeEvents()
      {
         GameEventSystem.Unsubscribe<HitEvent>(_SetHp);
         GameEventSystem.Unsubscribe<TrainDeadEvent>(_SetDead);
         GameEventSystem.Unsubscribe<TrainLevelUpEvent>(_SetLevelUp);
         GameEventSystem.Unsubscribe<ReplaceTrainEvent>(_OnReplaceTrain);
      }
      #endregion

      public void Init(Train train)
      {
         _train = train;
         trainLevelImage.gameObject.SetActive(false);
         _RefreshHealthUI();
         _RefreshSkillUI();
      }

      public void SetIcon(Sprite icon)
      {
         iconImage.sprite = icon;
      }

      private void _RefreshHealthUI()
      {
         if (healthImage == null)
            return;

         float ratio = _train != null ? _train.CurrentHpRatio : 1f;
         healthImage.fillAmount = ratio;
         healthImage.color = _GetHealthColor(ratio);
      }

      private static Color _GetHealthColor(float ratio)
      {
         if (ratio <= 0.25f)
            return Color.red;
         if (ratio <= 0.5f)
            return new Color(1f, 0.5f, 0f);
         if (ratio <= 0.7f)
            return Color.yellow;
         return Color.green;
      }

      private void _RefreshSkillUI()
      {
         bool hasSkill = _train != null && _train.HasActiveSkill;

         if (skillIcon != null)
         {
            skillIcon.gameObject.SetActive(hasSkill);
            if (hasSkill)
            {
               skillIcon.sprite = _train.SkillIcon;
            }
         }

         if (skillCooldownImage != null)
         {
            skillCooldownImage.gameObject.SetActive(hasSkill);
            skillCooldownImage.fillAmount = hasSkill ? _train.SkillCooldownRatio : 0f;
         }

         if (skillCooldownText != null)
         {
            // 텍스트는 쿨다운이 남아있을 때만 노출, 초기/스킬 없음 상태는 숨김
            skillCooldownText.gameObject.SetActive(false);
         }
      }

      private void _UpdateSkillCooldownUI()
      {
         if (_train == null || !_train.HasActiveSkill)
            return;

         if (skillCooldownImage != null)
         {
            skillCooldownImage.fillAmount = _train.SkillCooldownRatio;
         }

         if (skillCooldownText != null)
         {
            float remaining = _train.SkillRemainingCooldown;
            bool onCooldown = remaining > 0f;

            if (skillCooldownText.gameObject.activeSelf != onCooldown)
            {
               skillCooldownText.gameObject.SetActive(onCooldown);
            }

            if (onCooldown)
            {
               skillCooldownText.text = $"{remaining:0.0}s";
            }
         }
      }

      private void _OnClickSlot()
      {
         if (_longPressFired)
         {
            _longPressFired = false;
            return;
         }

         if (_train == null)
         {
            Debug.LogWarning("[TrainInfoSlotUI] _train is null, cannot use skill");
            return;
         }

         if (!_train.HasActiveSkill)
         {
            Debug.LogWarning($"[TrainInfoSlotUI] {_train.name} does not have skill");
            return;
         }

         TrainManager.Instance.TryUseTrainSkill(_train);
         _UpdateSkillCooldownUI();
      }

      private void _SetHp(HitEvent hitEvent)
      {
         if (_train != hitEvent.Damageable as Train)
            return;

         float ratio = hitEvent.CurrentHpRatio;

         if (healthImage != null)
         {
            healthImage.fillAmount = ratio;
            healthImage.color = _GetHealthColor(ratio);
         }
      }

      private void _SetLevelUp(TrainLevelUpEvent trainLevelUpEvent)
      {
         if (_train != trainLevelUpEvent.Train)
            return;

         trainLevelImage.gameObject.SetActive(true);
         trainLevelText.text = $"{trainLevelUpEvent.Level + 1}";
      }

      private void _SetDead(TrainDeadEvent trainDeadEvent)
      {
         if (_train != trainDeadEvent.Train)
            return;

         if (healthImage != null)
         {
            healthImage.color = Color.gray;
         }
      }

      private void _OnReplaceTrain(ReplaceTrainEvent replaceTrainEvent)
      {
         if (_train != replaceTrainEvent.OldTrain)
            return;

         _train = replaceTrainEvent.NewTrain;

         if (replaceTrainEvent.NewIcon != null)
         {
            SetIcon(replaceTrainEvent.NewIcon);
         }

         trainLevelImage.gameObject.SetActive(true);
         trainLevelText.text = "E";
         _RefreshHealthUI();
         _RefreshSkillUI();
      }
   }
}
