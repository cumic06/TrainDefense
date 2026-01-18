using UnityEngine;
using TrainDefense.Game;

namespace TrainDefense.Game.Events
{
    public class WarningRemovedEvent
    {
        private Vector3 _worldPosition;
        private float _warningDelaySeconds;
        private Monster _target;
        private Train _sender;

        public Vector3 WorldPosition => _worldPosition;
        public float WarningDelaySeconds => _warningDelaySeconds;
        public Monster Target => _target;
        public Train Sender => _sender;

        public WarningRemovedEvent(Vector3 worldPosition, float warningDelaySeconds, Monster target, Train sender)
        {
            _worldPosition = worldPosition;
            _warningDelaySeconds = warningDelaySeconds;
            _target = target;
            _sender = sender;
        }
    }
}