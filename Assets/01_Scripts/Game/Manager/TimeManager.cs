using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Tutorial;

namespace TrainDefense.Game
{
    public class TimeManager : Singleton<TimeManager>
    {
        [SerializeField]
        private float fastForwardScale = 3f;

        [SerializeField]
        [Tooltip("Time.deltaTime 최대값. 백그라운드 복귀 시 프레임 스파이크 방지")]
        private float maxDeltaTime = 0.1f;

        [BoxGroup("GameOverEffect")]
        [SerializeField]
        private float gameOverMinTimeScale = 0.05f;

        private bool _isPaused;
        private bool _isFastForward;
        private bool _wasPausedBeforeBackground;
        private bool _isGameOverSlowing;

        protected override void Awake()
        {
            base.Awake();
            Time.maximumDeltaTime = maxDeltaTime;
            // 씬 로드 직후 첫 프레임부터 정지 상태를 보장 (인트로/타임라인 연출 중 시간 진행 방지).
            // dontDestroyOnLoad 싱글톤이라 중복 인스턴스의 Awake가 Instance에 위임되어도 정상 동작.
            TimeManager.Instance?.Pause();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _wasPausedBeforeBackground = _isPaused;
                Pause();
            }
            else
            {
                Time.maximumDeltaTime = maxDeltaTime;
                if (!_wasPausedBeforeBackground)
                {
                    Resume();
                }
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (_isPaused)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _isFastForward = true;
                Time.timeScale = fastForwardScale;
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                _isFastForward = false;
                Time.timeScale = 1f;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                GameEventSystem.Publish(new LevelUpEvent(1));
            }
#endif
        }

        private void Start()
        {
            // Pause는 Awake에서 미리 수행. Start에서는 이벤트 구독만 처리.
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Subscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Subscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOverStart);

            // 튜토리얼 시작/완료 시 시간 제어 (시퀀스 레벨에서만)
            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager != null)
            {
                tutorialManager.OnTutorialStart += OnTutorialStart;
                tutorialManager.OnTutorialComplete += OnTutorialComplete;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _isGameOverSlowing = false;

            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);

            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager != null)
            {
                tutorialManager.OnTutorialStart -= OnTutorialStart;
                tutorialManager.OnTutorialComplete -= OnTutorialComplete;
            }
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            Pause();
        }

        private void OnEngageReady(EngageReadyEvent engageReadyEvent)
        {
            Pause();
        }

        private void OnEngageStart(EngageStartEvent engageStartEvent)
        {
            Resume();
        }

        private void OnStageEnd(StageEndEvent stageEndEvent)
        {
            Pause();
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            Pause();
        }

        private void OnLevelUp(LevelUpEvent levelUpEvent)
        {
            Pause();
        }

        private void OnInspectionStart(InspectionStartEvent inspectionStartEvent)
        {
            Pause();
        }

        private void OnTriChoiceSelect(TriChoiceSelectEvent triChoiceSelectEvent)
        {
            Resume();
        }

        private void OnTutorialStart(string sequenceId)
        {
            Debug.Log($"[TimeManager] Tutorial started: {sequenceId}");

            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager != null && tutorialManager.CurrentShouldPauseTime)
            {
                Pause();
            }
        }

        private void OnTutorialComplete(string sequenceId)
        {
            Debug.Log($"[TimeManager] Tutorial completed: {sequenceId}");
            Resume();
        }

        public void Pause()
        {
            _isPaused = true;

            if (_isGameOverSlowing)
                return;

            Time.timeScale = 0;
            SoundManager.Instance.SuppressSFX(true);
        }

        public void Resume()
        {
            if (_isGameOverSlowing)
                return;

            float next = _isFastForward ? fastForwardScale : 1f;
            _isPaused = false;
            Time.timeScale = next;
            SoundManager.Instance.SuppressSFX(false);
        }

        private void _OnGameOverStart(GameOverStartEvent e)
        {
            StartCoroutine(_GameOverSlowCoroutine(e.Duration));
        }

        private IEnumerator _GameOverSlowCoroutine(float duration)
        {
            _isGameOverSlowing = true;
            float elapsedTime = 0f;
            float startTimeScale = Time.timeScale;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                Time.timeScale = Mathf.Lerp(startTimeScale, gameOverMinTimeScale, t);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;

                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _isGameOverSlowing = false;

            GameEventSystem.Publish(new GameEndEvent(false));
        }
    }
}