using UnityEngine;
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
        [SerializeField] private float _typingSpeed = 0.03f;
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _verticalOffset = 80f;

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
        public void Show(string message, Vector2 anchorCanvasPosition)
        {
            if (string.IsNullOrEmpty(message))
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            _fullMessage = message;

            // 화면 중앙 기준으로 위/아래 배치 결정
            bool placeAbove = anchorCanvasPosition.y < 0;
            float yOffset = placeAbove ? _verticalOffset : -_verticalOffset;
            var bubblePos = new Vector2(anchorCanvasPosition.x, anchorCanvasPosition.y + yOffset);

            // 화면 밖으로 나가지 않도록 X 클램핑
            if (_parentCanvasRect != null)
            {
                var halfWidth = _parentCanvasRect.rect.width * 0.5f;
                var bubbleHalfWidth = _bubbleRect.rect.width * 0.5f;
                bubblePos.x = Mathf.Clamp(bubblePos.x,
                    -halfWidth + bubbleHalfWidth,
                     halfWidth - bubbleHalfWidth);
            }

            _bubbleRect.anchoredPosition = bubblePos;

            // 타이핑 효과
            _messageText.text = "";
            float typingDuration = message.Length * _typingSpeed;

            _typingTween?.Kill();
            _typingTween = _messageText.DOText(message, typingDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true);

            // 페이드인
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

        private void OnDestroy()
        {
            _typingTween?.Kill();
            _fadeTween?.Kill();
        }
    }
}
