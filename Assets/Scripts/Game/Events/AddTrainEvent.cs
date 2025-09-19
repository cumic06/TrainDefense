using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class AddTrainEvent
    {
        private Sprite _icon;
        private Train _train;

        public Sprite Icon => _icon;
        public Train Train => _train;

        public AddTrainEvent(Sprite icon, Train train)
        {
            _icon = icon;
            _train = train;
        }
    }
}