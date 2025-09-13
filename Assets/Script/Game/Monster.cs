using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Monster : MonoBehaviour, IDamageable
    {
        #region Field
        [SerializeField]
        protected int maxHp;
        [SerializeField]
        protected int damage;
        [SerializeField]
        protected float moveSpeed;
        [SerializeField]
        protected float attackDelay;
        [SerializeField]
        [BoxGroup("RangeSetting")]
        protected float detectRange;
        [SerializeField]
        [BoxGroup("RangeSetting")]
        protected float attackRange;
        #endregion

        protected int _currentHp;
        protected int _currentDamage;
        protected float _currentMoveSpeed;
        protected float _currentAttackDelay;
        protected bool _isDead;
        protected MainTrain _targetTrain;

        private void Start()
        {
            InitStats();
        }

        private void InitStats()
        {
            _currentHp = maxHp;
            _currentDamage = damage;
            _currentMoveSpeed = moveSpeed;

            _currentAttackDelay = attackDelay;
        }

        private void FixedUpdate()
        {
            DetectTrain();
            Move();
            AttackHandler();
        }

        private void DetectTrain()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectRange);
            MainTrain[] trains = colliders.Where(a => a.GetComponent<MainTrain>() != null)
            .Select(a => a.GetComponent<MainTrain>())
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

            Vector3 direction = (_targetTrain.transform.position - transform.position).normalized;
            transform.Translate(direction * Time.deltaTime * _currentMoveSpeed);
        }

        private void AttackHandler()
        {
            if (_currentAttackDelay <= 0)
            {
                _currentAttackDelay = attackDelay;
                Attack();
            }
            else
            {
                _currentAttackDelay -= Time.deltaTime;
            }
        }

        private void Attack()
        {
            if (_targetTrain == null) return;

            if (Vector3.Distance(transform.position, _targetTrain.transform.position) <= attackRange)
            {
                _targetTrain.TakeDamage(_currentDamage);
            }
        }

        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp -= damage;
            if (_currentHp <= 0)
            {
                OnDead();
            }
        }

        private void OnDead()
        {
            _isDead = true;
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
#endif
    }
}