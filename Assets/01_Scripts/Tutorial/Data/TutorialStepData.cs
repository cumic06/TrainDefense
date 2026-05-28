using System;
using TrainDefense.Localize;
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
        [SerializeField] private LocalizeKey _messageKey;

        [Header("연출")]
        [SerializeField] private TutorialArrowDirection _arrowDirection;
        [Tooltip("화살표가 바라보는 방향 (Auto = 배치 방향에 따라 자동)")]
        [SerializeField] private TutorialArrowLookDirection _arrowLookDirection = TutorialArrowLookDirection.Auto;
        [SerializeField] private bool _useDimming = true;
        [SerializeField] private bool _useHighlight = true;
        [SerializeField] private float _delayBefore;
        [SerializeField] private string _sfxKey;

        [Header("메시지 위치 보정")]
        [Tooltip("메시지 말풍선의 위치를 추가로 보정합니다 (캔버스 로컬 좌표 기준)")]
        [SerializeField] private Vector2 _messageOffset;

        [Header("NPC 캐릭터")]
        [SerializeField] private Sprite _npcSprite;
        [Tooltip("캔버스 기준 anchoredPosition. NPC Image RectTransform의 anchor/pivot에 따라 의미가 달라집니다.")]
        [SerializeField] private Vector2 _npcPosition;
        [Tooltip("NPC 이미지를 좌우 반전합니다.")]
        [SerializeField] private bool _npcFlipX;

        [Header("선행 조건")]
        [SerializeField] private string _prerequisiteStepId;

        #region Properties

        public string Id => _id;
        public GameObject TargetObject => _targetObject;
        public string TargetObjectName => _targetObjectName;
        public TutorialSkipCondition SkipCondition => _skipCondition;
        public float TimeoutDuration => _timeoutDuration;
        public UnityEvent CustomSkipEvent => _customSkipEvent;
        public string Message => _messageKey != default && Localization.IsInitialized
            ? Localization.Get(_messageKey)
            : _message;
        public TutorialArrowDirection ArrowDirection => _arrowDirection;
        public TutorialArrowLookDirection ArrowLookDirection => _arrowLookDirection;
        public bool UseDimming => _useDimming;
        public bool UseHighlight => _useHighlight;
        public float DelayBefore => _delayBefore;
        public string SfxKey => _sfxKey;
        public Vector2 MessageOffset => _messageOffset;
        public Sprite NpcSprite => _npcSprite;
        public Vector2 NpcPosition => _npcPosition;
        public bool NpcFlipX => _npcFlipX;
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
