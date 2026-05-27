using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class GameOverStartEvent
    {
        public float Duration { get; }
        public Transform Target { get; }

        public GameOverStartEvent(float duration, Transform target = null)
        {
            Duration = duration;
            Target = target;
        }
    }
}
