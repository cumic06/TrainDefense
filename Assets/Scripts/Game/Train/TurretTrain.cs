using System.Collections.Generic;
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
        private bool isTargeting = false;

        [SerializeField]
        private GameObject turretModel;
        #endregion

        private List<Monster> _targetMonsters = new();

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
            _targetMonsters = colliders.Where(a => a.GetComponent<Monster>() != null)
            .Select(a => a.GetComponent<Monster>())
            .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
            .ToList();
        }

        private Monster GetNearTargetMonster()
        {
            if (_targetMonsters.Count == 0) return null;

            return _targetMonsters.FirstOrDefault();
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
            if (_targetMonsters.Count == 0)
            {
                ResetTarget();
                return;
            }

            Attack();
        }

        private void ResetTarget()
        {
            _targetMonsters.Clear();
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
            if (turretModel != null)
            {
                turretModel.transform.LookAt2D(GetNearTargetMonster().transform);
                turretModel.transform.DOScale(Vector3.one * 0.9f, 0.1f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    turretModel.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.InBack);
                });
            }

            if (isTargeting)
            {
                TargetedAttack();
            }
            else if (!useParticleProjectile)
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
                if (i >= _targetMonsters.Count) break;

                Projectile bullet = ResourceManager.Instance.Spawn(turretTrainData.TurretProjectilePrefab?.GetComponent<Projectile>());
                bullet.transform.localScale = Vector3.one;

                if (i < turretProjectileSpawnPoints.Length)
                {
                    bullet.transform.SetParent(turretProjectileSpawnPoints[i]);
                }
                else
                {
                    bullet.transform.SetParent(turretProjectileSpawnPoints[0]);
                }

                bullet.transform.localPosition = Vector3.zero;

                bullet.Init(_currentTurretTrainStatus.AttackDamage, GetNearTargetMonster());
                bullet.transform.LookAt2D(GetNearTargetMonster().transform);
            }
        }

        private void TargetedAttack()
        {
            for (int i = 0; i < _currentTurretTrainStatus.TargetCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Debug.Log($"TargetedCount : {_targetMonsters.Count} realCount : {i}");

                Projectile bullet = ResourceManager.Instance.Spawn(turretTrainData.TurretProjectilePrefab?.GetComponent<Projectile>());
                bullet.transform.localScale = Vector3.one;

                if (i < turretProjectileSpawnPoints.Length)
                {
                    bullet.transform.SetParent(turretProjectileSpawnPoints[i]);
                }
                else
                {
                    bullet.transform.SetParent(turretProjectileSpawnPoints[0]);
                }

                bullet.transform.localPosition = Vector3.zero;

                if (_targetMonsters[i] != null)
                {
                    bullet.Init(_currentTurretTrainStatus.AttackDamage, _targetMonsters[i]);
                    bullet.transform.LookAt2D(_targetMonsters[i].transform);
                }
            }
        }

        #region DelayedDropAttack
        private void DelayedDropAttack()
        {
            if (_targetMonsters.Count == 0) return;

            Monster targetMonster = GetNearTargetMonster();
            if (targetMonster == null) return;

            for (int i = 0; i < _currentTurretTrainStatus.AttackCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Monster currentTarget = _targetMonsters[i];
                if (currentTarget == null) continue;

                Projectile bullet = ResourceManager.Instance.Spawn(turretTrainData.TurretProjectilePrefab?.GetComponent<Projectile>());
                if (bullet == null) continue;

                bullet.transform.localScale = Vector3.one;

                if (i < turretProjectileSpawnPoints.Length)
                {
                    bullet.transform.SetParent(turretProjectileSpawnPoints[i]);
                }
                else
                {
                    bullet.transform.SetParent(turretProjectileSpawnPoints[0]);
                }

                bullet.transform.localPosition = Vector3.zero;
                bullet.transform.localRotation = Quaternion.identity;

                // 지연 낙하 투사체 초기화
                bullet.Init(_currentTurretTrainStatus.AttackDamage, currentTarget);
                
                // 투사체가 DelayedDropMovement를 사용하도록 Config가 설정되어 있어야 함
                // 또는 여기서 직접 위치를 설정하고 투사체를 비활성화해두고, 코루틴으로 지연 후 활성화
            }
        }
        #endregion

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