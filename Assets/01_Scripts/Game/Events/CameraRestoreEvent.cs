using UnityEngine;

namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 카메라를 follow 타깃(보통 기차)으로 되돌리고 기본 OrthographicSize로 줌아웃한 뒤,
    /// 종횡비 자동 보정(AspectFit)을 재개한다(상점 퇴장 등).
    /// </summary>
    public class CameraRestoreEvent
    {
        public Transform FollowTarget { get; }
        public float Duration { get; }

        public CameraRestoreEvent(Transform followTarget, float duration)
        {
            FollowTarget = followTarget;
            Duration = duration;
        }
    }
}
