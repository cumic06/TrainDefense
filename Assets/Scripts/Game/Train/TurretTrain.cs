using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cumic.Events;
using DG.Tweening;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
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
            GameEventSystem.Subscribe<WarningRemovedEvent>(OnWarningRemoved);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<WarningRemovedEvent>(OnWarningRemoved);
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

            ProjectileData projectileData = GetProjectile().GetData();
            if (projectileData != null && projectileData.MovementType == MovementType.DelayedDrop)
            {
                DelayedDropAttack();
                return;
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

                Projectile bullet = ResourceManager.Instance.Spawn(GetProjectile());
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

                if (bullet.IsScaleByAttackRange())
                {
                    bullet.Init(_currentTurretTrainStatus.AttackDamage, GetNearTargetMonster(), _currentTurretTrainStatus.AttackRange);
                }
                else
                {
                    bullet.Init(_currentTurretTrainStatus.AttackDamage, GetNearTargetMonster());
                }

                bullet.transform.LookAt2D(GetNearTargetMonster().transform);
            }
        }

        private void TargetedAttack()
        {
            for (int i = 0; i < _currentTurretTrainStatus.TargetCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Projectile projectile = ResourceManager.Instance.Spawn(GetProjectile());
                projectile.transform.localScale = Vector3.one;

                if (i < turretProjectileSpawnPoints.Length)
                {
                    projectile.transform.SetParent(turretProjectileSpawnPoints[i]);
                }
                else
                {
                    projectile.transform.SetParent(turretProjectileSpawnPoints[0]);
                }

                projectile.transform.localPosition = Vector3.zero;

                if (_targetMonsters[i] != null)
                {
                    if (projectile.IsScaleByAttackRange())
                    {
                        projectile.Init(_currentTurretTrainStatus.AttackDamage, _targetMonsters[i], _currentTurretTrainStatus.AttackRange);
                    }
                    else
                    {
                        projectile.Init(_currentTurretTrainStatus.AttackDamage, _targetMonsters[i]);
                    }
                    projectile.transform.LookAt2D(_targetMonsters[i].transform);
                }
            }
        }

        #region DelayedDropAttack
        private void DelayedDropAttack()
        {
            // ProjectileData 가져오기
            Projectile projectilePrefab = GetProjectile();
            if (projectilePrefab == null) return;

            ProjectileData data = projectilePrefab.GetData();
            if (data == null) return;

            float delaySeconds = data.DelaySeconds;

            for (int i = 0; i < _currentTurretTrainStatus.AttackCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Monster currentTarget = _targetMonsters[i];
                if (currentTarget == null) continue;

                Vector3 targetPosition = currentTarget.transform.position;

                // WarningObject가 있으면 이벤트 발행
                if (data.IsWarningProjectile && data.WarningObject != null)
                {
                    WarningEvent warningEvent = new WarningEvent(data.WarningObject, targetPosition, delaySeconds, currentTarget);
                    GameEventSystem.Publish(warningEvent);
                }
                else
                {
                    // Warning이 없으면 바로 Projectile 소환
                    StartCoroutine(DelayedProjectileSpawn(currentTarget, targetPosition, delaySeconds));
                }
            }
        }

        private void OnWarningRemoved(WarningRemovedEvent warningRemovedEvent)
        {
            // 타겟이 사라졌으면 Projectile 소환하지 않음
            if (warningRemovedEvent.Target == null || !warningRemovedEvent.Target.IsActive)
            {
                return;
            }

            Projectile projectile = ResourceManager.Instance.Spawn(GetProjectile());

            if (projectile == null)
            {
                return;
            }

            projectile.transform.position = warningRemovedEvent.WorldPosition;

            if (projectile.IsScaleByAttackRange())
            {
                projectile.Init(_currentTurretTrainStatus.AttackDamage, warningRemovedEvent.Target, _currentTurretTrainStatus.AttackRange);
            }
            else
            {
                projectile.Init(_currentTurretTrainStatus.AttackDamage, warningRemovedEvent.Target);
            }
        }

        private IEnumerator DelayedProjectileSpawn(Monster target, Vector3 spawnWorldPosition, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            // 타겟이 사라졌으면 Projectile 소환하지 않음
            if (target == null || !target.IsActive)
            {
                yield break;
            }

            Projectile projectile = ResourceManager.Instance.Spawn(GetProjectile());

            if (projectile == null)
            {
                yield break;
            }

            projectile.transform.position = spawnWorldPosition;

            if (projectile.IsScaleByAttackRange())
            {
                projectile.Init(_currentTurretTrainStatus.AttackDamage, target, _currentTurretTrainStatus.AttackRange);
            }
            else
            {
                projectile.Init(_currentTurretTrainStatus.AttackDamage, target);
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

        private Projectile GetProjectile()
        {
            return turretTrainData.TurretProjectilePrefab?.GetComponent<Projectile>();
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