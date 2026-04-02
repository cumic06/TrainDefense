using System;
using UnityEngine;
using UnityEngine.Events;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 단일 스텝 정의
    /// </summary>
    [Serializable]
    public class TutorialStepData
    {
        [Header("기본 설정")]
        [SerializeField] private string _id;
        [SerializeField] private GameObject _targetObject;
        [SerializeField] private string _targetObjectName;

        [Header("진행 조건")]
        [SerializeField] private TutorialSkipCondition _skipCondition;
        [SerializeField] private float _timeoutDuration = 3f;
        [SerializeField] private UnityEvent _customSkipEvent;

        [Header("메시지")]
        [TextArea]
        [SerializeField] private string _message;

        [Header("연출")]
        [SerializeField] private TutorialArrowDirection _arrowDirection;
        [SerializeField] private bool _useDimming = true;
        [SerializeField] private bool _useHighlight = true;
        [SerializeField] private float _delayBefore;
        [SerializeField] private string _sfxKey;

        [Header("선행 조건")]
        [SerializeField] private string _prerequisiteStepId;

        #region Properties

        public string Id => _id;
        public GameObject TargetObject => _targetObject;
        public string TargetObjectName => _targetObjectName;
        public TutorialSkipCondition SkipCondition => _skipCondition;
        public float TimeoutDuration => _timeoutDuration;
        public UnityEvent CustomSkipEvent => _customSkipEvent;
        public string Message => _message;
        public TutorialArrowDirection ArrowDirection => _arrowDirection;
        public bool UseDimming => _useDimming;
        public bool UseHighlight => _useHighlight;
        public float DelayBefore => _delayBefore;
        public string SfxKey => _sfxKey;
        public string PrerequisiteStepId => _prerequisiteStepId;

        #endregion

        /// <summary>
        /// 타겟 오브젝트를 반환합니다. 직접 참조가 없으면 이름으로 검색합니다.
        /// </summary>
        public GameObject ResolveTarget()
        {
            if (_targetObject != null)
                return _targetObject;

            if (!string.IsNullOrEmpty(_targetObjectName))
                return GameObject.Find(_targetObjectName);

            return null;
        }
    }
}
