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
        public void Show(TutorialArrowDirection direction, Vector2 targetCanvasPosition)
        {
            if (direction == TutorialArrowDirection.None)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);

            // 방향에 따른 회전 및 위치 오프셋
            float rotation;
            Vector2 posOffset;

            switch (direction)
            {
                case TutorialArrowDirection.Up:
                    rotation = 0f;
                    posOffset = new Vector2(0, _offset);
                    break;
                case TutorialArrowDirection.Down:
                    rotation = 180f;
                    posOffset = new Vector2(0, -_offset);
                    break;
                case TutorialArrowDirection.Left:
                    rotation = 90f;
                    posOffset = new Vector2(-_offset, 0);
                    break;
                case TutorialArrowDirection.Right:
                    rotation = -90f;
                    posOffset = new Vector2(_offset, 0);
                    break;
                default:
                    Hide();
                    return;
            }

            _arrowTransform.localEulerAngles = new Vector3(0, 0, rotation);
            _arrowTransform.anchoredPosition = targetCanvasPosition + posOffset;

            // 바운스 방향 결정
            var bounceEndPos = _arrowTransform.anchoredPosition;
            switch (direction)
            {
                case TutorialArrowDirection.Up:
                    bounceEndPos.y += _bounceDistance;
                    break;
                case TutorialArrowDirection.Down:
                    bounceEndPos.y -= _bounceDistance;
                    break;
                case TutorialArrowDirection.Left:
                    bounceEndPos.x -= _bounceDistance;
                    break;
                case TutorialArrowDirection.Right:
                    bounceEndPos.x += _bounceDistance;
                    break;
            }

            _bounceTween?.Kill();
            _bounceTween = _arrowTransform.DOAnchorPos(bounceEndPos, _bounceDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
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
