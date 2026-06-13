using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using DG.Tweening;
using Sirenix.OdinInspector;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.Controller
{
    public class CameraController : MonoBehaviour
    {
        #region Variables

        #region Fields
        [BoxGroup("GameOver")]
        [SerializeField]
        private float gameOverZoomOrthoSize = 4f;

        [BoxGroup("AspectFit")]
        [SerializeField, Tooltip("화면 종횡비 변화에 맞춰 가로 가시 범위를 유지합니다(폴더블 펼침 등에서 확대되는 현상 방지).")]
        private bool _enableAspectFit = true;
        [BoxGroup("AspectFit")]
        [SerializeField, Tooltip("OrthographicSize가 설계된 기준 해상도. 가로:세로 비율만 사용됩니다.")]
        private Vector2 _referenceResolution = new Vector2(1920f, 1080f);
        #endregion

        private CinemachineCamera _camera;
        private CinemachineBasicMultiChannelPerlin _noiseComp;
        private float _startOrthoSize;
        private float _lastAspect = -1f;
        private bool _aspectFitActive = true;
        private Coroutine _shakeCor;
        private Tween _zoomTween;
        #endregion

        #region LifeCycle
        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            _noiseComp = GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        private void Start()
        {
            _startOrthoSize = _camera.Lens.OrthographicSize;
            _SubscribeEvents();
            _ApplyAspectFit(true);
        }

        private void Update()
        {
            if (_enableAspectFit && _aspectFitActive)
                _ApplyAspectFit(false);
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();
        }
        #endregion

        #region Sub/UnSub
        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOverStart);
            GameEventSystem.Subscribe<CameraShakeEvent>(_OnCameraShake);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);
            GameEventSystem.Unsubscribe<CameraShakeEvent>(_OnCameraShake);
        }
        #endregion

        [Button]
        public void ShakeCamera(float intensity, float duration)
        {
            if (_shakeCor != null)
                StopCoroutine(_shakeCor);

            _shakeCor = StartCoroutine(_ShakeCor(intensity, duration));
        }

        public void SetFollowTarget(Transform target)
        {
            _camera.Target.TrackingTarget = target;
            _camera.Target.LookAtTarget = target;
        }

        public void ZoomCamera(Transform target, float orthoSize, float duration)
        {
            if (target != null)
            {
                _camera.Target.TrackingTarget = target;
                _camera.Target.LookAtTarget = target;
            }

            _zoomTween?.Kill();
            _zoomTween = DOTween.To(
                () => _camera.Lens.OrthographicSize,
                x => _camera.Lens.OrthographicSize = x,
                orthoSize,
                duration
            ).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        private void _OnCameraShake(CameraShakeEvent e)
        {
            ShakeCamera(e.Intensity, e.Duration);
        }

        private void _OnGameOverStart(GameOverStartEvent e)
        {
            if (e.Target == null)
                return;

            // 게임오버 줌 연출이 우선이므로 종횡비 대응은 중단한다.
            _aspectFitActive = false;
            ZoomCamera(e.Target, gameOverZoomOrthoSize, e.Duration);
        }

        // 가로 기준 고정: 기준 화면비에서의 가로 가시 범위가 유지되도록 OrthographicSize를 보정한다.
        // 화면이 좁아지면(폴더블 펼침 등) OrthographicSize를 키워 확대되어 보이는 현상을 막는다.
        private void _ApplyAspectFit(bool force)
        {
            if (Screen.height <= 0)
                return;

            float aspect = (float)Screen.width / Screen.height;
            if (!force && Mathf.Approximately(aspect, _lastAspect))
                return;

            _lastAspect = aspect;

            float referenceAspect = _referenceResolution.x / _referenceResolution.y;
            _camera.Lens.OrthographicSize = _startOrthoSize * (referenceAspect / aspect);
        }

        private IEnumerator _ShakeCor(float intensity, float duration)
        {
            _noiseComp.AmplitudeGain = intensity;
            _noiseComp.FrequencyGain = 2f;
            yield return new WaitForSeconds(duration);
            _noiseComp.AmplitudeGain = 0;
            _noiseComp.FrequencyGain = 0;
        }
    }
}