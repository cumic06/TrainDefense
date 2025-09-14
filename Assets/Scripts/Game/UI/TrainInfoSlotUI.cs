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

        private void Start()
        {
            backGroundImage.color = Color.green;
            GameEventSystem.Subscribe<HitEvent>(SetHp);
        }

        public void SetIcon(Sprite icon)
        {
            iconImage.sprite = icon;
        }

        public void SetHp(HitEvent hitEvent)
        {
            backGroundImage.color = Color.Lerp(Color.green, Color.red, hitEvent.CurrentHpRatio);
        }
    }
}