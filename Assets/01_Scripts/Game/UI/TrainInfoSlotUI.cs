using UnityEngine;
using UnityEngine.UI;
using Cumic.Events;
using TrainDefense.Game.Events;
using TMPro;

namespace TrainDefense.Game.UI
{
   public class TrainInfoSlotUI : MonoBehaviour
   {
      #region Field
      [SerializeField]
      private Image backGroundImage;
      [SerializeField]
      private Image iconImage;

      [SerializeField]
      private Image trainLevelImage;
      [SerializeField]
      private TextMeshProUGUI trainLevelText;
      #endregion

      private Train _train;

      private void Start()
      {
         SubscribeEvents();
         backGroundImage.color = Color.green;
      }

      private void OnDestroy()
      {
         UnsubscribeEvents();
      }

      #region Event
      private void SubscribeEvents()
      {
         GameEventSystem.Subscribe<HitEvent>(SetHp);
         GameEventSystem.Subscribe<TrainDeadEvent>(SetDead);
         GameEventSystem.Subscribe<TrainLevelUpEvent>(SetLevelUp);
         GameEventSystem.Subscribe<ReplaceTrainEvent>(OnReplaceTrain);
      }

      private void UnsubscribeEvents()
      {
         GameEventSystem.Unsubscribe<HitEvent>(SetHp);
         GameEventSystem.Unsubscribe<TrainDeadEvent>(SetDead);
         GameEventSystem.Unsubscribe<TrainLevelUpEvent>(SetLevelUp);
         GameEventSystem.Unsubscribe<ReplaceTrainEvent>(OnReplaceTrain);
      }
      #endregion

      public void Init(Train train)
      {
         _train = train;
         trainLevelImage.gameObject.SetActive(false);
      }

      public void SetIcon(Sprite icon)
      {
         Debug.Log(icon);
         iconImage.sprite = icon;
      }

      private void SetHp(HitEvent hitEvent)
      {
         if (_train != hitEvent.Damageable as Train)
            return;

         if (hitEvent.CurrentHpRatio > 0.7f)
         {
            backGroundImage.color = Color.green;
         }
         else if (hitEvent.CurrentHpRatio <= 0.7f)
         {
            backGroundImage.color = Color.yellow;
         }
         else if (hitEvent.CurrentHpRatio <= 0.5f)
         {
            backGroundImage.color = Color.orange;
         }
         else if (hitEvent.CurrentHpRatio <= 0.3f)
         {
            backGroundImage.color = Color.red;
         }
         else
         {
            backGroundImage.color = Color.gray;
         }
      }

      private void SetLevelUp(TrainLevelUpEvent trainLevelUpEvent)
      {
         if (_train != trainLevelUpEvent.Train)
            return;

         trainLevelImage.gameObject.SetActive(true);
         trainLevelText.text = $"{trainLevelUpEvent.Level + 1}";
      }

      private void SetDead(TrainDeadEvent trainDeadEvent)
      {
         if (_train != trainDeadEvent.Train)
            return;

         backGroundImage.color = Color.gray;
      }

      private void OnReplaceTrain(ReplaceTrainEvent replaceTrainEvent)
      {
         // 이 슬롯이 대체될 oldTrain을 참조하고 있는지 확인
         if (_train != replaceTrainEvent.OldTrain)
            return;

         // 새로운 Train으로 교체
         _train = replaceTrainEvent.NewTrain;

         // 아이콘 업데이트
         if (replaceTrainEvent.NewIcon != null)
         {
            SetIcon(replaceTrainEvent.NewIcon);
         }

         // 레벨 UI 초기화 (새 Train은 레벨 0부터 시작)
         trainLevelImage.gameObject.SetActive(false);

         // HP 상태 초기화
         backGroundImage.color = Color.green;

         Debug.Log($"TrainInfoSlotUI: Replaced train UI");
      }
   }
}