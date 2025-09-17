using TrainDefense.Game.Data;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game
{
    public abstract class Train : MonoBehaviour, ITrainable, IDamageable
    {
        #region Field
        [SerializeField]
        protected TrainData trainData;
        #endregion

        protected bool _isDead;
        protected int _currentHp;

        public bool IsDead => _isDead;

        protected virtual void Start()
        {
            _isDead = false;
            _currentHp = trainData.MaxHp;
        }

        public virtual void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp -= damage;

            GameEventSystem.Publish(new HitEvent(_currentHp, trainData.MaxHp));

            if (_currentHp <= 0)
            {
                OnDead();
            }
        }

        protected virtual void OnDead()
        {
            _isDead = true;
        }
    }
}