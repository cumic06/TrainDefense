using System;
using UnityEngine;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 오버레이 뷰 인터페이스
    /// </summary>
    public interface ITutorialOverlayView
    {
        event Action OnScreenTapped;
        event Action OnSkipRequested;

        void Show();
        void Hide();
        void ShowStep(TutorialStepData step, GameObject target);
        void HideStep();
        void SetDimming(bool enabled);
        void SetHighlight(RectTransform target);
        void SetArrow(TutorialArrowDirection direction, Vector2 targetPosition,
            TutorialArrowLookDirection lookDirection = TutorialArrowLookDirection.Auto);
        void SetMessage(string text, Vector2 anchorPosition);
        void SetSkipButtonVisible(bool visible);
    }
}
