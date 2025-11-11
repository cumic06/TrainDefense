using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class WarningEvent
    {
        private GameObject _warningObject;
        private Vector3 _worldPosition;
        private float _delaySeconds;
        private Monster _target;

        public GameObject WarningObject => _warningObject;
        public Vector3 WorldPosition => _worldPosition;
        public float DelaySeconds => _delaySeconds;
        public Monster Target => _target;

        public WarningEvent(GameObject warningObject, Vector3 worldPosition, float delaySeconds, Monster target)
        {
            _warningObject = warningObject;
            _worldPosition = worldPosition;
            _delaySeconds = delaySeconds;
            _target = target;
        }
    }
}