using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼/인트로 메시지 말풍선 - TextAnimator 타이핑/효과 + 자동 위치 배치
    /// </summary>
    public class MessageBubble : MonoBehaviour
    {
        [SerializeField] private RectTransform _bubbleRect;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _npcPortrait;
        [SerializeField] private float _typingSpeed = 0.03f;
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _verticalOffset = 80f;
        [SerializeField] private Vector2 _padding = new Vector2(40f, 30f);
        [SerializeField] private float _minWidth = 200f;
        [SerializeField] private float _maxWidth = 600f;
        [SerializeField] private float _portraitAreaWidth = 120f;
        [SerializeField] private float _portraitGap = 8f;

        private Tween _fadeTween;
        private RectTransform _parentCanvasRect;
        private TextAnimator_TMP _textAnimator;
        private TypewriterComponent _typewriter;
        private Sprite _currentNpcSprite;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _parentCanvasRect = canvas.GetComponent<RectTransform>();

            _EnsureTextAnimator();
        }

        private void OnDestroy()
        {
            _fadeTween?.Kill();
        }

        private void Update()
        {
            // animationLoop = Script 이므로 직접 구동한다.
            // unscaledDeltaTime 사용으로 일시정지(timeScale=0) 중에도 타이핑/효과가 재생된다.
            if (_textAnimator != null)
                _textAnimator.Animate(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 메시지 텍스트에 TextAnimator + 타이프라이터를 보장합니다.
        /// MessageBubble 인스턴스가 씬/프리팹에 분산되어 있어 런타임에서 구성합니다.
        /// </summary>
        private void _EnsureTextAnimator()
        {
            if (_messageText == null)
                return;

            var textObject = _messageText.gameObject;

            if (!textObject.TryGetComponent(out _textAnimator))
                _textAnimator = textObject.AddComponent<TextAnimator_TMP>();

            // 일시정지 중에도 재생되도록 스크립트 루프(Update의 Animate)로 직접 구동한다.
            _textAnimator.animationLoop = AnimationLoop.Script;

            if (!textObject.TryGetComponent(out _typewriter))
                _typewriter = textObject.AddComponent<TypewriterComponent>();

            if (_typewriter.localSettings == null)
                _typewriter.localSettings = new UnityTypewriterSettings();
            _typewriter.localSettings.useTypeWriter = true;

            // 런타임 추가라 타이밍 에셋이 없으므로 기본 타이밍을 생성해 글자당 딜레이를 부여한다.
            if (_typewriter.TimingSettings == null)
            {
                var timings = ScriptableObject.CreateInstance<TypingDelaysByCharacter>();
                timings.waitForNormalChars = _typingSpeed;
                _typewriter.TimingSettings = timings;
            }
        }

        /// <summary>
        /// 메시지를 표시합니다. 타이핑 효과와 함께 페이드인됩니다.
        /// </summary>
        public void Show(string message, Vector2 anchorCanvasPosition, Sprite npcSprite = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                Hide();

                return;
            }

            gameObject.SetActive(true);

            // 새 초상화가 들어오면 갱신하고, null이면 마지막으로 지정된 초상화를 유지한다.
            // (튜토리얼 진행 중 스텝마다 NPC가 꺼졌다 켜지는 깜빡임 방지. 인트로처럼 한 번도
            //  초상화를 지정하지 않으면 _currentNpcSprite가 null이라 프레임은 계속 숨겨진다.)
            if (npcSprite != null)
                _currentNpcSprite = npcSprite;

            bool hasPortrait = _npcPortrait != null && _currentNpcSprite != null;

            if (_npcPortrait != null)
            {
                var npcFrame = _npcPortrait.transform.parent.gameObject;
                if (hasPortrait)
                {
                    _npcPortrait.sprite = _currentNpcSprite;
                    npcFrame.SetActive(true);
                }
                else
                {
                    npcFrame.SetActive(false);
                }
            }

            bool placeAbove = anchorCanvasPosition.y < 0;
            float yOffset = placeAbove ? _verticalOffset : -_verticalOffset;
            var bubblePos = new Vector2(anchorCanvasPosition.x, anchorCanvasPosition.y + yOffset);

            if (_parentCanvasRect != null)
            {
                var halfWidth = _parentCanvasRect.rect.width * 0.5f;
                var bubbleHalfWidth = _bubbleRect.rect.width * 0.5f;
                bubblePos.x = Mathf.Clamp(bubblePos.x,
                    -halfWidth + bubbleHalfWidth,
                     halfWidth - bubbleHalfWidth);
            }

            _bubbleRect.anchoredPosition = bubblePos;

            _ResizeBubble(message, hasPortrait);

            _canvasGroup.alpha = 0f;
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        /// <summary>
        /// 타이핑을 즉시 완료합니다.
        /// </summary>
        public void SkipTyping()
        {
            _typewriter?.SkipTypewriter();
        }

        public void Hide()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
            gameObject.SetActive(false);
        }

        private void _ResizeBubble(string message, bool hasPortrait)
        {
            float extraWidth = hasPortrait ? _portraitAreaWidth + _portraitGap : 0f;
            float availTextWidth = _maxWidth - _padding.x - extraWidth;

            _messageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, availTextWidth);

            // TextAnimator 타이프라이터로 텍스트를 설정하고 타이핑을 시작한다.
            // (DOText 점진 설정과 TextAnimator 재파싱이 충돌해 타이핑이 끊기던 문제를 해결)
            _typewriter.ShowText(message);
            _typewriter.StartShowingText();
            _messageText.ForceMeshUpdate();

            float preferredWidth = Mathf.Min(_messageText.preferredWidth, availTextWidth);
            float preferredHeight = _messageText.preferredHeight;

            float bubbleWidth = Mathf.Clamp(preferredWidth + _padding.x + extraWidth, _minWidth, _maxWidth);
            float bubbleHeight = preferredHeight + _padding.y;

            _bubbleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bubbleWidth);
            _bubbleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bubbleHeight);

            if (hasPortrait && _npcPortrait != null)
            {
                var frameRect = _npcPortrait.transform.parent as RectTransform;
                if (frameRect != null)
                {
                    frameRect.anchorMin = new Vector2(0, 0.5f);
                    frameRect.anchorMax = new Vector2(0, 0.5f);
                    frameRect.pivot = new Vector2(0, 0.5f);
                    frameRect.anchoredPosition = new Vector2(_padding.x * 0.5f, 0);
                    float frameHeight = Mathf.Min(bubbleHeight - _padding.y * 0.5f, _portraitAreaWidth);
                    frameRect.sizeDelta = new Vector2(_portraitAreaWidth, frameHeight);
                }
            }

            float textLeft = hasPortrait ? extraWidth + _padding.x * 0.5f : _padding.x * 0.5f;
            _messageText.rectTransform.offsetMin = new Vector2(textLeft, _padding.y * 0.5f);
            _messageText.rectTransform.offsetMax = new Vector2(-_padding.x * 0.5f, -_padding.y * 0.5f);
        }
    }
}
