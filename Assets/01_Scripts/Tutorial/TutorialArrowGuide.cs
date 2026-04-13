using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 화살표 가이드 - 방향별 회전 + DOTween Yoyo 바운스
    /// </summary>
    public class TutorialArrowGuide : MonoBehaviour
    {
        [SerializeField] private RectTransform _arrowTransform;
        [SerializeField] private Image _arrowImage;
        [SerializeField] private float _bounceDistance = 20f;
        [SerializeField] private float _bounceDuration = 0.6f;
        [SerializeField] private float _offset = 60f;

        private Tween _bounceTween;

        /// <summary>
        /// 화살표를 표시하고 바운스 애니메이션을 시작합니다.
        /// </summary>
        public void Show(TutorialArrowDirection direction, Vector2 targetCanvasPosition,
            TutorialArrowLookDirection lookDirection = TutorialArrowLookDirection.Auto)
        {
            if (direction == TutorialArrowDirection.None)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);

            // 배치 방향에 따른 위치 오프셋
            Vector2 posOffset = GetPositionOffset(direction);
            _arrowTransform.anchoredPosition = targetCanvasPosition + posOffset;

            // 바라보는 방향 회전 적용
            float rotation = lookDirection == TutorialArrowLookDirection.Auto
                ? GetAutoRotation(direction)
                : GetLookRotation(lookDirection);
            _arrowTransform.localEulerAngles = new Vector3(0, 0, rotation);

            // 바운스: 배치 방향 기준으로 바운스
            var bounceEndPos = _arrowTransform.anchoredPosition + GetBounceOffset(direction);

            _bounceTween?.Kill();
            _bounceTween = _arrowTransform.DOAnchorPos(bounceEndPos, _bounceDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private Vector2 GetPositionOffset(TutorialArrowDirection direction)
        {
            float diag = _offset * 0.707f;
            return direction switch
            {
                TutorialArrowDirection.Up => new Vector2(0, _offset),
                TutorialArrowDirection.Down => new Vector2(0, -_offset),
                TutorialArrowDirection.Left => new Vector2(-_offset, 0),
                TutorialArrowDirection.Right => new Vector2(_offset, 0),
                TutorialArrowDirection.UpLeft => new Vector2(-diag, diag),
                TutorialArrowDirection.UpRight => new Vector2(diag, diag),
                TutorialArrowDirection.DownLeft => new Vector2(-diag, -diag),
                TutorialArrowDirection.DownRight => new Vector2(diag, -diag),
                _ => Vector2.zero
            };
        }

        private Vector2 GetBounceOffset(TutorialArrowDirection direction)
        {
            float diag = _bounceDistance * 0.707f;
            return direction switch
            {
                TutorialArrowDirection.Up => new Vector2(0, _bounceDistance),
                TutorialArrowDirection.Down => new Vector2(0, -_bounceDistance),
                TutorialArrowDirection.Left => new Vector2(-_bounceDistance, 0),
                TutorialArrowDirection.Right => new Vector2(_bounceDistance, 0),
                TutorialArrowDirection.UpLeft => new Vector2(-diag, diag),
                TutorialArrowDirection.UpRight => new Vector2(diag, diag),
                TutorialArrowDirection.DownLeft => new Vector2(-diag, -diag),
                TutorialArrowDirection.DownRight => new Vector2(diag, -diag),
                _ => Vector2.zero
            };
        }

        /// <summary>
        /// Auto 모드: 배치 방향의 반대(타겟)를 가리키는 회전값
        /// </summary>
        private float GetAutoRotation(TutorialArrowDirection direction)
        {
            return direction switch
            {
                TutorialArrowDirection.Up => 0f,
                TutorialArrowDirection.Down => 180f,
                TutorialArrowDirection.Left => 90f,
                TutorialArrowDirection.Right => -90f,
                TutorialArrowDirection.UpLeft => 45f,
                TutorialArrowDirection.UpRight => -45f,
                TutorialArrowDirection.DownLeft => 135f,
                TutorialArrowDirection.DownRight => -135f,
                _ => 0f
            };
        }

        /// <summary>
        /// 수동 LookDirection에 따른 회전값 (해당 방향을 가리킴)
        /// </summary>
        private float GetLookRotation(TutorialArrowLookDirection look)
        {
            return look switch
            {
                TutorialArrowLookDirection.Down => 0f,
                TutorialArrowLookDirection.Up => 180f,
                TutorialArrowLookDirection.Right => 90f,
                TutorialArrowLookDirection.Left => -90f,
                TutorialArrowLookDirection.DownRight => 45f,
                TutorialArrowLookDirection.DownLeft => -45f,
                TutorialArrowLookDirection.UpRight => 135f,
                TutorialArrowLookDirection.UpLeft => -135f,
                _ => 0f
            };
        }

        public void Hide()
        {
            _bounceTween?.Kill();
            _bounceTween = null;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _bounceTween?.Kill();
        }
    }
}
