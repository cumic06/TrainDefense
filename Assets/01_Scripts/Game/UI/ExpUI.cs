using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using TMPro;

namespace TrainDefense.Game.UI
{
    public class ExpUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private float tweenDuration = 0.4f;
        [SerializeField]
        private TMP_Text expText;
        #endregion

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        private void Start()
        {
            GameEventSystem.Subscribe<AddExpEvent>(OnAddExp);
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);

            Setup();
        }

        private void Setup()
        {
            // if (UserDataManager.Instance != null)
            // {
            //     _slider.value = UserDataManager.Instance.ExpPercent;
            // }
            // else
            // {
            _slider.value = 0;
            // }
            // 게임 진입 연출 도중 GameEnterEvent가 _currentExp를 리셋하기 전이라, 시작 표시는 0으로 고정.
            if (expText != null && UserDataManager.Instance != null)
                expText.text = $"0 / {((int)UserDataManager.Instance.GetNextLevelUpExp()).ToCommaString()}";
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<AddExpEvent>(OnAddExp);
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        // 게임 진입 시 UserDataManager가 _currentExp=0으로 리셋한 뒤 텍스트를 갱신 (로비 경험치 잔존 방지)
        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            _slider.value = 0;
            UpdateExpText();
        }

        private void OnAddExp(AddExpEvent addExpEvent)
        {
            if (UserDataManager.Instance == null) return;

            _slider.DOValue(UserDataManager.Instance.ExpPercent, tweenDuration).SetUpdate(true);
            UpdateExpText();
        }

        private void UpdateExpText()
        {
            if (expText == null || UserDataManager.Instance == null) return;

            expText.text = $"{UserDataManager.Instance.CurrentExp.ToCommaString()} / {((int)UserDataManager.Instance.GetNextLevelUpExp()).ToCommaString()}";
        }
    }
}