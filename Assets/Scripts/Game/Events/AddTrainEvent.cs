using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class AddTrainEvent
    {
        private Sprite _icon;

        public Sprite Icon => _icon;

        public AddTrainEvent(Sprite icon)
        {
            _icon = icon;
        }
    }
}