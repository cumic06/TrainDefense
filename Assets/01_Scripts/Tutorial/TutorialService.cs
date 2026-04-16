using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 진행 로직 (Pure C#, MonoBehaviour 비의존)
    /// </summary>
    public class TutorialService : ITutorialService
    {
        public event Action<string> OnTutorialStart;
        public event Action<int, TutorialStepData> OnStepChanged;
        public event Action<string> OnTutorialComplete;

        private readonly Dictionary<string, TutorialSequenceData> _sequenceMap = new();
        private readonly TutorialSaveData _saveData;

        private TutorialSequenceData _currentSequence;
        private int _currentStepIndex;
        private bool _isActive;

        #region Properties

        public bool IsActive => _isActive;
        public string CurrentSequenceId => _currentSequence?.SequenceId;
        public int CurrentStepIndex => _currentStepIndex;
        public TutorialStepData CurrentStep =>
            _isActive && _currentStepIndex < _currentSequence.Steps.Count
                ? _currentSequence.Steps[_currentStepIndex]
                : null;
        public int TotalSteps => _currentSequence?.Steps.Count ?? 0;

        #endregion

        public TutorialService(IReadOnlyList<TutorialSequenceData> sequences, TutorialSaveData saveData)
        {
            _saveData = saveData;

            foreach (var seq in sequences)
            {
                if (seq == null) continue;
                _sequenceMap[seq.SequenceId] = seq;
            }
        }

        public bool TryStartSequence(string sequenceId)
        {
            if (_isActive)
            {
                Debug.LogWarning($"[Tutorial] 이미 진행 중인 튜토리얼이 있습니다: {CurrentSequenceId}");
                return false;
            }

            if (_saveData.IsSequenceCompleted(sequenceId))
            {
                return false;
            }

            if (!_sequenceMap.TryGetValue(sequenceId, out var sequence))
            {
                Debug.LogError($"[Tutorial] 시퀀스를 찾을 수 없습니다: {sequenceId}");
                return false;
            }

            if (sequence.Steps.Count == 0)
            {
                Debug.LogWarning($"[Tutorial] 빈 시퀀스: {sequenceId}");
                return false;
            }

            _currentSequence = sequence;
            _currentStepIndex = 0;
            _isActive = true;

            OnTutorialStart?.Invoke(sequenceId);
            OnStepChanged?.Invoke(_currentStepIndex, CurrentStep);

            return true;
        }

        public void AdvanceStep()
        {
            if (!_isActive) return;

            var currentStep = CurrentStep;
            if (currentStep != null && !string.IsNullOrEmpty(currentStep.Id))
            {
                _saveData.MarkStepCompleted(currentStep.Id);
            }

            _currentStepIndex++;

            if (_currentStepIndex >= _currentSequence.Steps.Count)
            {
                CompleteCurrentSequence();
                return;
            }

            // 선행 조건 확인 - 충족되지 않으면 건너뛰기
            var nextStep = CurrentStep;
            if (nextStep != null && !string.IsNullOrEmpty(nextStep.PrerequisiteStepId))
            {
                if (!_saveData.IsStepCompleted(nextStep.PrerequisiteStepId))
                {
                    Debug.LogWarning($"[Tutorial] 선행 조건 미충족, 스킵: {nextStep.Id}");
                    AdvanceStep();
                    return;
                }
            }

            OnStepChanged?.Invoke(_currentStepIndex, CurrentStep);
        }

        public void SkipCurrentSequence()
        {
            if (!_isActive) return;

            for (int i = _currentStepIndex; i < _currentSequence.Steps.Count; i++)
            {
                var step = _currentSequence.Steps[i];
                if (!string.IsNullOrEmpty(step.Id))
                {
                    _saveData.MarkStepCompleted(step.Id);
                }
            }

            CompleteCurrentSequence();
        }

        public bool IsSequenceCompleted(string sequenceId)
        {
            return _saveData.IsSequenceCompleted(sequenceId);
        }

        private void CompleteCurrentSequence()
        {
            var sequenceId = _currentSequence.SequenceId;
            _saveData.MarkSequenceCompleted(sequenceId);
            _isActive = false;
            _currentSequence = null;
            _currentStepIndex = 0;

            OnTutorialComplete?.Invoke(sequenceId);
        }
    }
}
