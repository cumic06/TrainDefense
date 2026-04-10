using System;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 프레젠터 - Service와 View를 바인딩합니다.
    /// </summary>
    public sealed class TutorialPresenter : IDisposable
    {
        private readonly ITutorialService _service;
        private readonly ITutorialOverlayView _view;
        private bool _isBound;

        /// <summary>
        /// 스텝이 변경될 때 발생합니다. Manager에서 코루틴 처리에 사용합니다.
        /// </summary>
        public event Action<int, TutorialStepData> OnStepPresented;

        /// <summary>
        /// 스킵 요청이 발생할 때 발생합니다. Manager에서 팝업 표시에 사용합니다.
        /// </summary>
        public event Action OnSkipPopupRequested;

        public TutorialPresenter(ITutorialService service, ITutorialOverlayView view)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _view = view ?? throw new ArgumentNullException(nameof(view));

            Bind();
        }

        public void Dispose()
        {
            if (!_isBound) return;

            _service.OnStepChanged -= HandleStepChanged;
            _service.OnTutorialStart -= HandleTutorialStart;
            _service.OnTutorialComplete -= HandleTutorialComplete;
            _view.OnScreenTapped -= HandleScreenTapped;
            _view.OnSkipRequested -= HandleSkipRequested;
            _isBound = false;
        }

        private void Bind()
        {
            if (_isBound) return;

            _service.OnStepChanged += HandleStepChanged;
            _service.OnTutorialStart += HandleTutorialStart;
            _service.OnTutorialComplete += HandleTutorialComplete;
            _view.OnScreenTapped += HandleScreenTapped;
            _view.OnSkipRequested += HandleSkipRequested;
            _isBound = true;
        }

        private void HandleTutorialStart(string sequenceId)
        {
            _view.Show();
        }

        private void HandleStepChanged(int index, TutorialStepData step)
        {
            var target = step.ResolveTarget();
            _view.ShowStep(step, target);

            // SFX 재생
            if (!string.IsNullOrEmpty(step.SfxKey))
            {
                SoundManager.Instance.PlaySFX(step.SfxKey);
            }

            OnStepPresented?.Invoke(index, step);
        }

        private void HandleTutorialComplete(string sequenceId)
        {
            _view.HideStep();
        }

        private void HandleScreenTapped()
        {
            if (!_service.IsActive) return;

            var currentStep = _service.CurrentStep;
            if (currentStep == null) return;

            if (currentStep.SkipCondition == TutorialSkipCondition.ScreenTap)
            {
                _service.AdvanceStep();
            }
        }

        private void HandleSkipRequested()
        {
            OnSkipPopupRequested?.Invoke();
        }
    }
}
