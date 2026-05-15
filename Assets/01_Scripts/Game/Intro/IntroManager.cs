using System.Collections;
using Cumic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game.Intro
{
    public class IntroManager : Singleton<IntroManager>
    {
        [Header("Data")]
        [SerializeField] private IntroSequenceData _introData;

        [Header("Audio")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _voiceSource;

        private IntroService _service;
        private IntroPresenter _presenter;
        private IntroInputHandler _inputHandler;
        private Coroutine _autoAdvanceCoroutine;

        public bool IsActive => _service?.IsActive ?? false;

        protected override void Awake()
        {
            base.Awake();
            _inputHandler = GetComponent<IntroInputHandler>();
            if (_inputHandler != null)
                _inputHandler.OnFullSkipRequested += RequestFullSkip;
        }

        private void Start()
        {
            StartCoroutine(TryStartIntroRoutine());
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            StopAutoAdvance();

            if (_inputHandler != null)
                _inputHandler.OnFullSkipRequested -= RequestFullSkip;
        }

        // LobbyEnterEvent는 씬 로드 이전 프레임에 발행되므로 직접 코루틴으로 시작
        private IEnumerator TryStartIntroRoutine()
        {
            yield return new WaitUntil(() => UserDataManager.Instance?.TutorialSaveData != null);

            EnsureService();
            BindView();

            if (_introData != null)
                _service?.TryStart(_introData);
        }

        private void EnsureService()
        {
            if (_service != null) return;

            var saveData = UserDataManager.Instance?.TutorialSaveData;
            if (saveData == null)
            {
                Debug.LogError("[IntroManager] TutorialSaveData를 찾을 수 없습니다.");
                return;
            }

            _service = new IntroService(saveData);
            _service.OnSlideChanged += HandleSlideChanged;
            _service.OnIntroComplete += HandleIntroComplete;
        }

        private void BindView()
        {
            _presenter?.Dispose();

            var view = FindAnyObjectByType<IntroOverlayView>(FindObjectsInactive.Include);
            if (view == null) return;

            _presenter = new IntroPresenter(_service, view);
            _presenter.OnNextRequested += AdvanceSlide;
            _presenter.OnSkipRequested += RequestFullSkip;
        }

        public void AdvanceSlide()
        {
            StopAutoAdvance();
            _service?.AdvanceSlide();
        }

        public void RequestFullSkip()
        {
            StopAutoAdvance();
            _inputHandler?.Disable();
            _service?.Skip();
        }

        #region Service Event Handlers

        private void HandleSlideChanged(int index, IntroSlideData slide)
        {
            StopAutoAdvance();
            PlaySlideAudio(slide);

            var config = _introData?.SkipConfig;
            if (config != null && config.CanSkip)
                _inputHandler?.Enable(config.PcHoldDuration);

            if (slide.IsAutoAdvance)
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine(slide.AutoAdvanceDuration));
        }

        private void HandleIntroComplete()
        {
            StopAutoAdvance();
            _inputHandler?.Disable();
            StopAudio();
        }

        #endregion

        #region Coroutine

        private IEnumerator AutoAdvanceRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            AdvanceSlide();
        }

        private void StopAutoAdvance()
        {
            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }
        }

        #endregion

        #region Audio

        private void PlaySlideAudio(IntroSlideData slide)
        {
            if (_bgmSource != null && slide.BgmClip != null && _bgmSource.clip != slide.BgmClip)
            {
                _bgmSource.clip = slide.BgmClip;
                _bgmSource.loop = true;
                _bgmSource.Play();
            }

            if (_voiceSource != null && slide.VoiceClip != null)
            {
                _voiceSource.Stop();
                _voiceSource.clip = slide.VoiceClip;
                _voiceSource.Play();
            }
        }

        private void StopAudio()
        {
            if (_bgmSource != null) _bgmSource.Stop();
            if (_voiceSource != null) _voiceSource.Stop();
        }

        #endregion

        [Button("인트로 초기화 (디버그)")]
        private void ResetIntro()
        {
            UserDataManager.Instance?.TutorialSaveData?.ResetAll();
            Debug.Log("[IntroManager] 인트로 진행 상태가 초기화되었습니다.");
        }
    }
}
