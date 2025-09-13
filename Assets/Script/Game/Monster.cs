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

        protected int currentHp;
        protected int currentDamage;
        protected float currentMoveSpeed;
        protected Train targetTrain;

        private void Start()
        {
            InitStats();
        }

        private void InitStats()
        {
            currentHp = maxHp;
            currentDamage = damage;
            currentMoveSpeed = moveSpeed;
        }

        private void FixedUpdate()
        {
            DetectTrain();
            Move();
        }

        private void DetectTrain()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectRange);
            Train[] trains = colliders.Where(a => a.GetComponent<Train>() != null)
            .Select(a => a.GetComponent<Train>())
            .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
            .ToArray();

            if (trains.Length > 0)
            {
                targetTrain = trains.FirstOrDefault();
            }
        }

        private void Move()
        {
            if (targetTrain == null) return;

            Vector3 direction = (targetTrain.transform.position - transform.position).normalized;
            transform.Translate(direction * Time.deltaTime * currentMoveSpeed);
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