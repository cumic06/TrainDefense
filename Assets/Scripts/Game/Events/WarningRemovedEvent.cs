using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class WarningRemovedEvent
    {
        private Vector3 _worldPosition;
        private Monster _target;

        public Vector3 WorldPosition => _worldPosition;
        public Monster Target => _target;

        public WarningRemovedEvent(Vector3 worldPosition, Monster target)
        {
            _worldPosition = worldPosition;
            _target = target;
        }
    }
}