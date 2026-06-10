using UnityEngine;

namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 카메라를 특정 타깃으로 옮기며 지정 OrthographicSize로 줌인한다(상점 진입 등).
    /// 줌 동안 종횡비 자동 보정(AspectFit)은 일시 중단된다. Delay 후 타깃 전환과 줌이 시작된다.
    /// </summary>
    public class CameraZoomEvent
    {
        public Transform Target { get; }
        public float OrthoSize { get; }
        public float Duration { get; }
        public float Delay { get; }

        public CameraZoomEvent(Transform target, float orthoSize, float duration, float delay = 0f)
        {
            Target = target;
            OrthoSize = orthoSize;
            Duration = duration;
            Delay = delay;
        }
    }
}
