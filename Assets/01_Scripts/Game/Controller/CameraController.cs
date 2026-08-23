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

        [BoxGroup("Entrance")]
        [SerializeField, Tooltip("기차 등장 연출(LoadingTimeline) 시작 시 카메라를 기본 크기의 몇 배로 벌린 상태에서 줌인을 시작할지. 1 이하면 줌 연출 없음.\n(예전 LoadingTimeline 카메라 트랙 키 15→9.156의 비율. 기본 크기는 씬 CinemachineCamera Lens.OrthographicSize + 종횡비 보정)")]
        private float entranceZoomOutMultiplier = 1.64f;
        [BoxGroup("Entrance")]
        [SerializeField, Tooltip("기차 등장 연출에서 기본 크기로 줌인되는 시간(초). 연출은 시간 정지 중이라 unscaled로 진행.")]
        private float entranceZoomDuration = 1f;

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
            GameEventSystem.Subscribe<TrainEntranceStartEvent>(_OnTrainEntranceStart);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);
            GameEventSystem.Unsubscribe<CameraShakeEvent>(_OnCameraShake);
            GameEventSystem.Unsubscribe<TrainEntranceStartEvent>(_OnTrainEntranceStart);
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
            // 옵션에서 카메라 흔들림을 끈 경우 무시한다.
            if (UserDataManager.Instance != null && !UserDataManager.Instance.IsCameraShakeEnabled)
                return;

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

        // 기차 등장 연출: 기본 크기 × entranceZoomOutMultiplier로 벌린 상태에서 기본 크기(종횡비 보정 포함)로 줌인한다.
        // 예전엔 LoadingTimeline의 Animation Track이 CinemachineCamera Lens.OrthographicSize를 15→9.156으로 직접 키잉해
        // 연출이 끝나면 씬에 설정한 크기와 무관하게 9.156에 고정됐다(줌 조정이 먹지 않음). 카메라 줌은 코드로 일원화하고 그 트랙은 음소거.
        private void _OnTrainEntranceStart(TrainEntranceStartEvent e)
        {
            if (Screen.height <= 0)
                return;

            float aspect = (float)Screen.width / Screen.height;
            float targetOrthoSize = _GetAspectFittedOrthoSize(aspect);

            if (entranceZoomOutMultiplier <= 1f || entranceZoomDuration <= 0f)
            {
                _camera.Lens.OrthographicSize = targetOrthoSize;
                return;
            }

            // 줌 트윈과 종횡비 보정이 같은 값을 두고 경합하지 않도록 연출 동안만 보정을 멈춘다(게임오버 줌과 동일 패턴).
            _aspectFitActive = false;
            _camera.Lens.OrthographicSize = targetOrthoSize * entranceZoomOutMultiplier;

            _zoomTween?.Kill();
            _zoomTween = DOTween.To(
                () => _camera.Lens.OrthographicSize,
                x => _camera.Lens.OrthographicSize = x,
                targetOrthoSize,
                entranceZoomDuration
            ).SetEase(Ease.InOutSine).SetUpdate(true).OnComplete(() =>
            {
                // 게임오버 줌이 먼저 이 트윈을 Kill했으면 OnComplete가 오지 않으므로 보정이 되살아나지 않는다.
                _aspectFitActive = true;
                _ApplyAspectFit(true);
            });
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
            _camera.Lens.OrthographicSize = _GetAspectFittedOrthoSize(aspect);
        }

        // 기준 화면비에서의 가로 가시 범위를 현재 화면비에서도 유지하는 OrthographicSize.
        private float _GetAspectFittedOrthoSize(float aspect)
        {
            float referenceAspect = _referenceResolution.x / _referenceResolution.y;
            return _startOrthoSize * (referenceAspect / aspect);
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