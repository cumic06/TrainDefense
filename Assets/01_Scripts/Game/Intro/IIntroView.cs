using System;

namespace TrainDefense.Game.Intro
{
    public interface IIntroView
    {
        event Action OnNextRequested;
        event Action OnSkipRequested;

        void Show();
        void Hide();
        void ShowSlide(IntroSlideData slide, int index, int total);
    }
}
