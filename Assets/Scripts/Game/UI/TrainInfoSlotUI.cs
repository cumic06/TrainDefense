using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class TrainInfoSlotUI : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private Image backGroundImage;
        [SerializeField]
        private Image iconImage;
        #endregion

        private Train _train;

        private void Start()
        {
            backGroundImage.color = Color.green;
            GameEventSystem.Subscribe<HitEvent>(SetHp);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<HitEvent>(SetHp);
        }

        public void Init(Train train)
        {
            _train = train;
        }

        public void SetIcon(Sprite icon)
        {
            iconImage.sprite = icon;
        }

        public void SetHp(HitEvent hitEvent)
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
    }
}