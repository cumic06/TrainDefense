using System.Linq;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Monster : MonoBehaviour
    {
        #region Field
        [SerializeField]
        protected int maxHp;
        [SerializeField]
        protected int damage;
        [SerializeField]
        protected float moveSpeed;
        [SerializeField]
        protected float detectRange;
        [SerializeField]
        protected float attackRange;
        #endregion

        protected int _currentHp;
        protected int _currentDamage;
        protected float _currentMoveSpeed;
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
        }

        private void FixedUpdate()
        {
            DetectTrain();
            Move();
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