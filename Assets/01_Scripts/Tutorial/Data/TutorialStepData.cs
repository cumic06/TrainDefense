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
        [Tooltip("로컬라이즈 키 이름(문자열). 비어 있지 않으면 _messageKey보다 우선한다. LocalizeKey enum은 Import & Bake 때 번호가 다시 매겨져 에셋에 저장된 정수가 다른 키를 가리킬 수 있어, 새로 추가하는 문구는 이름으로 찾는다.")]
        [SerializeField] private string _messageKeyName;

        [Header("연출")]
        [SerializeField] private TutorialArrowDirection _arrowDirection;
        [Tooltip("화살표가 바라보는 방향 (Auto = 배치 방향에 따라 자동)")]
        [SerializeField] private TutorialArrowLookDirection _arrowLookDirection = TutorialArrowLookDirection.Auto;
        [SerializeField] private bool _useDimming = true;
        [SerializeField] private bool _useHighlight = true;
        [Tooltip("이 스텝에서 게임 입력(화면 터치)을 통과시킵니다. 메인 트레인 발사 등 실제 조작을 체험시킬 때 사용. 활성화 시 딤/탭 영역의 레이캐스트 차단이 해제됩니다.")]
        [SerializeField] private bool _allowGameInput;
        [Tooltip("말풍선·화살표가 대상을 가리지 않게 대상 크기만큼 띄워 놓습니다. 켜면 위/아래 중 빈 공간이 넓은 쪽에 화살표 → 말풍선 순으로 쌓이고(화살표 방향은 None만 따르고 나머지는 자동), 화면 어디를 눌러도 다음으로 넘어갑니다. 몬스터처럼 크기가 제각각인 대상에 씁니다.")]
        [SerializeField] private bool _keepTargetClear;
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

        // 실행 중에만 채우는 값(직렬화 안 함). 몬스터처럼 미리 지정할 수 없는 대상, 판마다 달라지는 문구 인자,
        // 이번 판엔 해당 없는 스텝 제외에 쓴다. 시퀀스가 끝나면 ClearRuntimeContext로 비운다.
        [NonSerialized] private GameObject _runtimeTarget;
        [NonSerialized] private object[] _runtimeMessageArgs;
        [NonSerialized] private bool _isRuntimeExcluded;

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
                string raw;

                if (!string.IsNullOrEmpty(_messageKeyName))
                    raw = LocalizeHelper.GetByKey(_messageKeyName, _message);
                else
                    raw = _messageKey != default && Localization.IsInitialized
                        ? Localization.Get(_messageKey)
                        : _message;

                // Intro(IntroSlideData.GetText)와 동일하게 TSV의 리터럴 "\n"을 실제 줄바꿈으로 변환한다.
                // (TextAnimator 타이프라이터 경로에서 \n 이스케이프가 누락될 수 있어 데이터단에서 통일)
                raw = raw?.Replace("\\n", "\n");

                if (raw != null && _runtimeMessageArgs != null && _runtimeMessageArgs.Length > 0)
                    raw = string.Format(raw, _runtimeMessageArgs);

                return raw;
            }
        }
        public TutorialArrowDirection ArrowDirection => _arrowDirection;
        public TutorialArrowLookDirection ArrowLookDirection => _arrowLookDirection;
        public bool UseDimming => _useDimming;
        public bool UseHighlight => _useHighlight;
        public bool AllowGameInput => _allowGameInput;
        public bool KeepTargetClear => _keepTargetClear;
        public float DelayBefore => _delayBefore;
        public string SfxKey => _sfxKey;
        public Vector2 MessageOffset => _messageOffset;
        public Sprite NpcSprite => _npcSprite;
        public Vector2 NpcPosition => _npcPosition;
        public bool NpcFlipX => _npcFlipX;
        public string PrerequisiteStepId => _prerequisiteStepId;
        public bool IsRuntimeExcluded => _isRuntimeExcluded;

        #endregion

        /// <summary>
        /// 실행 중에만 쓰는 대상·문구 인자·제외 여부를 지정합니다.
        /// target은 직렬화된 대상보다 우선하고, messageArgs는 메시지의 {0}, {1}…에 채워집니다.
        /// </summary>
        public void SetRuntimeContext(GameObject target, bool isExcluded = false, params object[] messageArgs)
        {
            _runtimeTarget = target;
            _isRuntimeExcluded = isExcluded;
            _runtimeMessageArgs = messageArgs;
        }

        public void ClearRuntimeContext()
        {
            _runtimeTarget = null;
            _isRuntimeExcluded = false;
            _runtimeMessageArgs = null;
        }

        /// <summary>
        /// 타겟 오브젝트를 반환합니다. 실행 중 지정한 대상 → 직접 참조 → 이름 검색 순으로 찾습니다.
        /// </summary>
        public GameObject ResolveTarget()
        {
            if (_runtimeTarget != null)
                return _runtimeTarget;

            if (_targetObject != null)
                return _targetObject;

            if (!string.IsNullOrEmpty(_targetObjectName))
                return GameObject.Find(_targetObjectName);

            return null;
        }
    }
}
