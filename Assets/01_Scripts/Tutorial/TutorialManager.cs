using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cumic;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 매니저 - 싱글톤, 코루틴 관리, 외부 API
    /// </summary>
    public class TutorialManager : Singleton<TutorialManager>
    {
        [Header("Data")]
        [SerializeField] private List<TutorialSequenceData> _sequences = new();

        [Header("Views")]
        [SerializeField] private TutorialOverlayView _overlayView;
        [SerializeField] private TutorialSkipPopup _skipPopup;

        private TutorialService _service;
        private TutorialPresenter _presenter;
        private TutorialSaveData _saveData;

        private Coroutine _delayCoroutine;
        private Coroutine _timeoutCoroutine;
        private Button _currentButtonTarget;

        #region Events

        /// <summary> 튜토리얼 시작 시 (sequenceId) </summary>
        public event Action<string> OnTutorialStart;

        /// <summary> 스텝 변경 시 (stepIndex, stepData) </summary>
        public event Action<int, TutorialStepData> OnStepChanged;

        /// <summary> 튜토리얼 완료 시 (sequenceId) </summary>
        public event Action<string> OnTutorialComplete;

        #endregion

        #region Properties

        public bool IsActive => _service?.IsActive ?? false;
        public string CurrentSequenceId => _service?.CurrentSequenceId;

        #endregion

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            _saveData = TutorialSaveData.Load();
            _service = new TutorialService(_sequences, _saveData);
            _presenter = new TutorialPresenter(_service, _overlayView);

            // Service 이벤트 -> Manager 이벤트 전달
            _service.OnTutorialStart += (id) => OnTutorialStart?.Invoke(id);
            _service.OnStepChanged += (index, step) => OnStepChanged?.Invoke(index, step);
            _service.OnTutorialComplete += HandleTutorialComplete;

            // Presenter 이벤트 구독
            _presenter.OnStepPresented += HandleStepPresented;
            _presenter.OnSkipPopupRequested += HandleSkipPopupRequested;

            // 스킵 팝업 이벤트 구독
            if (_skipPopup != null)
            {
                _skipPopup.OnConfirmed += HandleSkipConfirmed;
                _skipPopup.OnCancelled += HandleSkipCancelled;
            }

            // 초기 상태: 오버레이 숨김
            _overlayView.Hide();
            if (_skipPopup != null)
                _skipPopup.Hide();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            StopAllTutorialCoroutines();
            CleanupButtonTarget();

            if (_skipPopup != null)
            {
                _skipPopup.OnConfirmed -= HandleSkipConfirmed;
                _skipPopup.OnCancelled -= HandleSkipCancelled;
            }
        }

        #region Public API

        /// <summary>
        /// 튜토리얼 시퀀스를 시작합니다.
        /// </summary>
        public bool StartTutorial(string sequenceId)
        {
            return _service.TryStartSequence(sequenceId);
        }

        /// <summary>
        /// 시퀀스가 완료되었는지 확인합니다.
        /// </summary>
        public bool IsTutorialCompleted(string sequenceId)
        {
            return _service.IsSequenceCompleted(sequenceId);
        }

        /// <summary>
        /// 모든 튜토리얼 진행 상태를 초기화합니다. (디버그용)
        /// </summary>
        public void ResetAllProgress()
        {
            _saveData.ResetAll();
        }

        #endregion

        #region Step Handling

        private void HandleStepPresented(int index, TutorialStepData step)
        {
            StopAllTutorialCoroutines();
            CleanupButtonTarget();

            // 스킵 버튼 표시 여부
            var sequence = GetCurrentSequence();
            _overlayView.SetSkipButtonVisible(sequence != null && sequence.CanSkip);

            // 딜레이 처리
            if (step.DelayBefore > 0)
            {
                _overlayView.HideStep();
                _delayCoroutine = StartCoroutine(DelayThenShowStep(step));
                return;
            }

            SetupSkipCondition(step);
        }

        private IEnumerator DelayThenShowStep(TutorialStepData step)
        {
            yield return new WaitForSeconds(step.DelayBefore);

            var target = step.ResolveTarget();
            _overlayView.ShowStep(step, target);
            SetupSkipCondition(step);
        }

        private void SetupSkipCondition(TutorialStepData step)
        {
            switch (step.SkipCondition)
            {
                case TutorialSkipCondition.Timeout:
                    _timeoutCoroutine = StartCoroutine(TimeoutAdvance(step.TimeoutDuration));
                    break;

                case TutorialSkipCondition.ButtonClick:
                    SetupButtonTarget(step);
                    break;

                case TutorialSkipCondition.Custom:
                    step.CustomSkipEvent?.AddListener(HandleCustomSkipEvent);
                    break;
            }
        }

        private IEnumerator TimeoutAdvance(float duration)
        {
            yield return new WaitForSeconds(duration);
            _service.AdvanceStep();
        }

        private void SetupButtonTarget(TutorialStepData step)
        {
            var target = step.ResolveTarget();
            if (target == null) return;

            _currentButtonTarget = target.GetComponent<Button>();
            if (_currentButtonTarget != null)
            {
                _currentButtonTarget.onClick.AddListener(HandleButtonTargetClicked);
            }
            else
            {
                Debug.LogWarning($"[Tutorial] ButtonClick 조건이지만 타겟에 Button이 없습니다: {step.Id}");
            }
        }

        private void HandleButtonTargetClicked()
        {
            CleanupButtonTarget();
            _service.AdvanceStep();
        }

        private void HandleCustomSkipEvent()
        {
            var currentStep = _service.CurrentStep;
            currentStep?.CustomSkipEvent?.RemoveListener(HandleCustomSkipEvent);
            _service.AdvanceStep();
        }

        private void CleanupButtonTarget()
        {
            if (_currentButtonTarget != null)
            {
                _currentButtonTarget.onClick.RemoveListener(HandleButtonTargetClicked);
                _currentButtonTarget = null;
            }

            var currentStep = _service.CurrentStep;
            currentStep?.CustomSkipEvent?.RemoveListener(HandleCustomSkipEvent);
        }

        #endregion

        #region Skip Handling

        private void HandleSkipPopupRequested()
        {
            if (_skipPopup != null)
            {
                _skipPopup.ShowPopup();
            }
            else
            {
                _service.SkipCurrentSequence();
            }
        }

        private void HandleSkipConfirmed()
        {
            _service.SkipCurrentSequence();
        }

        private void HandleSkipCancelled()
        {
            // 팝업 닫힘 - 튜토리얼 계속 진행
        }

        #endregion

        #region Completion

        private void HandleTutorialComplete(string sequenceId)
        {
            StopAllTutorialCoroutines();
            CleanupButtonTarget();
            OnTutorialComplete?.Invoke(sequenceId);
        }

        #endregion

        #region Utility

        private TutorialSequenceData GetCurrentSequence()
        {
            if (_service.CurrentSequenceId == null) return null;

            foreach (var seq in _sequences)
            {
                if (seq.SequenceId == _service.CurrentSequenceId)
                    return seq;
            }
            return null;
        }

        private void StopAllTutorialCoroutines()
        {
            if (_delayCoroutine != null)
            {
                StopCoroutine(_delayCoroutine);
                _delayCoroutine = null;
            }
            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }
        }

        #endregion
    }
}
