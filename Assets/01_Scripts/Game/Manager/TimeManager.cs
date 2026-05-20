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

        #endregion

        #region Variables

        private bool _isPaused;
        private bool _isFastForward;
        private bool _wasPausedBeforeBackground;

        #endregion

        #region LifeCycle

        protected override void Awake()
        {
            base.Awake();
            Time.maximumDeltaTime = maxDeltaTime;
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
                return;

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

        #endregion

        public void Pause()
        {
            if (_isPaused) return;
            Debug.Log($"[TimeManager] Pause — timeScale {Time.timeScale}→0");
            _isPaused = true;
            Time.timeScale = 0;
            SoundManager.Instance.SuppressSFX(true);
        }

        public void Resume()
        {
            if (!_isPaused) return;
            float next = _isFastForward ? fastForwardScale : 1f;
            Debug.Log($"[TimeManager] Resume — timeScale 0→{next}");
            _isPaused = false;
            Time.timeScale = next;
            SoundManager.Instance.SuppressSFX(false);
        }
    }
}
