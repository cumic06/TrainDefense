using UnityEngine;
using TrainDefense.Game;

namespace TrainDefense.Game.Events
{
    public class HitEvent
    {
        private int _currentHp;
        private int _maxHp;
        private IDamageable _damageable;
        private Vector3 _position;
        private int _damage;
        private bool _isCritical;

        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public float CurrentHpRatio => (float)_currentHp / _maxHp;
        public IDamageable Damageable => _damageable;
        public Vector3 Position => _position;
        public int Damage => _damage;
        public bool IsCritical => _isCritical;

        public HitEvent(int currentHp, int maxHp, IDamageable damageable, Vector3 position, int damage, bool isCritical = false)
        {
            _currentHp = currentHp;
            _maxHp = maxHp;
            _damageable = damageable;
            _position = position;
            _damage = damage;
            _isCritical = isCritical;
        }
    }
}