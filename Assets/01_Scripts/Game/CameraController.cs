using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace TrainDefense.Game
{
    public class CameraController : MonoBehaviour
    {
        #region Variable

        #region Fields
        [SerializeField]
        private float testShakeIntensity;
        [SerializeField]
        private float testShakeDuration;
        #endregion

        private CinemachineCamera _camera;

        private CinemachineBasicMultiChannelPerlin _noiseComp;

        private float _startFieldOfView;
        private Coroutine _shakeCor;
        private Tween _zoomTween;
        #endregion

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            _noiseComp = _camera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        private void Start()
        {
            _startFieldOfView = _camera.Lens.FieldOfView;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ShakeCamera(testShakeIntensity, testShakeDuration);
            }
        }

        [Button]
        public void ShakeCamera(float intensity, float duration)
        {
            if (_shakeCor != null)
            {
                StopCoroutine(_shakeCor);
            }

            _shakeCor = StartCoroutine(ShakeCor(intensity, duration));
        }

        private IEnumerator ShakeCor(float intensity, float duration)
        {
            _noiseComp.AmplitudeGain = intensity;
            _noiseComp.FrequencyGain = duration;
            yield return new WaitForSeconds(duration);
            _noiseComp.AmplitudeGain = 0;
            _noiseComp.FrequencyGain = 0;
        }

        public void ZoomCamera(GameObject zoomObject, float fov, float duration)
        {
            _camera.Follow = zoomObject.transform;
            _camera.LookAt = zoomObject.transform;

            _zoomTween = DOTween.To(
            () => _camera.Lens.OrthographicSize,
            x => _camera.Lens.OrthographicSize = x,
            fov,
            duration
        ).SetEase(Ease.InOutSine);
        }
    }
}