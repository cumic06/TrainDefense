using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 메시지 말풍선 - 타이핑 효과 + 자동 위치 배치
    /// </summary>
    public class TutorialMessageBubble : MonoBehaviour
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

        private Tween _typingTween;
        private Tween _fadeTween;
        private string _fullMessage;
        private RectTransform _parentCanvasRect;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _parentCanvasRect = canvas.GetComponent<RectTransform>();
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
            _fullMessage = message;

            bool hasPortrait = npcSprite != null && _npcPortrait != null;

            if (_npcPortrait != null)
            {
                var npcFrame = _npcPortrait.transform.parent.gameObject;
                if (hasPortrait)
                {
                    _npcPortrait.sprite = npcSprite;
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

            _messageText.text = "";
            float typingDuration = message.Length * _typingSpeed;

            _typingTween?.Kill();
            _typingTween = _messageText.DOText(message, typingDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true);

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
            _typingTween?.Kill();
            _typingTween = null;

            if (!string.IsNullOrEmpty(_fullMessage))
                _messageText.text = _fullMessage;
        }

        public void Hide()
        {
            _typingTween?.Kill();
            _typingTween = null;
            _fadeTween?.Kill();
            _fadeTween = null;
            gameObject.SetActive(false);
        }

        private void _ResizeBubble(string message, bool hasPortrait)
        {
            float extraWidth = hasPortrait ? _portraitAreaWidth + _portraitGap : 0f;
            float availTextWidth = _maxWidth - _padding.x - extraWidth;

            _messageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, availTextWidth);
            _messageText.text = message;
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

            _messageText.text = "";
        }

        private void OnDestroy()
        {
            _typingTween?.Kill();
            _fadeTween?.Kill();
        }
    }
}
