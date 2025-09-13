using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
    public class HpSlider : MonoBehaviour
    {
        private Slider _hpSlider;

        private void Awake()
        {
            _hpSlider = GetComponent<Slider>();
        }

        private void Start()
        {
            GameEventSystem.Subscribe<HitEvent>(SetHp);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<HitEvent>(SetHp);
        }

        public void SetHp(HitEvent hitEvent)
        {
            _hpSlider.DOValue(hitEvent.CurrentHpRatio, 0.25f);
        }
    }
}