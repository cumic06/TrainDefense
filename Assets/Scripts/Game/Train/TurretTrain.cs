using System.Linq;
using DG.Tweening;
using TrainDefense.Game.Data;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TurretTrain : Train, ITrainable
    {
        #region Field
        [SerializeField]
        private TurretTrainData turretTrainData => trainData as TurretTrainData;

        [SerializeField]
        private Transform[] turretProjectileSpawnPoints;

        [SerializeField]
        private GameObject turretModel;
        #endregion

        private Monster _targetMonster;
        private float _currentAttackDelay;

        protected override void Start()
        {
            base.Start();
            _currentAttackDelay = turretTrainData.AttackDelay;
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            DetectTarget();
            AttackHandler();
        }

        private void DetectTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, turretTrainData.AttackRange);
            _targetMonster = colliders.Where(a => a.GetComponent<Monster>() != null)
            .Select(a => a.GetComponent<Monster>())
            .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
            .FirstOrDefault();
        }

        private void AttackHandler()
        {
            if (_currentAttackDelay <= 0)
            {
                _currentAttackDelay = turretTrainData.AttackDelay;
                Attack();
            }
            else
            {
                _currentAttackDelay -= Time.deltaTime;
            }
        }

        private void Attack()
        {
            if (_targetMonster == null) return;

            turretModel.transform.LookAt2D(_targetMonster.transform);
            turretModel.transform.DOScale(Vector3.one * 0.9f, 0.1f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                turretModel.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.InBack);
            });

            for (int i = 0; i < turretTrainData.AttackCount; i++)
            {
                Projectile bullet = ResourceManager.Instance.Spawn(turretTrainData.TurretProjectilePrefab);
                bullet.transform.position = turretProjectileSpawnPoints[i].position;
                bullet.transform.LookAt2D(_targetMonster.transform);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (turretTrainData != null)
            {
                Gizmos.DrawWireSphere(transform.position, turretTrainData.AttackRange);
            }
        }
    }
#endif
}