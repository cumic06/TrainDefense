using System;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 오버레이 뷰 - Dimming, 화살표, 메시지, 하이라이트 통합 관리
    /// </summary>
    public class TutorialOverlayView : MonoBehaviour, ITutorialOverlayView
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup _dimmingOverlay;
        [SerializeField] private TutorialHighlight _highlight;
        [SerializeField] private TutorialArrowGuide _arrowGuide;
        [SerializeField] private TutorialMessageBubble _messageBubble;

        [Header("Buttons")]
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _screenTapArea;

        [Header("Settings")]
        [SerializeField] private float _dimmingAlpha = 0.7f;

        private Canvas _rootCanvas;
        private RectTransform _canvasRect;

        public event Action OnScreenTapped;
        public event Action OnSkipRequested;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas != null)
                _canvasRect = _rootCanvas.GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _screenTapArea.onClick.AddListener(HandleScreenTap);
            _skipButton.onClick.AddListener(HandleSkipRequest);
        }

        private void OnDisable()
        {
            _screenTapArea.onClick.RemoveListener(HandleScreenTap);
            _skipButton.onClick.RemoveListener(HandleSkipRequest);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowStep(TutorialStepData step, GameObject target)
        {
            Show();

            SetDimming(step.UseDimming);

            // 하이라이트
            if (step.UseHighlight && target != null)
            {
                var targetRect = target.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    SetHighlight(targetRect);
                }
                else
                {
                    _highlight.SetTargetWorldPosition(
                        target.transform.position,
                        new Vector2(100f, 100f));
                }
            }
            else
            {
                _highlight.ClearTarget();
            }

            // 화살표
            if (target != null)
            {
                var targetCanvasPos = GetCanvasPosition(target);
                SetArrow(step.ArrowDirection, targetCanvasPos);
            }
            else
            {
                _arrowGuide.Hide();
            }

            // 메시지
            if (!string.IsNullOrEmpty(step.Message))
            {
                var anchorPos = target != null
                    ? GetCanvasPosition(target)
                    : Vector2.zero;
                SetMessage(step.Message, anchorPos);
            }
            else
            {
                _messageBubble.Hide();
            }
        }

        public void HideStep()
        {
            _highlight.ClearTarget();
            _arrowGuide.Hide();
            _messageBubble.Hide();
            Hide();
        }

        public void SetDimming(bool enabled)
        {
            if (_dimmingOverlay != null)
                _dimmingOverlay.alpha = enabled ? _dimmingAlpha : 0f;
        }

        public void SetHighlight(RectTransform target)
        {
            _highlight.SetTarget(target);
        }

        public void SetArrow(TutorialArrowDirection direction, Vector2 targetPosition)
        {
            _arrowGuide.Show(direction, targetPosition);
        }

        public void SetMessage(string text, Vector2 anchorPosition)
        {
            _messageBubble.Show(text, anchorPosition);
        }

        public void SetSkipButtonVisible(bool visible)
        {
            _skipButton.gameObject.SetActive(visible);
        }

        /// <summary>
        /// GameObject의 위치를 캔버스 로컬 좌표로 변환합니다.
        /// </summary>
        private Vector2 GetCanvasPosition(GameObject target)
        {
            var rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform != null && _canvasRect != null)
            {
                // UI 요소: 월드 좌표 -> 스크린 -> 캔버스 로컬
                var worldPos = rectTransform.position;
                var screenPoint = RectTransformUtility.WorldToScreenPoint(
                    _rootCanvas.worldCamera, worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screenPoint, _rootCanvas.worldCamera, out var localPoint);
                return localPoint;
            }
            else if (_canvasRect != null)
            {
                // 월드 오브젝트: 월드 -> 스크린 -> 캔버스 로컬
                var screenPoint = RectTransformUtility.WorldToScreenPoint(
                    Camera.main, target.transform.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screenPoint, _rootCanvas.worldCamera, out var localPoint);
                return localPoint;
            }

            return Vector2.zero;
        }

        private void HandleScreenTap()
        {
            OnScreenTapped?.Invoke();
        }

        private void HandleSkipRequest()
        {
            OnSkipRequested?.Invoke();
        }
    }
}
