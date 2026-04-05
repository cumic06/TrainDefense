using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 시퀀스 (여러 스텝을 묶는 단위)
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialSequence", menuName = "TrainDefense/Tutorial/Tutorial Sequence")]
    public class TutorialSequenceData : ScriptableObject
    {
        [SerializeField] private string _sequenceId;
        [SerializeField] private string _displayName;
        [SerializeField] private List<TutorialStepData> _steps = new();
        [SerializeField] private bool _canSkip = true;
        [SerializeField] private int _priority;
        [SerializeField] private bool _shouldPauseTime = true;

        public string SequenceId => _sequenceId;
        public string DisplayName => _displayName;
        public IReadOnlyList<TutorialStepData> Steps => _steps;
        public bool CanSkip => _canSkip;
        public int Priority => _priority;
        public bool ShouldPauseTime => _shouldPauseTime;
    }
}
