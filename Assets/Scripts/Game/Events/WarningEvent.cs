using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class WarningEvent
    {
        private string _id;
        private Vector3 _position;
        private Vector3 _size;

        public string Id => _id;
        public Vector3 Position => _position;
        public Vector3 Size => _size;

        public WarningEvent(string id, Vector3 position, Vector3 size)
        {
            _id = id;
            _position = position;
            _size = size;
        }
    }
}