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

            backGroundImage.color = hitEvent.CurrentHpRatio > 0.5f ? Color.green : Color.red;

            // backGroundImage.color = Color.Lerp(Color.green, Color.red, hitEvent.CurrentHpRatio);
        }
    }
}