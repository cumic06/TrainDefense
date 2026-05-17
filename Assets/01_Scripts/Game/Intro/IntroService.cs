using System;
using TrainDefense.Game.Tutorial;
using UnityEngine;

namespace TrainDefense.Game.Intro
{
    public class IntroService
    {
        public event Action OnIntroStart;
        public event Action<int, IntroSlideData> OnSlideChanged;
        public event Action OnIntroComplete;

        private readonly TutorialSaveData _saveData;
        private IntroSequenceData _data;
        private int _currentIndex;

        public bool IsActive { get; private set; }
        public int CurrentIndex => _currentIndex;
        public IntroSlideData CurrentSlide => IsActive ? _data.Slides[_currentIndex] : null;
        public int TotalSlides => _data?.Slides.Count ?? 0;
        public IntroSkipConfig SkipConfig => _data?.SkipConfig;

        public IntroService(TutorialSaveData saveData)
        {
            _saveData = saveData;
        }

        public bool TryStart(IntroSequenceData data)
        {
            if (IsActive) return false;
            if (data == null || data.Slides.Count == 0) return false;
            if (_saveData.IsSequenceCompleted(data.SequenceId)) return false;

            _data = data;
            _currentIndex = 0;
            IsActive = true;

            OnIntroStart?.Invoke();
            OnSlideChanged?.Invoke(_currentIndex, CurrentSlide);
            return true;
        }

        public void AdvanceSlide()
        {
            if (!IsActive) return;

            _currentIndex++;
            if (_currentIndex >= _data.Slides.Count)
            {
                Complete();
                return;
            }

            OnSlideChanged?.Invoke(_currentIndex, CurrentSlide);
        }

        public void Skip()
        {
            if (!IsActive) return;
            Complete();
        }

        private void Complete()
        {
            _saveData.MarkSequenceCompleted(_data.SequenceId);
            IsActive = false;
            _data = null;
            _currentIndex = 0;
            OnIntroComplete?.Invoke();
        }
    }
}
