using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TurretTrain : MonoBehaviour, ITrainable
    {
        #region Field
        [SerializeField]
        [BoxGroup("AttackSetting")]
        private int attackDamage;
        [SerializeField]
        [BoxGroup("AttackSetting")]
        private int attackCount;
        [SerializeField]
        [BoxGroup("AttackSetting")]
        private float attackDelay;
        [SerializeField]
        [BoxGroup("AttackSetting")]
        private float attackRange;

        [SerializeField]
        private Projectile turretProjectilePrefab;
        [SerializeField]
        private Transform[] turretProjectileSpawnPoints;

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
            turretModel.transform.DOScale(Vector3.one * 0.9f, 0.1f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                turretModel.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.InBack);
            });

            for (int i = 0; i < attackCount; i++)
            {
                Projectile bullet = ResourceManager.Instance.Spawn(turretProjectilePrefab);
                bullet.transform.position = turretProjectileSpawnPoints[i].position;
                bullet.transform.LookAt2D(targetMonster.transform);
            }
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