using System.Collections;
using Cumic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrainDefense.Game.Intro
{
    public class IntroManager : Singleton<IntroManager>
    {
        #region Fields
        [Header("Data")]
        [SerializeField, FormerlySerializedAs("_introData")] private IntroSequenceData introData;

        [Header("Audio")]
        [SerializeField, FormerlySerializedAs("_bgmSource")] private AudioSource bgmSource;
        [SerializeField, FormerlySerializedAs("_voiceSource")] private AudioSource voiceSource;
        #endregion

        #region Variables
        private IntroService _service;
        private IntroPresenter _presenter;
        private IntroInputHandler _inputHandler;
        private Coroutine _autoAdvanceCoroutine;
        #endregion

        #region LifeCycle
        protected override void Awake()
        {
            base.Awake();
            _inputHandler = GetComponent<IntroInputHandler>();

            if (_inputHandler != null)
                _inputHandler.OnFullSkipRequested += RequestFullSkip;
        }

        private void Start()
        {
            StartCoroutine(_TryStartIntroRoutine());
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            _StopAutoAdvance();

            if (_inputHandler != null)
                _inputHandler.OnFullSkipRequested -= RequestFullSkip;
        }
        #endregion

        private IEnumerator _TryStartIntroRoutine()
        {
            yield return new WaitUntil(() => UserDataManager.Instance?.TutorialSaveData != null);

            _EnsureService();
            _BindView();

            if (introData != null)
            {
                bool started = _service?.TryStart(introData) ?? false;
                Debug.Log($"[IntroManager] TryStart 결과={started}, timeScale={Time.timeScale}");
                if (started)
                    TimeManager.Instance?.Pause();
                else
                    TimeManager.Instance?.Resume();
            }
            else
            {
                TimeManager.Instance?.Resume();
            }
        }

        private void _EnsureService()
        {
            if (_service != null) return;

            var saveData = UserDataManager.Instance?.TutorialSaveData;
            if (saveData == null)
            {
                Debug.LogError("[IntroManager] TutorialSaveData를 찾을 수 없습니다.");
                return;
            }

            _service = new IntroService(saveData);
            _service.OnSlideChanged += _HandleSlideChanged;
            _service.OnIntroComplete += _HandleIntroComplete;
        }

        private void _BindView()
        {
            _presenter?.Dispose();

            var view = FindAnyObjectByType<IntroOverlayView>(FindObjectsInactive.Include);
            if (view == null) return;

            view.Hide();

            _presenter = new IntroPresenter(_service, view);
            _presenter.OnNextRequested += AdvanceSlide;
            _presenter.OnSkipRequested += RequestFullSkip;
        }

        public void AdvanceSlide()
        {
            _StopAutoAdvance();
            _service?.AdvanceSlide();
        }

        public void RequestFullSkip()
        {
            _StopAutoAdvance();
            _inputHandler?.Disable();
            _service?.Skip();
        }

        #region Service Event Handlers

        private void _HandleSlideChanged(int index, IntroSlideData slide)
        {
            Debug.Log($"[IntroManager] 슬라이드 [{index}/{(introData?.Slides.Count ?? 0) - 1}] timeScale={Time.timeScale}");
            _StopAutoAdvance();
            _PlaySlideAudio(slide);

            var config = introData?.SkipConfig;
            if (config != null && config.CanSkip)
                _inputHandler?.Enable(config.PcHoldDuration);

            if (slide.IsAutoAdvance)
                _autoAdvanceCoroutine = StartCoroutine(_AutoAdvanceRoutine(slide.AutoAdvanceDuration));
        }

        private void _HandleIntroComplete()
        {
            Debug.Log($"[IntroManager] 인트로 완료 — Resume 직전 timeScale={Time.timeScale}");
            _StopAutoAdvance();
            _inputHandler?.Disable();
            _StopAudio();
            TimeManager.Instance?.Resume();
        }

        #endregion

        #region Coroutine

        private IEnumerator _AutoAdvanceRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            AdvanceSlide();
        }

        private void _StopAutoAdvance()
        {
            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }
        }

        #endregion

        #region Audio

        private void _PlaySlideAudio(IntroSlideData slide)
        {
            if (bgmSource != null && slide.BgmClip != null && bgmSource.clip != slide.BgmClip)
            {
                bgmSource.clip = slide.BgmClip;
                bgmSource.loop = true;
                bgmSource.Play();
            }

            if (voiceSource != null && slide.VoiceClip != null)
            {
                voiceSource.Stop();
                voiceSource.clip = slide.VoiceClip;
                voiceSource.Play();
            }
        }

        private void _StopAudio()
        {
            if (bgmSource != null) bgmSource.Stop();
            if (voiceSource != null) voiceSource.Stop();
        }

        #endregion

        // 인트로 진행 기록을 지우고 현재 씬에서 인트로를 처음부터 다시 재생한다. (옵션 '튜토리얼 다시 보기'에서 호출)
        [Button("인트로 다시 보기")]
        public void Replay()
        {
            UserDataManager.Instance?.TutorialSaveData?.ResetAll();

            if (_service != null)
            {
                _service.OnSlideChanged -= _HandleSlideChanged;
                _service.OnIntroComplete -= _HandleIntroComplete;
                _service = null;
            }

            _presenter?.Dispose();
            _presenter = null;

            _StopAutoAdvance();
            _inputHandler?.Disable();
            _StopAudio();

            StartCoroutine(_TryStartIntroRoutine());

            Debug.Log("[IntroManager] 인트로 진행 상태가 초기화되었습니다.");
        }
    }
}
