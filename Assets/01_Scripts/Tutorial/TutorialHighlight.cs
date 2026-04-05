using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 하이라이트 - 4패널 방식으로 cutout 구멍 생성
    /// Top/Bottom/Left/Right 4개 Image가 cutout 주변을 어둡게 채웁니다.
    /// </summary>
    public class TutorialHighlight : MonoBehaviour
    {
        [SerializeField] private RectTransform _cutoutRect;
        [SerializeField] private Image _dimmingImage;
        [SerializeField] private float _padding = 20f;
        [SerializeField] private float _transitionDuration = 0.3f;

        private Tween _moveTween;
        private Canvas _rootCanvas;
        private bool _hasTarget;

        // 4패널 dimming
        private Image[] _panels;
        private RectTransform _panelParent;

        // 외부 접근용
        public RectTransform CutoutRect => _cutoutRect;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
            CreatePanels();
        }

        private void CreatePanels()
        {
            if (_dimmingImage == null) return;

            var color = _dimmingImage.color;
            _panelParent = _dimmingImage.rectTransform;

            // 원본 이미지 비활성화 (4패널로 대체)
            _dimmingImage.enabled = false;

            _panels = new Image[4];
            string[] names = { "Top", "Bottom", "Left", "Right" };

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"DimmingPanel_{names[i]}",
                    typeof(RectTransform), typeof(Image));

                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(_panelParent, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                var img = go.GetComponent<Image>();
                img.color = color;
                img.raycastTarget = false;

                _panels[i] = img;
            }

            // 초기 상태: 전체 덮기 (cutout 없음)
            FillAll();
        }

        private void LateUpdate()
        {
            if (!_hasTarget || _panels == null) return;
            UpdatePanels();
        }

        /// <summary>
        /// cutout 주변 4패널 위치/크기 갱신 (매 프레임)
        /// </summary>
        private void UpdatePanels()
        {
            if (_panelParent == null || _cutoutRect == null) return;

            // cutout의 월드 좌표를 panelParent 로컬 좌표로 변환
            var corners = new Vector3[4];
            _cutoutRect.GetWorldCorners(corners);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                Vector2 localPoint = _panelParent.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            Rect parentRect = _panelParent.rect;
            float pL = parentRect.xMin;
            float pR = parentRect.xMax;
            float pB = parentRect.yMin;
            float pT = parentRect.yMax;

            float cutL = min.x;
            float cutR = max.x;
            float cutB = min.y;
            float cutT = max.y;
            float cutCenterY = (cutB + cutT) * 0.5f;
            float cutH = cutT - cutB;
            float fullW = pR - pL;

            // ┌──────────────────────┐
            // │       Top Panel       │
            // ├────┬────────────┬─────┤
            // │Left│  (cutout)  │Right│
            // ├────┴────────────┴─────┤
            // │      Bottom Panel     │
            // └──────────────────────┘

            // Top: 전체 폭, cutout 위쪽부터 부모 상단까지
            float topH = Mathf.Max(0, pT - cutT);
            SetPanelRect(_panels[0].rectTransform,
                0, cutT + topH * 0.5f, fullW, topH);

            // Bottom: 전체 폭, 부모 하단부터 cutout 아래쪽까지
            float botH = Mathf.Max(0, cutB - pB);
            SetPanelRect(_panels[1].rectTransform,
                0, pB + botH * 0.5f, fullW, botH);

            // Left: cutout 높이만큼, 부모 좌측부터 cutout 좌측까지
            float leftW = Mathf.Max(0, cutL - pL);
            SetPanelRect(_panels[2].rectTransform,
                pL + leftW * 0.5f, cutCenterY, leftW, cutH);

            // Right: cutout 높이만큼, cutout 우측부터 부모 우측까지
            float rightW = Mathf.Max(0, pR - cutR);
            SetPanelRect(_panels[3].rectTransform,
                cutR + rightW * 0.5f, cutCenterY, rightW, cutH);
        }

        private void SetPanelRect(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>
        /// 패널 하나로 전체 영역을 덮습니다 (cutout 없음).
        /// </summary>
        private void FillAll()
        {
            if (_panels == null || _panelParent == null) return;

            Rect parentRect = _panelParent.rect;

            SetPanelRect(_panels[0].rectTransform,
                0, 0, parentRect.width, parentRect.height);

            for (int i = 1; i < 4; i++)
                SetPanelRect(_panels[i].rectTransform, 0, 0, 0, 0);
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
            _hasTarget = true;

            // 타겟의 월드 좌표 → 캔버스 로컬 좌표
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _rootCanvas.worldCamera;

            var canvasRect = _rootCanvas.GetComponent<RectTransform>();
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPoint, cam, out var localPoint);

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
            _hasTarget = true;

            Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _rootCanvas.worldCamera;

            var canvasRect = _rootCanvas.GetComponent<RectTransform>();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, cam, out var localPoint);

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
            _hasTarget = false;

            FillAll();
            gameObject.SetActive(false);
        }

        public void SetDimmingAlpha(float alpha)
        {
            if (_panels == null) return;

            foreach (var panel in _panels)
            {
                if (panel == null) continue;
                var color = panel.color;
                color.a = alpha;
                panel.color = color;
            }
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
        }
    }
}
