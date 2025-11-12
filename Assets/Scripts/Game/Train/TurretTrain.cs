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
            .OrderBy(x => transform.position.SqrDistance(x.transform.position))
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
                    _particleProjectilePrefab.ReturnToPool();
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
            Monster nearTarget = GetNearTargetMonster();
            if (nearTarget == null) return;

            for (int i = 0; i < _currentTurretTrainStatus.AttackCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Projectile projectile = SpawnNormalProjectile(i);
                if (projectile != null)
                {
                    projectile.transform.LookAt2D(nearTarget.transform);
                }
            }
        }

        private void TargetedAttack()
        {
            for (int i = 0; i < _currentTurretTrainStatus.TargetCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Monster target = _targetMonsters[i];
                if (target == null) continue;

                Projectile projectile = SpawnNormalProjectile(i, target);
                if (projectile != null)
                {
                    projectile.transform.LookAt2D(target.transform);
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
                    WarningEvent warningEvent = new(data.WarningObject, targetPosition, delaySeconds, currentTarget);
                    GameEventSystem.Publish(warningEvent);
                }
                else
                {
                    // Warning이 없으면 바로 Projectile 소환
                    StartCoroutine(DelayedProjectileSpawn(currentTarget, targetPosition, delaySeconds));
                }
            }
        }

        /// <summary>
        /// World Position에 Projectile 소환 및 초기화 (DelayedDropAttack 공통 로직)
        /// </summary>
        private void SpawnProjectileAtWorldPosition(Monster target, Vector3 worldPosition)
        {
            if (target == null || !target.IsActive) return;

            Projectile projectile = ResourceManager.Instance.Spawn(GetProjectile());
            if (projectile == null) return;

            SetupProjectileTransform(projectile, 0, worldPosition);
            InitializeProjectile(projectile, target);
        }

        private void OnWarningRemoved(WarningRemovedEvent warningRemovedEvent)
        {
            SpawnProjectileAtWorldPosition(warningRemovedEvent.Target, warningRemovedEvent.WorldPosition);
        }

        private IEnumerator DelayedProjectileSpawn(Monster target, Vector3 spawnWorldPosition, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            SpawnProjectileAtWorldPosition(target, spawnWorldPosition);
        }
        #endregion

        #region ParticleAttack
        private void ParticleAttack()
        {
            ParticleProjectileSpawn();
            if (_particleProjectilePrefab != null)
            {
                _particleProjectilePrefab.Init(_currentTurretTrainStatus.AttackDamage);
            }
        }

        private void ParticleProjectileSpawn()
        {
            Projectile projectile = GetProjectile();

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

        #region Projectile Spawn Helpers
        /// <summary>
        /// Projectile의 Transform 설정 (Parent, Position, Scale, Rotation)
        /// </summary>
        private void SetupProjectileTransform(Projectile projectile, int spawnIndex, Vector3? worldPosition = null)
        {
            projectile.transform.localScale = Vector3.one;

            if (worldPosition.HasValue)
            {
                projectile.transform.position = worldPosition.Value;
            }
            else
            {
                Transform parent = spawnIndex < turretProjectileSpawnPoints.Length 
                    ? turretProjectileSpawnPoints[spawnIndex] 
                    : turretProjectileSpawnPoints[0];
                
                projectile.transform.SetParent(parent);
                projectile.transform.localPosition = Vector3.zero;
                projectile.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Projectile 초기화 (공통 로직)
        /// </summary>
        private void InitializeProjectile(Projectile projectile, Monster target)
        {
            if (projectile == null || target == null) return;

            projectile.Init(
                _currentTurretTrainStatus.AttackDamage, 
                target, 
                projectile.IsScaleByAttackRange() ? _currentTurretTrainStatus.AttackRange : 0f
            );
        }

        private Projectile SpawnNormalProjectile(int index, Monster target = null)
        {
            Projectile projectile = ResourceManager.Instance.Spawn(GetProjectile());
            if (projectile == null) return null;

            SetupProjectileTransform(projectile, index);

            Monster targetMonster = target ?? GetNearTargetMonster();
            InitializeProjectile(projectile, targetMonster);

            return projectile;
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