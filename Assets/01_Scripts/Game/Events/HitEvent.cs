using UnityEngine;
using TrainDefense.Game;

namespace TrainDefense.Game.Events
{
    public class HitEvent
    {
        private float _currentHp;
        private float _maxHp;
        private IDamageable _damageable;
        private Vector3 _position;
        private float _damage;
        private bool _isCritical;

        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public float CurrentHpRatio => _currentHp / _maxHp;
        public IDamageable Damageable => _damageable;
        public Vector3 Position => _position;
        public float Damage => _damage;
        public bool IsCritical => _isCritical;

        public HitEvent(float currentHp, float maxHp, IDamageable damageable, Vector3 position, float damage, bool isCritical = false)
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