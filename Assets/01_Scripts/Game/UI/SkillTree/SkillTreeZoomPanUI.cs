using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// 스킬트리 판의 줌 — 핀치(터치 2개)와 마우스 휠로 트리 내용(Content)을 전체가 판에 들어오는 배율~MaxScale배 사이에서 조절한다.
    /// 축소는 트리를 훑어보는 용도라 칸이 터치 크기보다 작아져도 된다 (11-03 사용자 결정).
    /// 이동(드래그)은 같은 오브젝트의 ScrollRect가 맡는다 (Clamped라 내용이 판 밖으로 안 나간다). 핀치 중에는 ScrollRect를 꺼서 두 손가락 드래그와 섞이지 않게 한다.
    /// 줌은 손가락·포인터 아래 지점이 제자리에 있도록 Content 위치를 같이 옮긴다.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class SkillTreeZoomPanUI : MonoBehaviour, IScrollHandler
    {
        private const float MaxScale = 2.2f;
        private const float WheelZoomStep = 0.1f;     // 휠 한 칸당 배율 변화 (에디터·PC 확인용)
        private const float PinchSensitivity = 0.005f; // 손가락 간격 1px 변화당 배율 변화 (1080p 기준 200px 벌리면 ×2)

        #region Fields
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        #endregion

        private ScrollRect _scrollRect;
        private bool _isPinching;
        private float _previousPinchDistance;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        private void Update()
        {
            if (Input.touchCount < 2)
            {
                if (_isPinching) _EndPinch();

                return;
            }

            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            float distance = Vector2.Distance(first.position, second.position);
            Vector2 center = (first.position + second.position) * 0.5f;

            if (!_isPinching)
            {
                _isPinching = true;
                _previousPinchDistance = distance;
                // ScrollRect가 첫 손가락 드래그를 잡고 있으면 핀치 동안 내용이 튄다
                _scrollRect.StopMovement();
                _scrollRect.enabled = false;

                return;
            }

            float scaleDelta = (distance - _previousPinchDistance) * PinchSensitivity;
            _previousPinchDistance = distance;
            _ZoomAround(center, scaleDelta);
        }

        private void OnDisable()
        {
            if (_isPinching) _EndPinch();
        }

        private void _EndPinch()
        {
            _isPinching = false;
            _scrollRect.enabled = true;
        }

        public void OnScroll(PointerEventData eventData)
        {
            _ZoomAround(eventData.position, eventData.scrollDelta.y * WheelZoomStep);
        }

        // 화면 좌표 screenPoint 아래의 내용이 줌 뒤에도 같은 화면 위치에 오도록 Content를 옮긴다.
        private void _ZoomAround(Vector2 screenPoint, float scaleDelta)
        {
            if (content == null || viewport == null || Mathf.Approximately(scaleDelta, 0f)) return;

            float currentScale = content.localScale.x;
            float nextScale = Mathf.Clamp(currentScale + scaleDelta, _GetFitScale(), MaxScale);
            if (Mathf.Approximately(nextScale, currentScale)) return;

            var canvas = GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, eventCamera, out Vector2 pointInViewport);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPoint, eventCamera, out Vector2 pointInContent);

            content.localScale = new Vector3(nextScale, nextScale, 1f);

            // Content 피벗·앵커는 뷰포트 가운데(0.5, 0.5). 뷰포트 로컬 좌표는 뷰포트 피벗 기준이라 가운데 기준으로 옮긴 뒤
            // "앵커 위치 + Content 로컬 점 × 배율 = 뷰포트 점"에서 앵커 위치를 역산한다
            Vector2 pointFromViewportCenter = pointInViewport - (new Vector2(0.5f, 0.5f) - viewport.pivot) * viewport.rect.size;
            // 핀치 중에는 ScrollRect가 꺼져 있어 위치 제한을 대신 걸어 준다. 내용이 판보다 작은 축은 가운데에 둔다
            Vector2 movableRange = Vector2.Max(content.rect.size * nextScale - viewport.rect.size, Vector2.zero) * 0.5f;
            Vector2 nextPosition = pointFromViewportCenter - pointInContent * nextScale;
            content.anchoredPosition = new Vector2(
                Mathf.Clamp(nextPosition.x, -movableRange.x, movableRange.x),
                Mathf.Clamp(nextPosition.y, -movableRange.y, movableRange.y));

            _scrollRect.StopMovement();
        }

        // 트리 전체가 판 안에 들어오는 배율. 판보다 작은 트리는 1배로 둔다
        private float _GetFitScale()
        {
            Vector2 contentSize = content.rect.size;
            Vector2 viewportSize = viewport.rect.size;

            return Mathf.Min(1f, viewportSize.x / contentSize.x, viewportSize.y / contentSize.y);
        }
    }
}
