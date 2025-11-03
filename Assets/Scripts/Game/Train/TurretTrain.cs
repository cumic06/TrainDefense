using System.Linq;
using DG.Tweening;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TurretTrain : Train, ITrainable
    {
        #region Field
        [SerializeField]
        private TurretTrainData turretTrainData => _trainData as TurretTrainData;

        [SerializeField]
        private Transform[] turretProjectileSpawnPoints;
        [SerializeField]
        private bool useParticleProjectile;

        [SerializeField]
        private GameObject turretModel;
        #endregion

        private Monster _targetMonster;

        private TurretTrainStatus _currentTurretTrainStatus;
        private ParticleProjectile _particleProjectilePrefab;

        protected override void Setup()
        {
            base.Setup();
            _currentTurretTrainStatus = turretTrainData.TurretTrainStatus;
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            DetectTarget();

            if (IsAttackDelayZero())
            {
                AttackHandler();
            }
        }

        private void DetectTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _currentTurretTrainStatus.AttackRange);
            _targetMonster = colliders.Where(a => a.GetComponent<Monster>() != null)
            .Select(a => a.GetComponent<Monster>())
            .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
            .FirstOrDefault();
        }

        private bool IsAttackDelayZero()
        {
            if (_currentTurretTrainStatus.AttackDelay <= 0)
            {
                _currentTurretTrainStatus.AttackDelay = turretTrainData.TurretTrainStatus.AttackDelay;
                return true;
            }
            else
            {
                _currentTurretTrainStatus.AttackDelay -= Time.deltaTime;
                return false;
            }
        }

        private void AttackHandler()
        {
            if (_targetMonster == null)
            {
                ResetTarget();
                return;
            }

            Attack();
        }

        private void ResetTarget()
        {
            _currentTurretTrainStatus.AttackDelay = turretTrainData.TurretTrainStatus.AttackDelay;

            if (useParticleProjectile)
            {
                if (_particleProjectilePrefab != null)
                {
                    _particleProjectilePrefab.gameObject.SetActive(false);
                }
            }
        }

        private void Attack()
        {
            turretModel.transform.LookAt2D(_targetMonster.transform);
            turretModel.transform.DOScale(Vector3.one * 0.9f, 0.1f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                turretModel.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.InBack);
            });

            if (!useParticleProjectile)
            {
                NormalAttack();
            }
            else
            {
                ParticleAttack();
            }
        }

        private void NormalAttack()
        {
            for (int i = 0; i < _currentTurretTrainStatus.AttackCount; i++)
            {
                Projectile bullet = ResourceManager.Instance.Spawn(turretTrainData.TurretProjectilePrefab?.GetComponent<Projectile>());
                bullet.transform.position = turretProjectileSpawnPoints[i].position;
                bullet.Init(_currentTurretTrainStatus.AttackDamage);
                bullet.transform.LookAt2D(_targetMonster.transform);
            }
        }

        #region ParticleAttack
        private void ParticleAttack()
        {
            if (_particleProjectilePrefab == null)
            {
                ParticleProjectileSpawn();
            }
            else
            {
                _particleProjectilePrefab.gameObject.SetActive(true);
                _particleProjectilePrefab.Init(_currentTurretTrainStatus.AttackDamage);
            }
        }

        private void ParticleProjectileSpawn()
        {
            Projectile projectile = turretTrainData.TurretProjectilePrefab?.GetComponent<Projectile>();
            if (projectile != null)
            {
                ParticleProjectile particleProjectile = projectile.GetComponent<ParticleProjectile>();
                if (particleProjectile == null)
                {
                    Debug.LogWarning($"TurretTrain: ParticleProjectile requires ParticleProjectile component on {turretTrainData.TurretProjectilePrefab.name}");
                    return;
                }
                _particleProjectilePrefab = ResourceManager.Instance.Spawn(particleProjectile, parent: turretProjectileSpawnPoints[0]);
                _particleProjectilePrefab.transform.localScale = Vector3.one;
                _particleProjectilePrefab.transform.localPosition = Vector3.zero;
                _particleProjectilePrefab.transform.localRotation = Quaternion.identity;
            }
        }
        #endregion

        public override void Upgrade(ITrainUpgradeData upgradeData)
        {
            base.Upgrade(upgradeData);

            if (upgradeData == null) return;

            // TurretTrain 전용 업그레이드 데이터가 있다면 적용
            if (upgradeData is TurretTrainUpgradeData turretUpgradeData)
            {
                _currentTurretTrainStatus.AttackDamage += turretUpgradeData.TurretStatusUpgrade.AttackDamage;
                _currentTurretTrainStatus.AttackRange += turretUpgradeData.TurretStatusUpgrade.AttackRange;
                _currentTurretTrainStatus.AttackCount += turretUpgradeData.TurretStatusUpgrade.AttackCount;
                _currentTurretTrainStatus.AttackDelay += turretUpgradeData.TurretStatusUpgrade.AttackDelay;
            }
        }

        protected override void OnDead()
        {
            if (useParticleProjectile && _particleProjectilePrefab != null)
            {
                _particleProjectilePrefab.gameObject.SetActive(false);
            }
            base.OnDead();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (turretTrainData != null)
            {
                Gizmos.DrawWireSphere(transform.position, _currentTurretTrainStatus.AttackRange);
            }
        }
    }
#endif
}