using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game
{
    public abstract class Train : MonoBehaviour, ITrainable, IDamageable
    {
        #region Field
        [SerializeField]
        protected string id;
        #endregion

        [ShowInInspector, ReadOnly]
        protected TrainData _trainData;
        protected bool _isDead;
        protected int _currentHp;
        protected int _currentLevel;
        protected int _currentMaxHp;

        public bool IsUnDead;

        public string Id => id;
        public TrainData TrainData => _trainData;
        public bool IsMainTrain => _trainData.IsMainTrain;

        public bool IsDead => _isDead;
        public int CurrentLevel => _currentLevel;

        protected virtual void Start()
        {
            Setup();
        }

        public virtual void Initialize(TrainData trainData)
        {
            _trainData = trainData;
            Setup();
        }

        protected virtual void Setup()
        {
            _isDead = false;
            _currentMaxHp = _trainData.TrainStatusData.MaxHp;
            _currentHp = _currentMaxHp;
            _currentLevel = 1;
        }


        public virtual void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp -= damage;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);

            GameEventSystem.Publish(new HitEvent(_currentHp, _currentMaxHp, this, transform.position, damage));

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
            _currentMaxHp += upgradeData.StatusUpgrade.MaxHp;
            _currentHp += upgradeData.StatusUpgrade.MaxHp;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
        }

        public virtual void StatusUpgrade(TrainStatusData upgradeData)
        {
            _currentMaxHp += upgradeData.MaxHp;
            _currentHp += upgradeData.MaxHp;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
        }

        public virtual void StatusUpgrade(TurretTrainStatus upgradeData)
        {

        }

        public virtual void StatusUpgrade(RangeTrainStatus upgradeData)
        {

        }

        public virtual void ApplyStats(IStat[] stats)
        {
            if (stats == null || stats.Length == 0) return;

            foreach (var stat in stats)
            {
                ApplyStat(stat);
            }
        }

        protected virtual void ApplyStat(IStat stat)
        {
            if (stat == null) return;

            switch (stat.Type)
            {
                case StatType.MaxHp:
                    {
                        int deltaHp = Mathf.RoundToInt(stat.Value);
                        _currentMaxHp += deltaHp;
                        _currentHp += deltaHp;
                        _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
                        break;
                    }
            }
        }
    }
}