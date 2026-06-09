using UnityEngine;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 부착된 RectTransform을 화면 종횡비에 맞춰 "Shrink" 방식으로 fit 스케일한다.
    /// CanvasScaler(Scale With Screen Size, Match 0.5)는 면적 기준이라 세로로 납작한 화면(폴더블 접힘 등)에서
    /// 세로로 긴 UI가 화면을 꽉 채우는 문제가 생긴다. 이 컴포넌트는 가로·세로 중 더 빡빡한 축 기준으로 줄여
    /// 어떤 화면에서도 대상이 항상 화면 안에 들어가게 한다(잘림 방지).
    ///
    /// Canvas 전체가 아니라 부착된 그룹에만 적용되므로 다른 UI에는 영향을 주지 않는다.
    /// 삼중택일 카드 그룹(Group_TrichoiceSelect)처럼 가로로 나열된 큰 UI 묶음에 붙여 사용한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIAspectFitScaler : MonoBehaviour
    {
        #region Fields
        [SerializeField, Tooltip("UI가 설계된 기준 해상도. CanvasScaler의 Reference Resolution과 동일하게 둔다.")]
        private Vector2 _referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField, Tooltip("최종 스케일 상한. 기준보다 큰 화면에서 과도하게 커지지 않도록 1로 두기를 권장한다.")]
        private float _maxScale = 1f;
        #endregion

        #region Variables
        private RectTransform _rect;
        private Canvas _canvas;
        private float _lastAspect = -1f;
        private float _lastCanvasScale = -1f;
        #endregion

        #region LifeCycle
        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable() => _ApplyFit(true);

        private void Update() => _ApplyFit(false);
        #endregion

        // 화면 비율 또는 Canvas 스케일이 바뀔 때만 그룹의 localScale을 다시 계산한다.
        private void _ApplyFit(bool force)
        {
            if (_canvas == null || Screen.height <= 0)
                return;

            float aspect = (float)Screen.width / Screen.height;
            float canvasScale = _canvas.scaleFactor;

            if (!force
                && Mathf.Approximately(aspect, _lastAspect)
                && Mathf.Approximately(canvasScale, _lastCanvasScale))
                return;

            _lastAspect = aspect;
            _lastCanvasScale = canvasScale;

            if (canvasScale <= 0f)
                return;

            // 가로·세로 중 더 빡빡한 축 기준(Shrink) fit 배율.
            float fit = Mathf.Min(Screen.width / _referenceResolution.x,
                                  Screen.height / _referenceResolution.y);
            // CanvasScaler가 이미 적용한 scaleFactor를 상쇄해, 그룹이 항상 화면 안에 들어가도록 보정한다.
            float scale = Mathf.Min(fit / canvasScale, _maxScale);
            _rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
