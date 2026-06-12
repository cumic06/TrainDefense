using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game
{
    public class TimeManager : Singleton<TimeManager>
    {
        #region Fields

        [SerializeField]
        private float fastForwardScale = 3f;

        [SerializeField]
        [Tooltip("Time.deltaTime 최대값. 백그라운드 복귀 시 프레임 스파이크 방지")]
        private float maxDeltaTime = 0.1f;

        [BoxGroup("GameOverEffect")]
        [SerializeField]
        private float gameOverMinTimeScale = 0.05f;

        #endregion

        #region Variables

        private bool _isPaused;
        private bool _isFastForward;
        private bool _wasPausedBeforeBackground;
        private bool _isInBackground;
        private bool _isGameOverSlowing;

        #endregion

        #region LifeCycle

        protected override void Awake()
        {
            base.Awake();
            Time.maximumDeltaTime = maxDeltaTime;
            TimeManager.Instance?.Pause();
        }

        private void Start()
        {
            _SubscribeEvents();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _isGameOverSlowing = false;

            _UnsubscribeEvents();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // pause(true)가 복귀 없이 연속 통지돼도(오버레이/팝업 등) 최초 진입 시점의 상태만 기억한다
                if (!_isInBackground)
                {
                    _isInBackground = true;
                    _wasPausedBeforeBackground = _isPaused;
                }
                Pause();
            }
            else
            {
                _isInBackground = false;
                Time.maximumDeltaTime = maxDeltaTime;

                if (!_wasPausedBeforeBackground)
                {
                    Resume();
                }
            }
        }

        public void SetFastForward(bool fast)
        {
            if (_isPaused) return;
            _isFastForward = fast;
            Time.timeScale = fast ? fastForwardScale : 1f;
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOverStart);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);
        }

        #endregion

        public void Pause()
        {
            if (_isPaused)
            {
                // _isPaused=true이지만 외부에서 timeScale이 직접 변경된 경우 재동기화
                if (!_isGameOverSlowing && Time.timeScale != 0f)
                    Time.timeScale = 0f;
                return;
            }
            _isPaused = true;

            if (_isGameOverSlowing)
                return;

            Time.timeScale = 0;
            SoundManager.Instance?.SuppressSFX(true);
        }

        public void Resume()
        {
            if (!_isPaused) return;

            if (_isGameOverSlowing)
                return;

            float next = _isFastForward ? fastForwardScale : 1f;
            _isPaused = false;
            Time.timeScale = next;
            SoundManager.Instance?.SuppressSFX(false);
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
