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

        private bool _isPaused;
        private bool _isFastForward;
        private bool _wasPausedBeforeBackground;

        protected override void Awake()
        {
            base.Awake();
            Time.maximumDeltaTime = maxDeltaTime;
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
            // Keep gameplay frozen until the intro/timeline explicitly starts the run.
            Pause();
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Subscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Subscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);

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
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);

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
            Debug.Log($"[TimeManager] Pause — timeScale {Time.timeScale}→0");
            _isPaused = true;
            Time.timeScale = 0;
        }

        public void Resume()
        {
            float next = _isFastForward ? fastForwardScale : 1f;
            Debug.Log($"[TimeManager] Resume — timeScale {Time.timeScale}→{next}");
            _isPaused = false;
            Time.timeScale = next;
        }
    }
}