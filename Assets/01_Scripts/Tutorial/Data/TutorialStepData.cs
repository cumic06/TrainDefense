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
        [Tooltip("이 스텝에서 게임 입력(화면 터치)을 통과시킵니다. 메인 트레인 발사 등 실제 조작을 체험시킬 때 사용. 활성화 시 딤/탭 영역의 레이캐스트 차단이 해제됩니다.")]
        [SerializeField] private bool _allowGameInput;
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
        public string Message
        {
            get
            {
                string raw = _messageKey != default && Localization.IsInitialized
                    ? Localization.Get(_messageKey)
                    : _message;

                // Intro(IntroSlideData.GetText)와 동일하게 TSV의 리터럴 "\n"을 실제 줄바꿈으로 변환한다.
                // (TextAnimator 타이프라이터 경로에서 \n 이스케이프가 누락될 수 있어 데이터단에서 통일)
                // 추가로 한글 단어 중간 줄바꿈을 방지(주황→주/황, 줍니다.→줍니/다. 방지).
                return LocalizeHelper.ProtectWordBreak(raw?.Replace("\\n", "\n"));
            }
        }
        public TutorialArrowDirection ArrowDirection => _arrowDirection;
        public TutorialArrowLookDirection ArrowLookDirection => _arrowLookDirection;
        public bool UseDimming => _useDimming;
        public bool UseHighlight => _useHighlight;
        public bool AllowGameInput => _allowGameInput;
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
