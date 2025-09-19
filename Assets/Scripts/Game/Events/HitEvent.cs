namespace TrainDefense.Game.Events
{
    public class HitEvent
    {
        private int currentHp;
        private int maxHp;
        private IDamageable _damageable;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public float CurrentHpRatio => (float)currentHp / maxHp;
        public IDamageable Damageable => _damageable;

        public HitEvent(int currentHp, int maxHp, IDamageable damageable)
        {
            this.currentHp = currentHp;
            this.maxHp = maxHp;
            _damageable = damageable;
        }
    }
}