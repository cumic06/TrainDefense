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
        private TutorialRaycastBlocker _screenTapBlocker;

        public event Action OnScreenTapped;
        public event Action OnSkipRequested;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas != null)
                _canvasRect = _rootCanvas.GetComponent<RectTransform>();

            SetupRaycastBlocker();
        }

        /// <summary>
        /// ScreenTapArea에 레이캐스트 블로커를 설정하여
        /// cutout 영역 외부의 터치를 차단합니다.
        /// </summary>
        private void SetupRaycastBlocker()
        {
            if (_screenTapArea == null) return;

            var tapImage = _screenTapArea.GetComponent<Image>();
            if (tapImage != null)
            {
                tapImage.raycastTarget = true;
            }

            _screenTapBlocker = _screenTapArea.gameObject.GetComponent<TutorialRaycastBlocker>();
            if (_screenTapBlocker == null)
            {
                _screenTapBlocker = _screenTapArea.gameObject.AddComponent<TutorialRaycastBlocker>();
            }

            // 초기 상태: cutout 없이 전체 차단
            _screenTapBlocker.SetBlockAll(true);
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
                    // cutout 영역 내부는 터치 통과
                    _screenTapBlocker?.SetCutoutRect(_highlight.CutoutRect);
                }
                else
                {
                    _highlight.SetTargetWorldPosition(
                        target.transform.position,
                        new Vector2(100f, 100f));
                    _screenTapBlocker?.SetCutoutRect(_highlight.CutoutRect);
                }
            }
            else
            {
                _highlight.ClearTarget();
                // cutout 없으면 전체 차단
                _screenTapBlocker?.SetBlockAll(true);
            }

            // 화살표
            if (target != null)
            {
                var targetCanvasPos = GetCanvasPosition(target);
                SetArrow(step.ArrowDirection, targetCanvasPos, step.ArrowLookDirection);
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
                anchorPos += step.MessageOffset;
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
            _screenTapBlocker?.SetBlockAll(true);
            _arrowGuide.Hide();
            _messageBubble.Hide();
            Hide();
        }

        public void SetDimming(bool enabled)
        {
            if (_dimmingOverlay != null)
            {
                _dimmingOverlay.alpha = enabled ? _dimmingAlpha : 0f;
                // 딤핑이 꺼져도 레이캐스트는 차단 유지
                _dimmingOverlay.blocksRaycasts = true;
            }
        }

        public void SetHighlight(RectTransform target)
        {
            _highlight.SetTarget(target);
        }

        public void SetArrow(TutorialArrowDirection direction, Vector2 targetPosition,
            TutorialArrowLookDirection lookDirection = TutorialArrowLookDirection.Auto)
        {
            _arrowGuide.Show(direction, targetPosition, lookDirection);
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
            Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _rootCanvas.worldCamera;

            if (rectTransform != null && _canvasRect != null)
            {
                // UI 요소: 월드 좌표 -> 스크린 -> 캔버스 로컬
                var worldPos = rectTransform.position;
                var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screenPoint, cam, out var localPoint);
                return localPoint;
            }
            else if (_canvasRect != null)
            {
                // 월드 오브젝트: 월드 -> 스크린 -> 캔버스 로컬
                var screenPoint = RectTransformUtility.WorldToScreenPoint(
                    Camera.main, target.transform.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screenPoint, cam, out var localPoint);
                return localPoint;
            }

            return Vector2.zero;
        }

        private void HandleScreenTap()
        {
            // ScreenTapArea의 onClick은 cutout 외부를 탭했을 때만 호출됨
            // (TutorialRaycastBlocker가 cutout 내부는 통과시킴)
            OnScreenTapped?.Invoke();
        }

        private void HandleSkipRequest()
        {
            OnSkipRequested?.Invoke();
        }
    }
}
