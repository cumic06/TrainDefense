using System;
using System.Collections.Generic;
using System.Linq;
using Cumic;
using Cumic.Events;
using DG.Tweening;
using TrainDefense;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 포탑 공격(개별 투사체 발사) 동작을 캡슐화하는 컴포넌트. 직렬화 설정(spawn point/turret/model/flag)도 직접 보유.
    /// </summary>
    public class TurretAttackModule : MonoBehaviour, IAttackModule,
        IProjectileEmitter, IAttackEvents, IProjectileAttacker, IExternalProjectileSpawner, IForceAttacker
    {
        [SerializeField] private Transform[] turretProjectileSpawnPoints;
        [SerializeField] private bool useParticleProjectile;
        [SerializeField] private bool isTargeting = false;
        [SerializeField] private GameObject turret;
        [SerializeField] private GameObject turretModel;

        private Train _owner;
        private TurretTrainData _data;
        private bool _initialized;

        private TurretTrainStatus _currentTurretTrainStatus;
        private bool _isStatusInitialized;
        private List<Monster> _targetMonsters = new();
        private readonly List<Projectile> _nonMovementProjectiles = new();
        private bool _useNonMovementProjectilePooling;
        private int _attackCounter;
        private float _attackCountdown;
        private Vector3 _turretmodelScale;

        private readonly List<IProjectileModifier> _projectileModifiers = new();

        // ApplyStat 퍼센트 누적 손실 방지용 fractional accumulator (UtilMath.AccumulateIntDelta 참조)
        private float _statAttackDamageAccum;
        private float _statAttackCountAccum;
        private float _statTargetCountAccum;

        public event Action<Monster> OnAttacked;
        public event Action OnTargetPosAttacked;
        public Func<Vector3?> TargetPosOverride { get; set; }

        public TurretTrainStatus BaseStatus => _data != null ? _data.TurretTrainStatus : default;
        public TurretTrainStatus CurrentStatus => _currentTurretTrainStatus;
        public float CurrentAttackDamage => _currentTurretTrainStatus.AttackDamage;
        public float CurrentAttackRange => _currentTurretTrainStatus.AttackRange;
        public float RangeIndicatorRadius => _currentTurretTrainStatus.AttackRange;

        #region Lifecycle
        public void InitializeModule(Train owner)
        {
            _owner = owner;
            _data = owner != null ? owner.TrainData as TurretTrainData : null;
            if (_owner == null || _data == null) return;

            // 이미 초기화된 경우 업그레이드된 값이 덮어쓰이지 않도록 보호
            if (!_isStatusInitialized)
            {
                _currentTurretTrainStatus = _data.TurretTrainStatus;
                _attackCountdown = _currentTurretTrainStatus.AttackInterval;
                _isStatusInitialized = true;
            }

            InitializeProjectilePoolingMode();

            if (turretModel != null)
                _turretmodelScale = turretModel.transform.localScale;

            _initialized = true;
        }

        private void OnEnable()
        {
            GameEventSystem.Subscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
        }

        private void OnDisable()
        {
            GameEventSystem.Unsubscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
        }

        private void OnDestroy() => ClearAttachedProjectiles();

        private void FixedUpdate()
        {
            if (!_initialized || _owner == null || _owner.IsDead) return;

            DetectTarget();

            if (IsAttackDelayZero())
            {
                AttackHandler();
            }
        }

        private void _OnEngageReady(EngageReadyEvent _) => ClearAttachedProjectiles();

        private void _OnEngageStart(EngageStartEvent _)
        {
            if (!_initialized || !_useNonMovementProjectilePooling) return;
            PreCreateNonMovementProjectiles();
        }

        // 기차(또는 스폰포인트) 하위에 부착된 NonMovement 투사체(화염 파티클 등)를 풀로 반환한다.
        public void ClearAttachedProjectiles()
        {
            if (ResourceManager.Instance != null)
            {
                foreach (var proj in _nonMovementProjectiles)
                {
                    if (proj == null) continue;
                    // persistent 등록(터렛 소유)을 해제해야 공용 풀로 실제 반환된다
                    ResourceManager.Instance.UnregisterPersistent(proj.gameObject);
                    ResourceManager.Instance.Destroy(proj.gameObject);
                }
            }

            _nonMovementProjectiles.Clear();
        }
        #endregion

        #region Projectile Modifiers
        public void AddProjectileModifier(IProjectileModifier modifier)
        {
            if (modifier == null) return;
            _projectileModifiers.Add(modifier);
            // Order 오름차순 유지: 프리팹 선택(낮음) → 비주얼/스탯 수식(높음) 순으로 적용된다.
            _projectileModifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void RemoveProjectileModifier(IProjectileModifier modifier)
        {
            if (modifier != null) _projectileModifiers.Remove(modifier);
        }

        /// <summary>등록된 모디파이어들의 OverridePrefab을 Order 순으로 순회. 마지막 non-null이 최종 프리팹.</summary>
        private Projectile GetOverrideProjectile(ProjectileSpawnContext ctx)
        {
            Projectile prefab = null;
            for (int i = 0; i < _projectileModifiers.Count; i++)
            {
                var p = _projectileModifiers[i].OverridePrefab(ctx, prefab);
                if (p != null) prefab = p;
            }
            return prefab;
        }

        private void ApplyProjectileModifiers(ProjectileSpawnContext ctx, Projectile projectile)
        {
            for (int i = 0; i < _projectileModifiers.Count; i++)
                _projectileModifiers[i].Apply(ctx, projectile);
        }
        #endregion

        #region Attack Loop
        private void DetectTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(_owner.transform.position, _currentTurretTrainStatus.AttackRange);
            _targetMonsters = colliders.Where(a => a.GetComponent<Monster>() != null)
                .Select(a => a.GetComponent<Monster>())
                .OrderBy(x => _owner.transform.position.SqrDistance(x.transform.position))
                .ToList();
        }

        private Monster GetNearTargetMonster()
        {
            if (_targetMonsters.Count == 0) return null;
            return _targetMonsters.FirstOrDefault();
        }

        public Monster GetNearTargetMonsterPublic() => GetNearTargetMonster();

        private bool IsAttackDelayZero()
        {
            if (_attackCountdown <= 0)
            {
                // 쿨 리셋은 발사 성공 시(AttackHandler)에만 → 타겟 없으면 쿨 유지(헛돌지 않음)
                return true;
            }
            // FixedUpdate에서 호출되므로 fixedDeltaTime 사용
            _attackCountdown -= Time.fixedDeltaTime;
            return false;
        }

        private void AttackHandler()
        {
            if (_targetMonsters.Count == 0)
            {
                ResetTarget();
                if (_owner.TrainData.DamageType == DamageType.Tick)
                {
                    SoundManager.Instance.StopSFX(_data.AttackSoundType);
                }
                return;
            }

            Attack();
            _attackCountdown = _currentTurretTrainStatus.AttackInterval;
        }

        private void ResetTarget()
        {
            _targetMonsters.Clear();

            if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
            {
                foreach (var projectile in _nonMovementProjectiles)
                {
                    if (projectile != null)
                        projectile.gameObject.SetActive(false);
                }
            }
        }

        private void Attack()
        {
            Monster nearTarget = GetNearTargetMonster();
            if (nearTarget == null) return;

            _attackCounter++;
            OnAttacked?.Invoke(nearTarget);

            if (turretModel != null)
            {
                if (_owner.IsRotateTurret)
                    turret.transform.LookAt2D(nearTarget.transform);

                PlayAttackAnimation();
            }

            if (_data.AttackSoundType != SoundType.None && SoundManager.Instance != null)
            {
                if (_owner.TrainData.DamageType == DamageType.Direct)
                {
                    SoundManager.Instance.PlaySFX(_data.AttackSoundType);
                }
                else
                {
                    // 사전 생성 실패/풀 반환으로 리스트가 비거나 파괴된 참조가 남아도 발사가 막히지 않게 가드
                    Projectile firstPooled = _nonMovementProjectiles.FirstOrDefault(p => p != null);
                    if (firstPooled == null || !firstPooled.gameObject.activeSelf)
                    {
                        SoundManager.Instance.PlaySFX(_data.AttackSoundType, true);
                    }
                }
            }

            ProjectileData projectileData = GetProjectile().GetData();

            // 비주얼 먼저 소환 (DirectDamage로 적이 죽기 전에 방향 설정)
            if (projectileData != null && projectileData.MovementType == MovementType.TargetPos)
            {
                TargetPosAttack();
                if (projectileData == null || !projectileData.DirectDamage)
                    return;
            }
            else if (isTargeting)
            {
                TargetedAttack();
            }
            else
            {
                NormalAttack();
            }

            // DirectDamage: 비주얼 소환 후 즉석 데미지
            if (projectileData != null && projectileData.DirectDamage)
            {
                DirectDamageAttack(projectileData);
            }
        }

        public bool ForceAttack()
        {
            if (_owner == null || _owner.IsDead) return false;
            DetectTarget();
            if (_targetMonsters.Count == 0) return false;
            Attack();
            return true;
        }

        private void NormalAttack(Monster forcedTarget = null, Vector2? aimPosition = null)
        {
            Vector2 aimPos;
            if (aimPosition.HasValue)
            {
                aimPos = aimPosition.Value;
            }
            else
            {
                Monster nearTarget = forcedTarget != null ? forcedTarget : GetNearTargetMonster();
                if (nearTarget == null) return;
                aimPos = nearTarget.transform.position;
            }

            ProjectileData baseData = GetProjectile()?.GetData();
            float spreadAngle = baseData != null ? baseData.SpreadAngle : 0f;
            int count = _currentTurretTrainStatus.AttackCount;
            // 파티클 투사체(화염 등)는 스폰포인트가 포탑 중심에서 오프셋되어 있어 포탑 회전을 그대로 따라가도록 처리.
            bool alignToTurret = useParticleProjectile && _owner.IsRotateTurret && turret != null;

            for (int i = 0; i < count; i++)
            {
                Projectile projectile = SpawnNormalProjectile(i);
                if (projectile != null)
                {
                    if (alignToTurret)
                        projectile.transform.rotation = turret.transform.rotation;
                    else
                        projectile.transform.LookAt2D(aimPos);

                    if (spreadAngle > 0f && count > 1)
                    {
                        float offset = (i - (count - 1) * 0.5f) * spreadAngle;
                        projectile.transform.Rotate(0f, 0f, offset);
                    }
                }
            }
        }

        private void TargetedAttack()
        {
            int processCount = Mathf.Min(_currentTurretTrainStatus.TargetCount, _targetMonsters.Count);

            if (_useNonMovementProjectilePooling)
            {
                EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);

                for (int i = processCount; i < _nonMovementProjectiles.Count; i++)
                {
                    if (_nonMovementProjectiles[i] != null)
                        _nonMovementProjectiles[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < _currentTurretTrainStatus.TargetCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Monster target = _targetMonsters[i];
                if (target == null)
                {
                    if (_useNonMovementProjectilePooling && i < _nonMovementProjectiles.Count)
                        _nonMovementProjectiles[i]?.gameObject.SetActive(false);
                    continue;
                }

                Projectile projectile = SpawnNormalProjectile(i, target);
                if (projectile != null)
                    projectile.transform.LookAt2D(target.transform);
            }
        }

        #region DirectDamageAttack
        private const float BaseCriticalDamagePercent = 100f;

        private void DirectDamageAttack(ProjectileData projectileData)
        {
            int targetCount = Mathf.Max(_currentTurretTrainStatus.TargetCount, _currentTurretTrainStatus.AttackCount);

            for (int i = 0; i < targetCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Monster target = _targetMonsters[i];
                if (target == null || !target.IsActive) continue;

                var (finalDamage, isCritical) = CalculateDirectDamage();
                target.TakeDamage(finalDamage, isCritical);

                if (projectileData.HasStunEffect)
                    target.Stun(projectileData.StunDuration);
                if (projectileData.HasShoveEffect)
                    target.Shove(projectileData.ShovePower, projectileData.ShoveDuration);
                if (projectileData.HasSlowEffect)
                    target.Slow(projectileData.SlowValue, 0f);
            }
        }

        private (float finalDamage, bool isCritical) CalculateDirectDamage()
        {
            bool isCritical = _currentTurretTrainStatus.CriticalChance > 0f
                && UtilMath.CheckProbability(_currentTurretTrainStatus.CriticalChance);
            float finalDamage = _currentTurretTrainStatus.AttackDamage;
            if (isCritical)
            {
                finalDamage += _currentTurretTrainStatus.AttackDamage
                    * (BaseCriticalDamagePercent + _currentTurretTrainStatus.CriticalDamage) / 100f;
            }
            return (finalDamage, isCritical);
        }
        #endregion

        #region TargetPosAttack
        private void TargetPosAttack()
        {
            Projectile projectilePrefab = GetProjectile();
            if (projectilePrefab == null) return;

            ProjectileData data = projectilePrefab.GetData();
            if (data == null) return;

            for (int i = 0; i < _currentTurretTrainStatus.AttackCount; i++)
            {
                if (i >= _targetMonsters.Count) break;

                Monster currentTarget = _targetMonsters[i];
                if (currentTarget == null) continue;

                Vector3? overridePos = TargetPosOverride?.Invoke();
                if (overridePos.HasValue)
                    SpawnProjectileAtWorldPosition(null, overridePos.Value);
                else
                    SpawnProjectileAtWorldPosition(currentTarget, currentTarget.transform.position);
            }

            OnTargetPosAttacked?.Invoke();
        }

        private void SpawnProjectileAtWorldPosition(Monster target, Vector3 worldPosition)
        {
            if (target != null && !target.IsActive) return;

            Projectile projectile = null;

            if (_useNonMovementProjectilePooling)
            {
                projectile = _nonMovementProjectiles.FirstOrDefault(p => p != null && !p.gameObject.activeSelf);
                if (projectile == null)
                {
                    Projectile baseProjectile = GetProjectile();
                    if (baseProjectile != null)
                        projectile = CreatePooledNonMovementProjectile(baseProjectile);
                }
            }
            else
            {
                projectile = ResourceManager.Instance.Spawn(GetProjectile());
            }

            if (projectile == null) return;

            SetupProjectileTransform(projectile, 0, worldPosition);
            if (target != null)
                InitializeProjectile(projectile, target);
            else
                InitializeProjectileDamage(projectile);

            if (_useNonMovementProjectilePooling)
                projectile.gameObject.SetActive(true);
        }

        public void SpawnProjectileAtWorldPositionPublic(Monster target, Vector3 worldPosition, bool playSound = false)
        {
            // 무작위 위치 폭격 발사음 (Attack() 미경유로 무음 → 호출자가 빈도를 조절해 재생).
            if (playSound && _data.AttackSoundType != SoundType.None && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(_data.AttackSoundType);
            SpawnProjectileAtWorldPosition(target, worldPosition);
        }
        #endregion

        public void RepeatNormalAttack(Vector2? aimPosition = null)
        {
            if (_owner == null || _owner.IsDead) return;

            Monster nearTarget = null;
            if (!aimPosition.HasValue)
            {
                DetectTarget();
                if (_targetMonsters.Count == 0) return;
                nearTarget = GetNearTargetMonster();
                if (nearTarget == null) return;
            }

            if (turretModel != null)
            {
                if (_owner.IsRotateTurret)
                {
                    if (aimPosition.HasValue)
                        turret.transform.LookAt2D(aimPosition.Value);
                    else
                        turret.transform.LookAt2D(nearTarget.transform);
                }
                PlayAttackAnimation();
            }

            if (_data.AttackSoundType != SoundType.None && SoundManager.Instance != null)
            {
                if (_owner.TrainData.DamageType == DamageType.Direct)
                {
                    SoundManager.Instance.PlaySFX(_data.AttackSoundType);
                }
                else if (_nonMovementProjectiles.Count > 0 && !_nonMovementProjectiles[0].gameObject.activeSelf)
                {
                    SoundManager.Instance.PlaySFX(_data.AttackSoundType, true);
                }
            }

            NormalAttack(nearTarget, aimPosition);
        }

        private void PlayAttackAnimation()
        {
            var model = turretModel;
            if (model == null) return;
            model.transform.DOKill();
            model.transform.DOScale(_turretmodelScale * 0.9f, 0.1f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                model.transform.DOScale(_turretmodelScale, 0.1f).SetEase(Ease.InBack);
            });
        }

        public void SpawnExternalProjectile(Projectile prefab, float radius,
            IProjectileTarget target = null, float damageMul = 1f, float shoveScale = 1f)
            => SpawnExternalProjectileAtSelf(prefab, radius, damageMul, shoveScale, target as Monster);

        public void SpawnExternalProjectileAtSelf(Projectile prefab, float radius, float damageMul = 1f, float shoveScale = 1f, Monster target = null)
        {
            if (prefab == null || _owner == null) return;
            var spawned = ResourceManager.Instance.Spawn(prefab, _owner.transform.position, Quaternion.identity);
            if (spawned == null) return;
            float r = radius >= 0f ? radius : _currentTurretTrainStatus.AttackArea;
            Monster nearTarget = target != null ? target : GetNearTargetMonster();
            if (nearTarget != null) spawned.transform.LookAt2D(nearTarget.transform);
            int damage = Mathf.RoundToInt(_currentTurretTrainStatus.AttackDamage * damageMul);
            spawned.ShoveScale = shoveScale;
            spawned.Init(
                damage,
                _owner,
                nearTarget,
                r,
                _currentTurretTrainStatus.CriticalChance,
                _currentTurretTrainStatus.CriticalDamage);
        }
        #endregion

        #region NonMovement Pooling
        private void InitializeProjectilePoolingMode()
        {
            Projectile projectile = GetProjectile();
            ProjectileData projectileData = projectile?.GetData();

            _useNonMovementProjectilePooling = projectileData?.MovementType == MovementType.NonMovement;

            if (_useNonMovementProjectilePooling)
                PreCreateNonMovementProjectiles();
        }

        private void PreCreateNonMovementProjectiles()
        {
            Projectile baseProjectile = GetProjectile();
            if (baseProjectile == null) return;

            int maxCount = Mathf.Max(_currentTurretTrainStatus.AttackCount, _currentTurretTrainStatus.TargetCount);

            for (int i = 0; i < maxCount; i++)
            {
                if (CreatePooledNonMovementProjectile(baseProjectile) == null)
                    Debug.LogError($"TurretAttackModule: NonMovement 프로젝타일 사전 생성 실패 ({baseProjectile.name}, {i + 1}/{maxCount})");
            }
        }

        private Projectile CreatePooledNonMovementProjectile(Projectile baseProjectile)
        {
            Projectile spawned = ResourceManager.Instance.Spawn(baseProjectile);
            if (spawned == null) return null;

            ResourceManager.Instance.RegisterPersistent(spawned.gameObject);
            spawned.gameObject.SetActive(false);

            InitializeProjectileDamage(spawned);

            _nonMovementProjectiles.Add(spawned);
            return spawned;
        }

        private Projectile GetNonMovementProjectile(int index)
        {
            if (!_useNonMovementProjectilePooling) return null;
            if (index < 0 || index >= _nonMovementProjectiles.Count) return null;
            return _nonMovementProjectiles[index];
        }

        private void EnsureNonMovementProjectileCount(int requiredCount)
        {
            if (!_useNonMovementProjectilePooling) return;

            Projectile baseProjectile = GetProjectile();
            if (baseProjectile == null) return;

            while (_nonMovementProjectiles.Count < requiredCount)
            {
                Projectile spawned = ResourceManager.Instance.Spawn(baseProjectile);
                if (spawned == null) break;

                spawned.gameObject.SetActive(false);
                InitializeProjectileDamage(spawned);
                _nonMovementProjectiles.Add(spawned);
            }
        }
        #endregion

        #region Projectile Spawn Helpers
        private void SetupProjectileTransform(Projectile projectile, int spawnIndex, Vector3? worldPosition = null)
        {
            if (projectile == null) return;

            MovementType movementType = projectile.GetData().MovementType;

            if (movementType == MovementType.Linear || movementType == MovementType.TargetPos)
                projectile.transform.localScale = Vector3.one;

            if (worldPosition.HasValue)
            {
                if (movementType == MovementType.NonMovement)
                {
                    projectile.transform.SetParent(null);
                    projectile.transform.position = worldPosition.Value;
                    projectile.transform.localRotation = Quaternion.identity;

                    if (useParticleProjectile)
                        projectile.transform.localScale = Vector3.one;
                }
                else
                {
                    projectile.transform.position = worldPosition.Value;
                }
            }
            else
            {
                Transform parent = null;
                var spawnPoints = turretProjectileSpawnPoints;

                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    parent = spawnIndex < spawnPoints.Length
                        ? spawnPoints[spawnIndex]
                        : spawnPoints[0];
                }

                if (movementType == MovementType.NonMovement)
                {
                    if (parent != null)
                    {
                        projectile.transform.SetParent(parent);
                        projectile.transform.localPosition = Vector3.zero;
                        projectile.transform.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        projectile.transform.SetParent(_owner.transform);
                        projectile.transform.localPosition = Vector3.zero;
                        projectile.transform.localRotation = Quaternion.identity;
                    }

                    // 빔류는 회전·비균등 스케일된 spawn point의 자식이라, 루트 스케일을 부모 lossyScale 역수로 정규화.
                    if (projectile.IsScaleByArea() && !projectile.GetData().IsSpawnTriggerHandle && projectile.transform.parent != null)
                    {
                        Vector3 parentLossyScale = projectile.transform.parent.lossyScale;
                        projectile.transform.localScale = new Vector3(
                            Mathf.Approximately(parentLossyScale.x, 0f) ? 1f : 1f / parentLossyScale.x,
                            Mathf.Approximately(parentLossyScale.y, 0f) ? 1f : 1f / parentLossyScale.y,
                            Mathf.Approximately(parentLossyScale.z, 0f) ? 1f : 1f / parentLossyScale.z);
                    }

                    if (useParticleProjectile)
                        projectile.transform.localScale = Vector3.one;
                }
                else
                {
                    projectile.transform.position = parent != null ? parent.position : _owner.transform.position;
                }
            }
        }

        private void InitializeProjectile(Projectile projectile, Monster target)
        {
            if (projectile == null || target == null) return;

            projectile.Init(
                _currentTurretTrainStatus.AttackDamage,
                _owner,
                target,
                projectile.IsScaleByArea() ? _currentTurretTrainStatus.AttackArea : 0f,
                _currentTurretTrainStatus.CriticalChance,
                _currentTurretTrainStatus.CriticalDamage,
                projectile.IsScaleByArea() ? _currentTurretTrainStatus.AttackRange : 0f
            );
        }

        private void InitializeProjectileDamage(Projectile projectile)
        {
            if (projectile == null) return;

            projectile.Init(
                _currentTurretTrainStatus.AttackDamage,
                _owner,
                null,
                projectile.IsScaleByArea() ? _currentTurretTrainStatus.AttackArea : 0f,
                _currentTurretTrainStatus.CriticalChance,
                _currentTurretTrainStatus.CriticalDamage,
                projectile.IsScaleByArea() ? _currentTurretTrainStatus.AttackRange : 0f
            );
        }

        private Projectile SpawnNormalProjectile(int index, Monster target = null)
        {
            Projectile projectile = null;
            var ctx = new ProjectileSpawnContext { AttackIndex = _attackCounter, ProjectileIndex = index };

            // 모디파이어가 베이스 프리팹을 교체할 수 있음 (관통/광역 등). 있으면 우선 사용 (풀링 우회)
            Projectile overridePrefab = GetOverrideProjectile(ctx);
            if (overridePrefab != null)
            {
                projectile = ResourceManager.Instance.Spawn(overridePrefab);
            }
            else if (_useNonMovementProjectilePooling)
            {
                projectile = GetNonMovementProjectile(index);
            }
            else
            {
                projectile = ResourceManager.Instance.Spawn(GetProjectile());
            }

            if (projectile == null) return null;

            SetupProjectileTransform(projectile, index);

            Monster targetMonster = target ?? GetNearTargetMonster();
            InitializeProjectile(projectile, targetMonster);

            if (_useNonMovementProjectilePooling)
                projectile.gameObject.SetActive(true);

            // 합성 가능한 총알 수식 적용 (크기/넉백 등 — 여러 능력이 누적된다)
            ApplyProjectileModifiers(ctx, projectile);

            return projectile;
        }

        private Projectile GetProjectile()
        {
            return _data.TurretProjectilePrefab?.GetComponent<Projectile>();
        }
        #endregion

        #region Upgrade / Stats
        public void ApplyUpgrade(ITrainUpgradeData upgradeData, int prevLevel)
        {
            if (upgradeData is not TurretTrainUpgradeData turretUpgradeData) return;

            int upgradeLevelIndex = prevLevel + 1;
            var turretStatus = turretUpgradeData.GetTurretStatusUpgrade(upgradeLevelIndex);
            _currentTurretTrainStatus.AttackDamage += turretStatus.AttackDamage;
            _currentTurretTrainStatus.AttackRange += turretStatus.AttackRange;
            _currentTurretTrainStatus.AttackArea += turretStatus.AttackArea;
            _currentTurretTrainStatus.AttackCount += turretStatus.AttackCount;
            _currentTurretTrainStatus.AttackInterval += turretStatus.AttackInterval;
            _currentTurretTrainStatus.TargetCount += turretStatus.TargetCount;
            _currentTurretTrainStatus.CriticalChance += turretStatus.CriticalChance;
            _currentTurretTrainStatus.CriticalDamage += turretStatus.CriticalDamage;

            if (_useNonMovementProjectilePooling)
            {
                int maxCount = Mathf.Max(_currentTurretTrainStatus.AttackCount, _currentTurretTrainStatus.TargetCount);
                EnsureNonMovementProjectileCount(maxCount);

                if (_nonMovementProjectiles.Count > 0 && (turretStatus.AttackDamage != 0 || turretStatus.AttackArea != 0))
                {
                    foreach (var projectile in _nonMovementProjectiles)
                        if (projectile != null) InitializeProjectileDamage(projectile);
                }
            }
        }

        public void StatusUpgrade(TurretTrainStatus upgradeData)
        {
            _currentTurretTrainStatus.AttackDamage += upgradeData.AttackDamage;
            _currentTurretTrainStatus.AttackRange += upgradeData.AttackRange;
            _currentTurretTrainStatus.AttackArea += upgradeData.AttackArea;
            _currentTurretTrainStatus.AttackCount += upgradeData.AttackCount;
            _currentTurretTrainStatus.AttackInterval += upgradeData.AttackInterval;
            _currentTurretTrainStatus.TargetCount += upgradeData.TargetCount;
            _currentTurretTrainStatus.CriticalChance += upgradeData.CriticalChance;
            _currentTurretTrainStatus.CriticalDamage += upgradeData.CriticalDamage;

            if (_useNonMovementProjectilePooling)
            {
                int maxCount = Mathf.Max(_currentTurretTrainStatus.AttackCount, _currentTurretTrainStatus.TargetCount);
                EnsureNonMovementProjectileCount(maxCount);

                if (_nonMovementProjectiles.Count > 0 && (upgradeData.AttackDamage != 0 || upgradeData.AttackArea != 0))
                {
                    foreach (var projectile in _nonMovementProjectiles)
                        if (projectile != null) InitializeProjectileDamage(projectile);
                }
            }
        }

        public bool ApplyAttackStatByCurrentValue(IStat stat)
        {
            if (stat == null) return false;
            float percent = stat.Value / 100f;
            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentTurretTrainStatus.AttackRange += _currentTurretTrainStatus.AttackRange * percent;
                    return true;
                case StatType.AttackArea:
                    _currentTurretTrainStatus.AttackArea += _currentTurretTrainStatus.AttackArea * percent;
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                    {
                        float ratio = _currentTurretTrainStatus.AttackArea / _data.TurretTrainStatus.AttackArea;
                        foreach (var p in _nonMovementProjectiles) { if (p != null && !p.IsScaleByArea()) p.transform.localScale = new Vector3(ratio, ratio, 1f); }
                    }
                    return true;
                case StatType.AttackDamage:
                    _currentTurretTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, _currentTurretTrainStatus.AttackDamage * percent);
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                        foreach (var p in _nonMovementProjectiles) { if (p != null) InitializeProjectileDamage(p); }
                    return true;
                case StatType.AttackInterval:
                    _currentTurretTrainStatus.AttackInterval *= 1f / (1f + (-percent));
                    return true;
                default:
                    return false;
            }
        }

        public void ApplyAttackStat(IStat stat)
        {
            if (stat == null) return;
            var baseStatus = _data.TurretTrainStatus;
            float percent = stat.Value / 100f;
            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentTurretTrainStatus.AttackRange += baseStatus.AttackRange * percent;
                    break;

                case StatType.AttackArea:
                    _currentTurretTrainStatus.AttackArea += baseStatus.AttackArea * percent;
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                    {
                        float ratio = _currentTurretTrainStatus.AttackArea / baseStatus.AttackArea;
                        foreach (var projectile in _nonMovementProjectiles)
                            if (projectile != null && !projectile.IsScaleByArea()) projectile.transform.localScale = new Vector3(ratio, ratio, 1f);
                    }
                    break;

                case StatType.AttackDamage:
                    _currentTurretTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, stat.Value * _data.AttackDamageMultiplier);
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                        foreach (var projectile in _nonMovementProjectiles)
                            if (projectile != null) InitializeProjectileDamage(projectile);
                    break;

                case StatType.AttackCount:
                    _currentTurretTrainStatus.AttackCount += UtilMath.AccumulateIntDelta(ref _statAttackCountAccum, baseStatus.AttackCount * percent);
                    if (_useNonMovementProjectilePooling)
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.AttackCount);
                    break;

                case StatType.AttackInterval:
                    _currentTurretTrainStatus.AttackInterval += baseStatus.AttackInterval * percent;
                    break;

                case StatType.TargetCount:
                    _currentTurretTrainStatus.TargetCount += UtilMath.AccumulateIntDelta(ref _statTargetCountAccum, baseStatus.TargetCount * percent);
                    if (_useNonMovementProjectilePooling)
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);
                    break;

                case StatType.CriticalChance:
                    _currentTurretTrainStatus.CriticalChance += stat.Value;
                    break;

                case StatType.CriticalDamage:
                    _currentTurretTrainStatus.CriticalDamage += stat.Value;
                    break;
            }
        }

        public bool ApplyAttackStatLevelAware(IStat stat, int newLevel, int prevLevel)
        {
            if (stat == null) return false;
            var baseStatus = _data.TurretTrainStatus;
            float percent = stat.Value / 100f;
            int times = newLevel - prevLevel;

            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentTurretTrainStatus.AttackRange = _currentTurretTrainStatus.AttackRange / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    return true;

                case StatType.AttackArea:
                    _currentTurretTrainStatus.AttackArea = _currentTurretTrainStatus.AttackArea / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                    {
                        float ratio = _currentTurretTrainStatus.AttackArea / baseStatus.AttackArea;
                        foreach (var p in _nonMovementProjectiles)
                            if (p != null && !p.IsScaleByArea()) p.transform.localScale = new Vector3(ratio, ratio, 1f);
                    }
                    return true;

                case StatType.AttackDamage:
                    _currentTurretTrainStatus.AttackDamage = _currentTurretTrainStatus.AttackDamage / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                        foreach (var p in _nonMovementProjectiles)
                            if (p != null) InitializeProjectileDamage(p);
                    return true;

                case StatType.AttackCount:
                {
                    int newTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * newLevel);
                    int oldTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * prevLevel);
                    _currentTurretTrainStatus.AttackCount += newTot - oldTot;
                    if (_useNonMovementProjectilePooling)
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.AttackCount);
                    return true;
                }

                case StatType.AttackInterval:
                    _currentTurretTrainStatus.AttackInterval = _currentTurretTrainStatus.AttackInterval * (1f + (-percent) * prevLevel) / (1f + (-percent) * newLevel);
                    return true;

                case StatType.TargetCount:
                {
                    int newTot = Mathf.RoundToInt(baseStatus.TargetCount * percent * newLevel);
                    int oldTot = Mathf.RoundToInt(baseStatus.TargetCount * percent * prevLevel);
                    _currentTurretTrainStatus.TargetCount += newTot - oldTot;
                    if (_useNonMovementProjectilePooling)
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);
                    return true;
                }

                case StatType.CriticalChance:
                    _currentTurretTrainStatus.CriticalChance += stat.Value * times;
                    return true;

                case StatType.CriticalDamage:
                    _currentTurretTrainStatus.CriticalDamage += stat.Value * times;
                    return true;

                default:
                    return false;
            }
        }

        public void CopyProgressFrom(IAttackModule source)
        {
            if (source is not TurretAttackModule srcTurret) return;
            if (srcTurret._data == null || _data == null) return;

            var srcBase = srcTurret._data.TurretTrainStatus;
            var srcCurrent = srcTurret._currentTurretTrainStatus;
            var newBase = _data.TurretTrainStatus;

            _currentTurretTrainStatus.AttackDamage = newBase.AttackDamage + (srcCurrent.AttackDamage - srcBase.AttackDamage);
            _currentTurretTrainStatus.AttackRange = newBase.AttackRange + (srcCurrent.AttackRange - srcBase.AttackRange);
            _currentTurretTrainStatus.AttackArea = newBase.AttackArea + (srcCurrent.AttackArea - srcBase.AttackArea);
            _currentTurretTrainStatus.AttackCount = newBase.AttackCount + (srcCurrent.AttackCount - srcBase.AttackCount);
            _currentTurretTrainStatus.AttackInterval = newBase.AttackInterval + (srcCurrent.AttackInterval - srcBase.AttackInterval);
            _currentTurretTrainStatus.TargetCount = newBase.TargetCount + (srcCurrent.TargetCount - srcBase.TargetCount);
            _currentTurretTrainStatus.CriticalChance = newBase.CriticalChance + (srcCurrent.CriticalChance - srcBase.CriticalChance);
            _currentTurretTrainStatus.CriticalDamage = newBase.CriticalDamage + (srcCurrent.CriticalDamage - srcBase.CriticalDamage);

            _statAttackDamageAccum = srcTurret._statAttackDamageAccum;
            _statAttackCountAccum = srcTurret._statAttackCountAccum;
            _statTargetCountAccum = srcTurret._statTargetCountAccum;

            if (_useNonMovementProjectilePooling)
            {
                int maxCount = Mathf.Max(_currentTurretTrainStatus.AttackCount, _currentTurretTrainStatus.TargetCount);
                EnsureNonMovementProjectileCount(maxCount);
                foreach (var projectile in _nonMovementProjectiles)
                    if (projectile != null) InitializeProjectileDamage(projectile);
            }
        }
        #endregion

        #region Display / SpawnPoint (IAttackModule)
        public Transform GetSkillSpawnPoint(int index)
        {
            if (turretProjectileSpawnPoints == null || turretProjectileSpawnPoints.Length == 0) return null;
            if (index < 0) index = 0;
            if (index >= turretProjectileSpawnPoints.Length) index = turretProjectileSpawnPoints.Length - 1;
            return turretProjectileSpawnPoints[index];
        }

        public string GetStatSummary()
        {
            var s = _currentTurretTrainStatus;
            return $"DMG={s.AttackDamage} | RANGE={s.AttackRange} | AREA={s.AttackArea} | CNT={s.AttackCount} | TGT={s.TargetCount}";
        }

        public (string label, string value)[] GetStatDetailLines()
        {
            var s = _currentTurretTrainStatus;
            Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            var details = new List<(string label, string value)>
            {
                (L("Detail_Damage", "공격력"), $"{Mathf.RoundToInt(s.AttackDamage)}"),
                (L("Detail_Range", "사거리"), $"{s.AttackRange:F1}"),
            };
            if (s.AttackArea > 0f && _data != null && _data.UsesAttackArea)
                details.Add((L("Detail_Area", "범위"), $"{s.AttackArea:F1}"));
            details.Add((L("Detail_Speed", "공격속도"), $"{TrainStatLine.ToAttackSpeed(s.AttackInterval):F2}"));
            if (s.TargetCount > 1)
                details.Add((L("Detail_Targets", "대상 수"), $"{s.TargetCount}"));
            details.Add((L("Detail_CritChance", "크리티컬 확률"), $"{s.CriticalChance:F0}%"));
            details.Add((L("Detail_CritDamage", "크리티컬 데미지"), $"+{Projectile.BaseCriticalDamagePercent + s.CriticalDamage:F0}%"));
            return details.ToArray();
        }
        #endregion
    }
}
