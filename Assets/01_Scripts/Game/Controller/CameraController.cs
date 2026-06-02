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
        [SerializeField]
        private float testShakeIntensity;
        [SerializeField]
        private float testShakeDuration;

        [BoxGroup("GameOver")]
        [SerializeField]
        private float gameOverZoomOrthoSize = 4f;
        #endregion

        private CinemachineCamera _camera;
        private CinemachineBasicMultiChannelPerlin _noiseComp;
        private float _startOrthoSize;
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

            ZoomCamera(e.Target, gameOverZoomOrthoSize, e.Duration);
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