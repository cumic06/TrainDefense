using Cumic.Events;
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
        protected int _currentLevel;

        public bool IsDead => _isDead;
        public bool IsMainTrain => trainData.IsMainTrain;
        public int CurrentLevel => _currentLevel;
        public TrainData TrainData => trainData;

        protected virtual void Start()
        {
            _isDead = false;
            _currentHp = trainData.TrainStatusData.MaxHp;
            _currentLevel = 1;
        }

        public virtual void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp -= damage;

            GameEventSystem.Publish(new HitEvent(_currentHp, trainData.TrainStatusData.MaxHp, this));

            if (_currentHp <= 0)
            {
                OnDead();
            }
        }

        protected virtual void OnDead()
        {
            if (!IsMainTrain)
            {
                GameEventSystem.Publish(new TrainDeadEvent(this));
            }

            _isDead = true;
        }

        public virtual void Upgrade()
        {
            _currentLevel++;
        }
    }
}