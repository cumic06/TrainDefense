using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// 스킬트리 선로 세그먼트 — 선행 노드에서 다음 노드로 이어지는 간선 하나 (시그니처 "점등되는 선로").
    /// 미점등 침목(baseImage) 위에 accent 점등 이미지(litImage, Filled Vertical·Bottom)가 겹쳐 있고,
    /// 도착 노드를 처음 습득하는 순간 아래→위로 흐르며 점등된다 (스윕 320ms, SetUpdate(true)).
    /// </summary>
    public class SkillTreeRailUI : MonoBehaviour
    {
        private const float SweepDuration = 0.32f;

        #region Fields
        [SerializeField] private Image baseImage;   // 미점등 침목 — Rail.png 타일링 권장
        [SerializeField] private Image litImage;    // 점등 선로 — Image Type=Filled, Vertical, Origin=Bottom 필수
        #endregion

        private string _fromNodeId;
        private string _toNodeId;
        private Tween _sweepTween;

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

            rectTransform.anchoredPosition = (fromPosition + toPosition) * 0.5f;
            rectTransform.sizeDelta = new Vector2(thickness, delta.magnitude);
            // 세로(+y)가 진행 방향이 되도록 회전 — fill Origin(Bottom)이 출발 노드 쪽이 된다
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
        }

        /// <summary>점등 상태 반영. animated면 아래→위 스윕 연출 (timeScale=0에서도 재생).</summary>
        public void SetLit(bool isLit, bool animated)
        {
            if (litImage == null) return;

            _sweepTween?.Kill();

            if (!isLit)
            {
                litImage.fillAmount = 0f;

                return;
            }

            if (animated)
            {
                litImage.fillAmount = 0f;
                _sweepTween = litImage.DOFillAmount(1f, SweepDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            }
            else
            {
                litImage.fillAmount = 1f;
            }
        }
    }
}
