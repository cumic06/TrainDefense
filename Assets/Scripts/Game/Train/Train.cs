using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;

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

        public bool IsUnDead;

        public TrainData TrainData => trainData;
        public bool IsMainTrain => trainData.IsMainTrain;

        public bool IsDead => _isDead;
        public int CurrentLevel => _currentLevel;

        protected virtual void Start()
        {
            Setup();
        }

        protected virtual void Setup()
        {
            _isDead = false;
            _currentHp = trainData.TrainStatusData.MaxHp;
            _currentLevel = 1;
        }


        public virtual void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp -= damage;
            _currentHp = Mathf.Clamp(_currentHp, 0, trainData.TrainStatusData.MaxHp);

            GameEventSystem.Publish(new HitEvent(_currentHp, trainData.TrainStatusData.MaxHp, this, transform.position, damage));

            if (_currentHp <= 0)
            {
                OnDead();
            }
        }

        protected virtual void OnDead()
        {
            if (IsUnDead) return;

            if (!IsMainTrain)
            {
                GameEventSystem.Publish(new TrainDeadEvent(this));
            }

            _isDead = true;
        }

        public virtual void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            _currentLevel++;
            _currentHp += upgradeData.StatusUpgrade.MaxHp;
        }
    }
}