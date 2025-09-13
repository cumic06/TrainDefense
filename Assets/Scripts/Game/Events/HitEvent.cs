using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class HitEvent
    {
        private int currentHp;
        private int maxHp;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public float CurrentHpRatio => (float)currentHp / maxHp;

        public HitEvent(int currentHp, int maxHp)
        {
            this.currentHp = currentHp;
            this.maxHp = maxHp;
        }
    }
}