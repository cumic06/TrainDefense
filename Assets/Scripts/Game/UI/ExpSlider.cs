using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
    public class ExpSlider : MonoBehaviour
    {
        private Slider _expSlider;

        private void Awake()
        {
            _expSlider = GetComponent<Slider>();
        }

        private void Start()
        {
            _expSlider.value = 0;
            GameEventSystem.Subscribe<ExpUpEvent>(SetExp);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<ExpUpEvent>(SetExp);
        }

        public void SetExp(ExpUpEvent expUpEvent)
        {
            _expSlider.DOValue(expUpEvent.CurrentExpRatio, 0.25f);
        }
    }
}