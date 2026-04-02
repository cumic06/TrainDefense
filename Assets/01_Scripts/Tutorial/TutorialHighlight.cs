using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 하이라이트 - 어두운 오버레이 + 타겟 부분 cutout
    /// </summary>
    public class TutorialHighlight : MonoBehaviour
    {
        [SerializeField] private RectTransform _cutoutRect;
        [SerializeField] private Image _dimmingImage;
        [SerializeField] private float _padding = 20f;
        [SerializeField] private float _transitionDuration = 0.3f;

        private Tween _moveTween;
        private Canvas _rootCanvas;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// UI 타겟에 하이라이트를 표시합니다.
        /// </summary>
        public void SetTarget(RectTransform target)
        {
            if (target == null)
            {
                ClearTarget();
                return;
            }

            gameObject.SetActive(true);

            // 타겟의 월드 좌표 기준 Bounds 계산
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            // 캔버스 로컬 좌표로 변환
            var canvasRect = _rootCanvas.GetComponent<RectTransform>();
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    RectTransformUtility.WorldToScreenPoint(_rootCanvas.worldCamera, corners[i]),
                    _rootCanvas.worldCamera,
                    out var localPoint);

                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            var center = (min + max) * 0.5f;
            var size = (max - min) + Vector2.one * _padding * 2f;

            _moveTween?.Kill();
            _moveTween = DOTween.Sequence()
                .Join(_cutoutRect.DOAnchorPos(center, _transitionDuration))
                .Join(_cutoutRect.DOSizeDelta(size, _transitionDuration))
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        /// <summary>
        /// 월드 좌표 기반 타겟에 하이라이트를 표시합니다.
        /// </summary>
        public void SetTargetWorldPosition(Vector3 worldPosition, Vector2 size)
        {
            gameObject.SetActive(true);

            var canvasRect = _rootCanvas.GetComponent<RectTransform>();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, _rootCanvas.worldCamera, out var localPoint);

            var totalSize = size + Vector2.one * _padding * 2f;

            _moveTween?.Kill();
            _moveTween = DOTween.Sequence()
                .Join(_cutoutRect.DOAnchorPos(localPoint, _transitionDuration))
                .Join(_cutoutRect.DOSizeDelta(totalSize, _transitionDuration))
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        public void ClearTarget()
        {
            _moveTween?.Kill();
            _moveTween = null;
            gameObject.SetActive(false);
        }

        public void SetDimmingAlpha(float alpha)
        {
            if (_dimmingImage != null)
            {
                var color = _dimmingImage.color;
                color.a = alpha;
                _dimmingImage.color = color;
            }
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
        }
    }
}
