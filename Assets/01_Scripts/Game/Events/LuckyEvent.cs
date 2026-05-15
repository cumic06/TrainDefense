using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class LuckyEvent
    {
        public Vector3 Position { get; }

        public LuckyEvent(Vector3 position)
        {
            Position = position;
        }
    }
}
