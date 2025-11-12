using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class WarningEvent
    {
        private GameObject _warningObject;
        private Vector3 _worldPosition;
        private float _warningDelaySeconds;
        private Monster _target;

        public GameObject WarningObject => _warningObject;
        public Vector3 WorldPosition => _worldPosition;
        public float WarningDelaySeconds => _warningDelaySeconds;
        public Monster Target => _target;

        public WarningEvent(GameObject warningObject, Vector3 worldPosition, float warningDelaySeconds, Monster target)
        {
            _warningObject = warningObject;
            _worldPosition = worldPosition;
            _warningDelaySeconds = warningDelaySeconds;
            _target = target;
        }
    }
}