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
        }

        private void UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<HitEvent>(SetHp);
            GameEventSystem.Unsubscribe<TrainDeadEvent>(SetDead);
            GameEventSystem.Unsubscribe<TrainLevelUpEvent>(SetLevelUp);
        }
        #endregion

        public void Init(Train train)
        {
            _train = train;
            trainLevelImage.gameObject.SetActive(false);
        }

        public void SetIcon(Sprite icon)
        {
            iconImage.sprite = icon;
        }

        private void SetHp(HitEvent hitEvent)
        {
            if (_train != hitEvent.Damageable as Train) return;

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
            if (_train != trainLevelUpEvent.Train) return;

            trainLevelImage.gameObject.SetActive(true);
            trainLevelText.text = $"{trainLevelUpEvent.Level + 1}";
        }

        private void SetDead(TrainDeadEvent trainDeadEvent)
        {
            if (_train != trainDeadEvent.Train) return;

            backGroundImage.color = Color.gray;
        }
    }
}