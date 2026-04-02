using System;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 서비스 인터페이스
    /// </summary>
    public interface ITutorialService
    {
        event Action<string> OnTutorialStart;
        event Action<int, TutorialStepData> OnStepChanged;
        event Action<string> OnTutorialComplete;

        bool IsActive { get; }
        string CurrentSequenceId { get; }
        int CurrentStepIndex { get; }
        TutorialStepData CurrentStep { get; }
        int TotalSteps { get; }

        bool TryStartSequence(string sequenceId);
        void AdvanceStep();
        void SkipCurrentSequence();
        bool IsSequenceCompleted(string sequenceId);
    }
}
