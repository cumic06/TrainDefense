using Cumic.Events;
using DG.Tweening;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class ExpUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private float tweenDuration = 0.4f;
        #endregion

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        private void Start()
        {
            GameEventSystem.Subscribe<AddExpEvent>(OnAddExp);

            if (UserDataManager.Instance != null)
            {
                _slider.value = UserDataManager.Instance.ExpPercent;
            }
            else
            {
                _slider.value = 0;
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<AddExpEvent>(OnAddExp);
        }

        private void OnAddExp(AddExpEvent addExpEvent)
        {
            if (UserDataManager.Instance == null) return;

            _slider.DOValue(UserDataManager.Instance.ExpPercent, tweenDuration);
        }
    }
}