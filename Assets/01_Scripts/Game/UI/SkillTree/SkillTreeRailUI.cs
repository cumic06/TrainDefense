using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// 스킬트리 선로 세그먼트 — 선행 노드에서 다음 노드로 이어지는 간선 하나 (시그니처 "점등되는 선로").
    /// 실제 기찻길 스프라이트(Rail, 가로 이미지)를 Tiled로 깔고 자식을 +90° 회전시켜 선로 방향으로 눕힌다.
    /// 미점등 기찻길(baseImage) 위에 accent 점등 기찻길(litImage)이 겹쳐 있고,
    /// 도착 노드를 처음 습득하는 순간 아래→위로 길이가 자라며 점등된다 (스윕 320ms, SetUpdate(true)).
    /// 도착 노드가 "획득 가능" 상태면 진입 선로가 accent로 맥동한다 (DESIGN.md 상태 표).
    /// </summary>
    public class SkillTreeRailUI : MonoBehaviour
    {
        private const float SweepDuration = 0.32f;
        private const float PulseDuration = 0.8f;
        private const float PulseMaxAlpha = 0.55f;

        #region Fields
        [SerializeField] private Image baseImage;   // 미점등 침목 — Rail.png 타일링 권장
        [SerializeField] private Image litImage;    // 점등 선로 — Image Type=Filled, Vertical, Origin=Bottom 필수
        #endregion

        private string _fromNodeId;
        private string _toNodeId;
        private float _length;
        private float _thickness;
        private Tween _sweepTween;
        private Tween _pulseTween;
        private bool _isPulsing;

        /// <summary>이 선로가 도착하는(점등 조건이 되는) 노드 id.</summary>
        public string ToNodeId => _toNodeId;

        private void Awake()
        {
            if (baseImage != null)
                baseImage.color = SkillTreePalette.SurfaceLine;
            if (litImage != null)
                litImage.color = SkillTreePalette.Accent;
        }

        private void OnDestroy()
        {
            _sweepTween?.Kill();
            _pulseTween?.Kill();
        }

        public void Init(string fromNodeId, string toNodeId)
        {
            _fromNodeId = fromNodeId;
            _toNodeId = toNodeId;
        }

        /// <summary>두 노드 사이에 선로를 놓는다. 좌표는 같은 부모(treeContent) 기준 anchoredPosition.</summary>
        public void Place(Vector2 fromPosition, Vector2 toPosition, float thickness)
        {
            var rectTransform = (RectTransform)transform;
            Vector2 delta = toPosition - fromPosition;

            _length = delta.magnitude;
            _thickness = thickness;
            rectTransform.anchoredPosition = (fromPosition + toPosition) * 0.5f;
            rectTransform.sizeDelta = new Vector2(thickness, _length);
            // 세로(+y)가 진행 방향이 되도록 회전 — 출발 노드가 아래쪽이 된다
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);

            // 기찻길 스프라이트는 가로 이미지 — 자식을 +90° 회전시켜 x축을 진행 방향(위)으로 눕힌다
            if (baseImage != null)
                _ConfigureOrientedRect((RectTransform)baseImage.transform, _length, isLitLayer: false);
            if (litImage != null)
                _ConfigureOrientedRect((RectTransform)litImage.transform, 0f, isLitLayer: true);
        }

        // 점등 레이어는 pivot을 출발(아래) 끝에 둬서 길이가 아래→위로 자라게 한다
        private void _ConfigureOrientedRect(RectTransform rect, float length, bool isLitLayer)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = isLitLayer ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rect.anchoredPosition = isLitLayer ? new Vector2(0f, -_length * 0.5f) : Vector2.zero;
            rect.sizeDelta = new Vector2(length, _thickness);
        }

        private void _SetLitLength(float length)
        {
            var litRect = (RectTransform)litImage.transform;
            litRect.sizeDelta = new Vector2(length, _thickness);
        }

        /// <summary>점등 상태 반영. animated면 아래→위로 길이가 자라는 스윕 연출 (timeScale=0에서도 재생).</summary>
        public void SetLit(bool isLit, bool animated)
        {
            if (litImage == null) return;

            _sweepTween?.Kill();
            _StopPulse();

            if (!isLit)
            {
                _SetLitLength(0f);

                return;
            }

            if (animated)
            {
                _SetLitLength(0f);
                _sweepTween = DOTween.To(() => ((RectTransform)litImage.transform).sizeDelta.x, _SetLitLength, _length, SweepDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
            else
            {
                _SetLitLength(_length);
            }
        }

        /// <summary>
        /// 도착 노드가 "획득 가능"일 때의 진입 선로 맥동. SetLit 이후에 호출한다 (점등 선로에는 적용 안 됨).
        /// </summary>
        public void SetApproachPulse(bool isPulsing)
        {
            if (litImage == null) return;
            if (isPulsing == _isPulsing) return;

            if (!isPulsing)
            {
                _StopPulse();
                _SetLitLength(0f);

                return;
            }

            _isPulsing = true;
            _sweepTween?.Kill();
            _SetLitLength(_length);
            litImage.color = new Color(SkillTreePalette.Accent.r, SkillTreePalette.Accent.g, SkillTreePalette.Accent.b, 0f);
            _pulseTween = litImage.DOFade(PulseMaxAlpha, PulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void _StopPulse()
        {
            if (!_isPulsing) return;

            _isPulsing = false;
            _pulseTween?.Kill();
            litImage.color = SkillTreePalette.Accent;
        }
    }
}
