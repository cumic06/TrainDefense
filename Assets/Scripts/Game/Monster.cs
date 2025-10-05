using System.Linq;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Monster : MonoBehaviour, IDamageable
    {
        #region Field
        [SerializeField]
        protected MonsterData monsterData;
        #endregion

        protected MonsterStatusInfo _currentMonsterStatus;

        protected bool _isDead;
        protected Train _targetTrain;

        private void Start()
        {
            InitStats();
        }

        private void OnEnable()
        {
            InitStats();
            _isDead = false;
            _targetTrain = null;
        }

        private void InitStats()
        {
            _currentMonsterStatus = monsterData.MonsterStatusData;
        }

        private void FixedUpdate()
        {
            DetectTrain();
            Move();
            AttackHandler();
        }

        private void DetectTrain()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _currentMonsterStatus.DetectRange);
            Train[] trains = colliders.Where(a => a.GetComponent<Train>() != null)
            .Select(a => a.GetComponent<Train>())
            .Where(a => !a.IsDead && !a.IsMainTrain)
            .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
            .ToArray();

            if (trains.Length > 0)
            {
                _targetTrain = trains.FirstOrDefault();
            }
        }

        private void Move()
        {
            if (_targetTrain == null) return;

            Vector3 direction = _targetTrain.transform.position - transform.position;
            transform.Translate(direction.normalized * Time.deltaTime * _currentMonsterStatus.MoveSpeed);
        }

        private void AttackHandler()
        {
            if (_currentMonsterStatus.AttackDelay <= 0)
            {
                _currentMonsterStatus.AttackDelay = monsterData.MonsterStatusData.AttackDelay;
                Attack();
            }
            else
            {
                _currentMonsterStatus.AttackDelay -= Time.deltaTime;
            }
        }

        private void Attack()
        {
            if (_targetTrain == null) return;

            if (Vector3.Distance(transform.position, _targetTrain.transform.position) <= _currentMonsterStatus.AttackRange)
            {
                _targetTrain.TakeDamage(_currentMonsterStatus.Damage);
            }
        }

        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentMonsterStatus.MaxHp -= damage;
            if (_currentMonsterStatus.MaxHp <= 0)
            {
                OnDead();
            }
        }

        private void OnDead()
        {
            _isDead = true;
            int dropMoney = Random.Range(_currentMonsterStatus.DropMoneyMin, _currentMonsterStatus.DropMoneyMax);
            ResourceManager.Instance.Spawn(Resources.Load<GameObject>("Prefabs/Money"), transform.position);
            GameEventSystem.Publish(new MonsterDeadEvent(dropMoney));
            ResourceManager.Instance.Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _currentMonsterStatus.DetectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _currentMonsterStatus.AttackRange);
        }
#endif
    }
}