using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class ExpChangeEvent
    {
        private int changeValue;

        public int ChangeValue => changeValue;

        public ExpChangeEvent(int changeValue)
        {
            this.changeValue = changeValue;
        }
    }
}