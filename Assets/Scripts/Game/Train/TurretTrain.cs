using System.Collections.Generic;
using System.Linq;
using Cumic.Events;
using DG.Tweening;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Stats;
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
        private readonly List<Projectile> _nonMovementProjectiles = new();
        private bool _useNonMovementProjectilePooling;

        protected override void Setup()
        {
            base.Setup();

            _currentTurretTrainStatus = turretTrainData.TurretTrainStatus;

            GameEventSystem.Subscribe<WarningRemovedEvent>(OnWarningRemoved);

            InitializeProjectilePoolingMode();
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
            if (_currentTurretTrainStatus.AttackInterval <= 0)
            {
                _currentTurretTrainStatus.AttackInterval = turretTrainData.TurretTrainStatus.AttackInterval;
                return true;
            }
            else
            {
                _currentTurretTrainStatus.AttackInterval -= Time.deltaTime;
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
            _currentTurretTrainStatus.AttackInterval = turretTrainData.TurretTrainStatus.AttackInterval;

            if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
            {
                foreach (var projectile in _nonMovementProjectiles)
                {
                    if (projectile != null)
                    {
                        projectile.gameObject.SetActive(false);
                    }
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
            if (projectileData != null && projectileData.MovementType == MovementType.TargetPos)
            {
                TargetPosAttack();
                return;
            }

            if (isTargeting)
            {
                TargetedAttack();
            }
            else
            {
                NormalAttack();
            }
        }

        private void NormalAttack()
        {
            Monster nearTarget = GetNearTargetMonster();
            if (nearTarget == null) return;

            ProjectileData data = GetProjectile()?.GetData();
            bool hasWarning = data != null && data.HasWarning && data.WarningPrefab != null;

            for (int i = 0; i < _currentTurretTrainStatus.AttackCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                if (hasWarning && data.WarningDelaySeconds > 0f)
                {
                    GameEventSystem.Publish(new WarningEvent(
                        data.WarningPrefab,
                        nearTarget.transform.position,
                        data.WarningDelaySeconds,
                        nearTarget,
                        data.IsScaleByAttackRange ? _currentTurretTrainStatus.AttackRange : 0f,
                        data.IsScaleByAttackRange
                    ));
                }
                else
                {
                    Projectile projectile = SpawnNormalProjectile(i);
                    if (projectile != null)
                    {
                        projectile.transform.LookAt2D(nearTarget.transform);
                    }
                }
            }
        }

        private void TargetedAttack()
        {
            ProjectileData data = GetProjectile()?.GetData();
            bool hasWarning = data != null && data.HasWarning && data.WarningPrefab != null;

            for (int i = 0; i < _currentTurretTrainStatus.TargetCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Monster target = _targetMonsters[i];
                if (target == null) continue;

                if (hasWarning && data.WarningDelaySeconds > 0f)
                {
                    // Warning 프리팹 소환을 위한 이벤트 발행
                    Vector3 spawnPosition = turretProjectileSpawnPoints[i < turretProjectileSpawnPoints.Length ? i : 0].position;
                    GameEventSystem.Publish(new WarningEvent(
                        data.WarningPrefab,
                        spawnPosition,
                        data.WarningDelaySeconds,
                        target,
                        data.IsScaleByAttackRange ? _currentTurretTrainStatus.AttackRange : 0f,
                        data.IsScaleByAttackRange
                    ));
                }
                else
                {
                    Projectile projectile = SpawnNormalProjectile(i, target);
                    if (projectile != null)
                    {
                        projectile.transform.LookAt2D(target.transform);
                    }
                }
            }
        }

        #region TargetPosAttack
        private void TargetPosAttack()
        {
            // ProjectileData 가져오기
            Projectile projectilePrefab = GetProjectile();
            if (projectilePrefab == null) return;

            ProjectileData data = projectilePrefab.GetData();
            if (data == null) return;

            bool hasWarning = data.HasWarning && data.WarningPrefab != null;

            for (int i = 0; i < _currentTurretTrainStatus.AttackCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Monster currentTarget = _targetMonsters[i];
                if (currentTarget == null) continue;

                Vector3 targetPosition = currentTarget.transform.position;

                // Warning이 있고 딜레이가 있는 경우 WarningEvent 발행
                if (hasWarning && data.WarningDelaySeconds > 0f)
                {
                    GameEventSystem.Publish(new WarningEvent(
                        data.WarningPrefab,
                        targetPosition,
                        data.WarningDelaySeconds,
                        currentTarget,
                        data.IsScaleByAttackRange ? _currentTurretTrainStatus.AttackRange : 0f,
                        data.IsScaleByAttackRange
                    ));
                }
                else
                {
                    // 딜레이가 없으면 바로 타겟 위치에 Projectile 소환
                    SpawnProjectileAtWorldPosition(currentTarget, targetPosition);
                }
            }
        }

        /// <summary>
        /// Warning 딜레이 후 프로젝타일 발사 처리
        /// </summary>
        private void OnWarningRemoved(WarningRemovedEvent warningRemovedEvent)
        {
            if (warningRemovedEvent.Target == null || !warningRemovedEvent.Target.IsActive) return;

            ProjectileData data = GetProjectile()?.GetData();
            if (data == null) return;

            // MovementType에 따라 다른 방식으로 프로젝타일 소환
            if (data.MovementType == MovementType.TargetPos)
            {
                // TargetPos 타입: 타겟 위치에 프로젝타일 소환
                SpawnProjectileAtWorldPosition(warningRemovedEvent.Target, warningRemovedEvent.WorldPosition);
            }
            else
            {
                // Linear/NonMovement 타입: spawn point 위치에서 타겟을 향해 발사
                Projectile projectile = ResourceManager.Instance.Spawn(GetProjectile());
                if (projectile == null) return;

                // WarningRemovedEvent의 WorldPosition은 spawn point 위치
                projectile.transform.position = warningRemovedEvent.WorldPosition;
                projectile.transform.localScale = Vector3.one;
                projectile.transform.LookAt2D(warningRemovedEvent.Target.transform);

                InitializeProjectile(projectile, warningRemovedEvent.Target);
            }
        }

        /// <summary>
        /// World Position에 Projectile 소환 및 초기화 (TargetPosAttack 공통 로직)
        /// </summary>
        private void SpawnProjectileAtWorldPosition(Monster target, Vector3 worldPosition)
        {
            if (target == null || !target.IsActive) return;

            Projectile projectile = ResourceManager.Instance.Spawn(GetProjectile());
            if (projectile == null) return;

            SetupProjectileTransform(projectile, 0, worldPosition);
            InitializeProjectile(projectile, target);
        }

        #endregion

        #region NonMovement Projectile Pooling
        /// <summary>
        /// ProjectileData 가 NonMovement 인 경우, Turret 에서 발사하는 Projectile 을
        /// ResourceManager 풀 대신 Turret 단위에서 한 번만 소환해두고 active 로만 관리한다.
        /// (ParticleAttack 과 동일한 컨셉)
        /// </summary>
        private void InitializeProjectilePoolingMode()
        {
            Projectile projectile = GetProjectile();
            ProjectileData projectileData = projectile?.GetData();

            _useNonMovementProjectilePooling = projectileData?.MovementType == MovementType.NonMovement;

            if (_useNonMovementProjectilePooling)
            {
                // NonMovement 프로젝타일은 Setup 시점에 미리 생성
                PreCreateNonMovementProjectiles();
            }
        }

        /// <summary>
        /// NonMovement 프로젝타일을 미리 생성 (Setup 시점에 호출)
        /// </summary>
        private void PreCreateNonMovementProjectiles()
        {
            Projectile baseProjectile = GetProjectile();
            if (baseProjectile == null) return;

            // AttackCount와 TargetCount 중 큰 값만큼 미리 생성
            int maxCount = Mathf.Max(_currentTurretTrainStatus.AttackCount, _currentTurretTrainStatus.TargetCount);
            
            for (int i = 0; i < maxCount; i++)
            {
                Projectile spawned = ResourceManager.Instance.Spawn(baseProjectile);
                if (spawned == null) continue;

                spawned.gameObject.SetActive(false); // 실제 발사 시점에 활성화
                _nonMovementProjectiles.Add(spawned);
            }
        }

        /// <summary>
        /// NonMovement 프로젝타일 가져오기 (이미 생성된 것만 사용)
        /// </summary>
        private Projectile GetNonMovementProjectile(int index)
        {
            if (!_useNonMovementProjectilePooling)
            {
                return null;
            }

            // index가 범위를 벗어나면 null 반환 (이미 생성된 것만 사용)
            if (index < 0 || index >= _nonMovementProjectiles.Count)
            {
                return null;
            }

            return _nonMovementProjectiles[index];
        }

        /// <summary>
        /// AttackCount나 TargetCount가 증가했을 때 추가 프로젝타일 생성
        /// </summary>
        private void EnsureNonMovementProjectileCount(int requiredCount)
        {
            if (!_useNonMovementProjectilePooling) return;

            Projectile baseProjectile = GetProjectile();
            if (baseProjectile == null) return;

            // 필요한 개수만큼 추가 생성
            while (_nonMovementProjectiles.Count < requiredCount)
            {
                Projectile spawned = ResourceManager.Instance.Spawn(baseProjectile);
                if (spawned == null) break;

                spawned.gameObject.SetActive(false);
                _nonMovementProjectiles.Add(spawned);
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
            Projectile projectile = null;

            if (_useNonMovementProjectilePooling)
            {
                // NonMovement: 이미 생성된 프로젝타일 사용 (활성화만)
                projectile = GetNonMovementProjectile(index);
            }
            else
            {
                // 일반 프로젝타일: ResourceManager에서 소환
                projectile = ResourceManager.Instance.Spawn(GetProjectile());
            }

            if (projectile == null) return null;

            SetupProjectileTransform(projectile, index);

            Monster targetMonster = target ?? GetNearTargetMonster();
            InitializeProjectile(projectile, targetMonster);

            if (_useNonMovementProjectilePooling)
            {
                projectile.gameObject.SetActive(true);
            }

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
                _currentTurretTrainStatus.AttackInterval += turretUpgradeData.TurretStatusUpgrade.AttackInterval;

                if (_useNonMovementProjectilePooling)
                {
                    EnsureNonMovementProjectileCount(_currentTurretTrainStatus.AttackCount);
                }
            }
        }

        public override void StatusUpgrade(TurretTrainStatus upgradeData)
        {
            _currentTurretTrainStatus.AttackDamage += upgradeData.AttackDamage;
            _currentTurretTrainStatus.AttackRange += upgradeData.AttackRange;
            _currentTurretTrainStatus.AttackCount += upgradeData.AttackCount;
            _currentTurretTrainStatus.AttackInterval += upgradeData.AttackInterval;
            _currentTurretTrainStatus.TargetCount += upgradeData.TargetCount;

            if (_useNonMovementProjectilePooling)
            {
                int maxCount = Mathf.Max(_currentTurretTrainStatus.AttackCount, _currentTurretTrainStatus.TargetCount);
                EnsureNonMovementProjectileCount(maxCount);
            }
        }

        protected override void ApplyStat(IStat stat)
        {
            base.ApplyStat(stat);
            if (stat == null) return;

            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentTurretTrainStatus.AttackRange += stat.Value;
                    break;

                case StatType.AttackDamage:
                    _currentTurretTrainStatus.AttackDamage += Mathf.RoundToInt(stat.Value);
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                    {
                        foreach (var projectile in _nonMovementProjectiles)
                        {
                            if (projectile != null)
                            {
                                projectile.Init(
                                    _currentTurretTrainStatus.AttackDamage,
                                    null,
                                    projectile.IsScaleByAttackRange() ? _currentTurretTrainStatus.AttackRange : 0f
                                );
                            }
                        }
                    }
                    break;

                case StatType.AttackCount:
                    _currentTurretTrainStatus.AttackCount += Mathf.RoundToInt(stat.Value);
                    if (_useNonMovementProjectilePooling)
                    {
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.AttackCount);
                    }
                    break;

                case StatType.AttackInterval:
                    _currentTurretTrainStatus.AttackInterval += stat.Value;
                    break;

                case StatType.TargetCount:
                    _currentTurretTrainStatus.TargetCount += Mathf.RoundToInt(stat.Value);
                    if (_useNonMovementProjectilePooling)
                    {
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);
                    }
                    break;
            }
        }

        protected override void OnDead()
        {
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