using System.Linq;
using UnityEngine;

namespace TrainDefense.Game
{
    public class NormalTurretTrain : MonoBehaviour, ITrainable
    {
        #region Field
        [SerializeField]
        private int attackDamage;
        [SerializeField]
        private float attackDelay;
        [SerializeField]
        private float attackRange;

        [SerializeField]
        private Projectile turretProjectilePrefab;
        [SerializeField]
        private Transform turretProjectileSpawnPoint;

        [SerializeField]
        private GameObject turretModel;
        #endregion

        private Monster targetMonster;
        private float currentAttackDelay;

        private void Start()
        {
            currentAttackDelay = attackDelay;
        }

        private void FixedUpdate()
        {
            DetectTarget();
            AttackHandler();
        }

        private void DetectTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
            targetMonster = colliders.Where(a => a.GetComponent<Monster>() != null)
            .Select(a => a.GetComponent<Monster>())
            .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
            .FirstOrDefault();
        }

        private void AttackHandler()
        {
            if (currentAttackDelay <= 0)
            {
                currentAttackDelay = attackDelay;
                Attack();
            }
            else
            {
                currentAttackDelay -= Time.deltaTime;
            }
        }

        private void Attack()
        {
            if (targetMonster == null) return;

            turretModel.transform.LookAt2D(targetMonster.transform);

            Projectile bullet = Instantiate(turretProjectilePrefab);
            bullet.transform.position = turretProjectileSpawnPoint.position;
            bullet.transform.LookAt2D(targetMonster.transform);
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
#endif
    }
}