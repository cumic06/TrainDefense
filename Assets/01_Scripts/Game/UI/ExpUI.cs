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
        [SerializeField]
        private TMP_Text levelText;
        #endregion

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        private void Start()
        {
            GameEventSystem.Subscribe<AddExpEvent>(OnAddExp);
            GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);
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
            // 게임 진입 연출 도중 GameEnterEvent가 _currentExp/_currentLevel을 리셋하기 전이라, 시작 표시는
            // 분자 0 + 분모는 레벨 1 기준으로 고정한다. GetNextLevelUpExp()(=_currentLevel 의존)를 쓰면
            // 구독 순서에 따라 로비 시뮬레이션에서 누적된 레벨(2 이상)의 필요량(약 879)이 잠깐 보인다.
            if (expText != null && UserDataManager.Instance != null)
                expText.text = $"0 / {((int)UserDataManager.Instance.GetNextLevelUpExp(1)).ToCommaString()}";
            if (levelText != null)
                levelText.text = "Lv 1";
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<AddExpEvent>(OnAddExp);
            GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        // 게임 진입 시 0부터 시작. UserDataManager의 _currentExp=0 리셋(OnGameEnter)이 이 콜백보다
        // 늦게 실행될 수 있어, CurrentExp를 읽으면 로비 시뮬레이션에서 누적된 값이 잠깐 표시된다.
        // 분자는 0으로 고정(Setup 재사용)해 콜백 순서와 무관하게 0부터 보이도록 한다.
        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            Setup();
        }

        private void OnAddExp(AddExpEvent addExpEvent)
        {
            if (UserDataManager.Instance == null) return;

            _slider.DOValue(UserDataManager.Instance.ExpPercent, tweenDuration).SetUpdate(true);
            UpdateExpText();
        }

        // 레벨 표시는 AddExpEvent가 아니라 LevelUpEvent로 갱신한다. LevelUpEvent는 UserDataManager가
        // 레벨을 올린 뒤 발행하므로 구독 순서와 무관하게 CurrentLevel이 항상 갱신 완료 상태다.
        private void OnLevelUp(LevelUpEvent levelUpEvent)
        {
            if (levelText == null || UserDataManager.Instance == null) return;

            levelText.text = $"Lv {UserDataManager.Instance.CurrentLevel}";
        }

        private void UpdateExpText()
        {
            if (expText == null || UserDataManager.Instance == null) return;

            expText.text = $"{UserDataManager.Instance.CurrentExp.ToCommaString()} / {((int)UserDataManager.Instance.GetNextLevelUpExp()).ToCommaString()}";
        }
    }
}