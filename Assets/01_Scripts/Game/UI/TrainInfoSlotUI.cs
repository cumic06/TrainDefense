using Cumic.Events;
using TMPro;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
   [RequireComponent(typeof(Button))]
   public class TrainInfoSlotUI : MonoBehaviour
   {
      #region Verialbes
      #region Field
      [SerializeField]
      private Button slotButton;
      [SerializeField]
      private Image backGroundImage;
      [SerializeField]
      private Image iconImage;
      [SerializeField]
      private Image skillIcon;
      [SerializeField]
      private Image skillCooldownImage;

      [SerializeField]
      private Image trainLevelImage;
      [SerializeField]
      private TextMeshProUGUI trainLevelText;
      #endregion

      private Train _train;
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
         backGroundImage.color = Color.green;
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

         _UnsubscribeEvents();
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
         _RefreshSkillUI();
      }

      public void SetIcon(Sprite icon)
      {
         iconImage.sprite = icon;
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
      }

      private void _UpdateSkillCooldownUI()
      {
         if (skillCooldownImage == null || _train == null || !_train.HasActiveSkill)
            return;

         skillCooldownImage.fillAmount = _train.SkillCooldownRatio;
      }

      private void _OnClickSlot()
      {
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

         if (ratio <= 0.25f)
         {
            backGroundImage.color = Color.red;
         }
         else if (ratio <= 0.5f)
         {
            backGroundImage.color = new Color(1f, 0.5f, 0f);
         }
         else if (ratio <= 0.7f)
         {
            backGroundImage.color = Color.yellow;
         }
         else
         {
            backGroundImage.color = Color.green;
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

         backGroundImage.color = Color.gray;
      }

      private void _OnReplaceTrain(ReplaceTrainEvent replaceTrainEvent)
      {
         if (_train != replaceTrainEvent.OldTrain)
         {
            Debug.LogWarning($"[TrainInfoSlotUI] OldTrain mismatch - Expected: {(_train != null ? _train.name : "null")}, Got: {(replaceTrainEvent.OldTrain != null ? replaceTrainEvent.OldTrain.name : "null")}");
            return;
         }

         _train = replaceTrainEvent.NewTrain;

         if (replaceTrainEvent.NewIcon != null)
         {
            SetIcon(replaceTrainEvent.NewIcon);
         }

         trainLevelImage.gameObject.SetActive(false);
         backGroundImage.color = Color.green;
         _RefreshSkillUI();
      }
   }
}
