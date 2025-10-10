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
        private TurretTrainData turretTrainData => trainData as TurretTrainData;

        [SerializeField]
        private Transform[] turretProjectileSpawnPoints;
        [SerializeField]
        private bool isParticleProjectile;

        [SerializeField]
        private GameObject turretModel;
        #endregion

        private Monster _targetMonster;

        private TurretTrainStatus _currentTurretTrainStatus;
        private Projectile _particleProjectilePrefab;

        protected override void Start()
        {
            base.Start();
            _currentTurretTrainStatus = turretTrainData.TurretTrainStatus;
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            DetectTarget();

            if (_targetMonster == null)
            {
                Debug.Log($"Target Null");
                _currentTurretTrainStatus.AttackDelay = turretTrainData.TurretTrainStatus.AttackDelay;

                if (isParticleProjectile)
                {
                    if (_particleProjectilePrefab != null)
                    {
                        _particleProjectilePrefab.gameObject.SetActive(false);
                    }
                }
                return;
            }

            AttackHandler();
        }

        private void DetectTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _currentTurretTrainStatus.AttackRange);
            _targetMonster = colliders.Where(a => a.GetComponent<Monster>() != null)
            .Select(a => a.GetComponent<Monster>())
            .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
            .FirstOrDefault();
        }

        private void AttackHandler()
        {
            if (_currentTurretTrainStatus.AttackDelay <= 0)
            {
                _currentTurretTrainStatus.AttackDelay = turretTrainData.TurretTrainStatus.AttackDelay;
                Attack();
            }
            else
            {
                _currentTurretTrainStatus.AttackDelay -= Time.deltaTime;
            }
        }

        private bool particleAttack;
        private void Attack()
        {
            if (_targetMonster == null) return;

            turretModel.transform.LookAt2D(_targetMonster.transform);
            turretModel.transform.DOScale(Vector3.one * 0.9f, 0.1f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                turretModel.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.InBack);
            });

            if (!isParticleProjectile)
            {
                for (int i = 0; i < _currentTurretTrainStatus.AttackCount; i++)
                {
                    Projectile bullet = ResourceManager.Instance.Spawn(turretTrainData.TurretProjectilePrefab);
                    bullet.transform.position = turretProjectileSpawnPoints[i].position;
                    bullet.Init(_currentTurretTrainStatus.AttackDamage);
                    bullet.transform.LookAt2D(_targetMonster.transform);
                }
            }
            else
            {
                if (particleAttack) return;
                particleAttack = true;

                if (_particleProjectilePrefab == null)
                {
                    _particleProjectilePrefab = ResourceManager.Instance.Spawn(turretTrainData.TurretProjectilePrefab);
                    _particleProjectilePrefab.transform.SetParent(turretProjectileSpawnPoints[0]);
                    _particleProjectilePrefab.transform.localScale = Vector3.one;
                    _particleProjectilePrefab.transform.localPosition = Vector3.zero;
                    _particleProjectilePrefab.transform.localRotation = Quaternion.identity;
                }
                _particleProjectilePrefab.gameObject.SetActive(true);
                _particleProjectilePrefab.Init(_currentTurretTrainStatus.AttackDamage);
            }
        }

        public override void Upgrade(TrainUpgradeData upgradeData)
        {
            base.Upgrade(upgradeData);

            if (upgradeData == null) return;

            // TurretTrain 전용 업그레이드 데이터가 있다면 적용
            if (upgradeData.ExtensionData is TurretTrainUpgradeExtension turretUpgrade)
            {
                _currentTurretTrainStatus.AttackDamage += turretUpgrade.TurretStatusUpgrade.AttackDamage;
                _currentTurretTrainStatus.AttackRange += turretUpgrade.TurretStatusUpgrade.AttackRange;
                _currentTurretTrainStatus.AttackCount += turretUpgrade.TurretStatusUpgrade.AttackCount;
                _currentTurretTrainStatus.AttackDelay += turretUpgrade.TurretStatusUpgrade.AttackDelay;
            }
        }

        protected override void OnDead()
        {
            if (_particleProjectilePrefab != null)
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