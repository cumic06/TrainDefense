using System;

namespace TrainDefense.Game.Intro
{
    public sealed class IntroPresenter : IDisposable
    {
        private readonly IntroService _service;
        private readonly IIntroView _view;
        private bool _isBound;

        public event Action OnNextRequested;
        public event Action OnSkipRequested;

        public IntroPresenter(IntroService service, IIntroView view)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            Bind();
        }

        public void Dispose()
        {
            if (!_isBound) return;

            _service.OnIntroStart -= HandleIntroStart;
            _service.OnSlideChanged -= HandleSlideChanged;
            _service.OnIntroComplete -= HandleIntroComplete;
            _view.OnNextRequested -= HandleNextRequested;
            _view.OnSkipRequested -= HandleSkipRequested;
            _isBound = false;
        }

        private void Bind()
        {
            if (_isBound) return;

            _service.OnIntroStart += HandleIntroStart;
            _service.OnSlideChanged += HandleSlideChanged;
            _service.OnIntroComplete += HandleIntroComplete;
            _view.OnNextRequested += HandleNextRequested;
            _view.OnSkipRequested += HandleSkipRequested;
            _isBound = true;
        }

        private void HandleIntroStart() => _view.Show();
        private void HandleIntroComplete() => _view.Hide();

        private void HandleSlideChanged(int index, IntroSlideData slide)
        {
            _view.ShowSlide(slide, index, _service.TotalSlides);
        }

        private void HandleNextRequested() => OnNextRequested?.Invoke();
        private void HandleSkipRequested() => OnSkipRequested?.Invoke();
    }
}
