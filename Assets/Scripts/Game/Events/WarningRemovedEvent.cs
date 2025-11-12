using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class WarningRemovedEvent
    {
        private Vector3 _worldPosition;
        private float _warningDelaySeconds;
        private Monster _target;

        public Vector3 WorldPosition => _worldPosition;
        public float WarningDelaySeconds => _warningDelaySeconds;
        public Monster Target => _target;

        public WarningRemovedEvent(Vector3 worldPosition, float warningDelaySeconds, Monster target)
        {
            _worldPosition = worldPosition;
            _warningDelaySeconds = warningDelaySeconds;
            _target = target;
        }
    }
}