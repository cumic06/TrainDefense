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
    public class TurretTrain : Train, ITrainable, IProjectileEmitter
    {
        #region Field
        private TurretTrainData turretTrainData => _trainData as TurretTrainData;

        [SerializeField]
        private Transform[] turretProjectileSpawnPoints;
        [SerializeField]
        private bool useParticleProjectile;
        [SerializeField]
        private bool isTargeting = false;

        [SerializeField]
        private GameObject turret;

        [SerializeField]
        private GameObject turretModel;
        #endregion

        protected List<Monster> _targetMonsters = new();

        protected TurretTrainStatus _currentTurretTrainStatus;
        private bool _isStatusInitialized = false; // Setup이 이미 호출되었는지 추적
        private readonly List<Projectile> _nonMovementProjectiles = new();
        private bool _useNonMovementProjectilePooling;
        protected int _attackCounter;
        private Vector3 _turretmodelScale;

        // ApplyStat 퍼센트 누적 손실 방지용 fractional accumulator (UtilMath.AccumulateIntDelta 참조)
        private float _statAttackDamageAccum;
        private float _statAttackCountAccum;
        private float _statTargetCountAccum;

        // 합성 가능한 총알 수식자 파이프라인. 기존 "프리팹 통짜 교체 + 스칼라 덮어쓰기" 모델을 대체한다.
        private readonly List<IProjectileModifier> _projectileModifiers = new();
        private float _attackCountdown;

        public TurretTrainStatus BaseStatus => turretTrainData.TurretTrainStatus;
        public float CurrentAttackDamage => _currentTurretTrainStatus.AttackDamage;
        public override float CurrentAttackRange => _currentTurretTrainStatus.AttackRange;

        public event Action<Monster> OnAttacked;
        public event Action OnTargetPosAttacked;

        public Func<Vector3?> TargetPosOverride { get; set; }

        #region LifeCycle
        protected override void OnEnable()
        {
            base.OnEnable();
            GameEventSystem.Subscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
        }

        private void OnDisable()
        {
            GameEventSystem.Unsubscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearAttachedProjectiles();
        }
        #endregion

        #region Sub/UnSub
        private void _OnEngageReady(EngageReadyEvent _) => ClearAttachedProjectiles();

        // 기차(또는 스폰포인트) 하위에 부착된 NonMovement 투사체(화염 파티클 등)를 풀로 반환한다. (상점 진입/전투 준비 시 잔류 투사체 정리)
        public override void ClearAttachedProjectiles()
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

        private void _OnEngageStart(EngageStartEvent _)
        {
            if (!_useNonMovementProjectilePooling) return;

            PreCreateNonMovementProjectiles();
        }
        #endregion

        protected override void Setup()
        {
            base.Setup();

            // 이미 초기화된 경우 업그레이드된 값이 덮어쓰이지 않도록 보호
            if (!_isStatusInitialized)
            {
                _currentTurretTrainStatus = turretTrainData.TurretTrainStatus;
                _attackCountdown = _currentTurretTrainStatus.AttackInterval;
                _isStatusInitialized = true;
            }

            InitializeProjectilePoolingMode();

            if (turretModel != null)
                _turretmodelScale = turretModel.transform.localScale;
        }

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

        public Monster GetNearTargetMonsterPublic() => GetNearTargetMonster();

        public void RepeatNormalAttack(Vector2? aimPosition = null)
        {
            if (_isDead) return;

            // aimPosition이 있으면 재조준 없이 그 방향으로 발사 (연속 발사: 타겟이 죽어도 그 방향).
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
                if (isRotateTurret)
                {
                    if (aimPosition.HasValue)
                        turret.transform.LookAt2D(aimPosition.Value);
                    else
                        turret.transform.LookAt2D(nearTarget.transform);
                }
                PlayAttackAnimation();
            }

            if (turretTrainData.AttackSoundType != SoundType.None && SoundManager.Instance != null)
            {
                if (TrainData.DamageType == DamageType.Direct)
                {
                    SoundManager.Instance.PlaySFX(turretTrainData.AttackSoundType);
                }
                else if (_nonMovementProjectiles.Count > 0 && !_nonMovementProjectiles[0].gameObject.activeSelf)
                {
                    SoundManager.Instance.PlaySFX(turretTrainData.AttackSoundType, true);
                }
            }

            NormalAttack(nearTarget, aimPosition);
        }

        private void PlayAttackAnimation()
        {
            if (turretModel == null) return;
            turretModel.transform.DOKill();
            turretModel.transform.DOScale(_turretmodelScale * 0.9f, 0.1f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                turretModel.transform.DOScale(_turretmodelScale, 0.1f).SetEase(Ease.InBack);
            });
        }

        public void SpawnExternalProjectileAtSelf(Projectile prefab, float radius, float damageMul = 1f, float shoveScale = 1f, Monster target = null)
        {
            if (prefab == null) return;
            var spawned = ResourceManager.Instance.Spawn(prefab, transform.position, Quaternion.identity);
            if (spawned == null) return;
            float r = radius >= 0f ? radius : _currentTurretTrainStatus.AttackArea;
            // 새 총알(눈덩이 등)은 지정 타겟(없으면 기본 타겟팅)을 조준해서 발사한다.
            // NonMovement(폭발 등)는 방향/타겟이 무관하므로 조준해도 동작에 영향 없음.
            Monster nearTarget = target != null ? target : GetNearTargetMonster();
            if (nearTarget != null) spawned.transform.LookAt2D(nearTarget.transform);
            int damage = Mathf.RoundToInt(_currentTurretTrainStatus.AttackDamage * damageMul);
            spawned.ShoveScale = shoveScale;
            spawned.Init(
                damage,
                this,
                nearTarget,
                r,
                _currentTurretTrainStatus.CriticalChance,
                _currentTurretTrainStatus.CriticalDamage);
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

        protected void DetectTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _currentTurretTrainStatus.AttackRange);
            _targetMonsters = colliders.Where(a => a.GetComponent<Monster>() != null)
            .Select(a => a.GetComponent<Monster>())
            .OrderBy(x => transform.position.SqrDistance(x.transform.position))
            .ToList();
        }

        protected Monster GetNearTargetMonster()
        {
            if (_targetMonsters.Count == 0) return null;

            return _targetMonsters.FirstOrDefault();
        }

        private bool IsAttackDelayZero()
        {
            if (_attackCountdown <= 0)
            {
                // 쿨 리셋은 발사 성공 시(AttackHandler)에만 → 타겟 없으면 쿨 유지(헛돌지 않음)
                return true;
            }
            else
            {
                // FixedUpdate에서 호출되므로 fixedDeltaTime 사용
                _attackCountdown -= Time.fixedDeltaTime;
                return false;
            }
        }

        private void AttackHandler()
        {
            if (_targetMonsters.Count == 0)
            {
                ResetTarget();
                if (TrainData.DamageType == DamageType.Tick)
                {
                    SoundManager.Instance.StopSFX(turretTrainData.AttackSoundType);
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
                    {
                        projectile.gameObject.SetActive(false);
                    }
                }
            }
        }

        protected virtual void Attack()
        {
            Monster nearTarget = GetNearTargetMonster();
            if (nearTarget == null) return;

            _attackCounter++;
            OnAttacked?.Invoke(nearTarget);

            if (turretModel != null)
            {
                if (isRotateTurret)
                {
                    turret.transform.LookAt2D(nearTarget.transform);
                }

                PlayAttackAnimation();
            }

            if (turretTrainData.AttackSoundType != SoundType.None && SoundManager.Instance != null)
            {
                if (TrainData.DamageType == DamageType.Direct)
                {
                    SoundManager.Instance.PlaySFX(turretTrainData.AttackSoundType);
                }
                else
                {
                    // 사전 생성 실패/풀 반환으로 리스트가 비거나 파괴된 참조가 남아도 발사가 막히지 않게 가드
                    Projectile firstPooled = _nonMovementProjectiles.FirstOrDefault(p => p != null);
                    if (firstPooled == null || !firstPooled.gameObject.activeSelf)
                    {
                        SoundManager.Instance.PlaySFX(turretTrainData.AttackSoundType, true);
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

        /// <summary>
        /// 기존 엘리트 서브클래스용 후속타 훅. 신규 패시브는 OnAttacked 이벤트 사용.
        /// </summary>
        protected virtual void TryTriggerFollowUp(Monster target) { }

        /// <summary>
        /// 등록된 모디파이어들의 OverridePrefab을 Order 순으로 순회. 마지막 non-null이 최종 프리팹.
        /// (전부 null이면 베이스 프리팹/풀링 사용) 서브클래스가 추가 override 가능.
        /// </summary>
        protected virtual Projectile GetOverrideProjectile(ProjectileSpawnContext ctx)
        {
            Projectile prefab = null;
            for (int i = 0; i < _projectileModifiers.Count; i++)
            {
                var p = _projectileModifiers[i].OverridePrefab(ctx, prefab);
                if (p != null) prefab = p;
            }
            return prefab;
        }

        // 스폰된 투사체에 모든 모디파이어의 Apply를 누적 적용한다(크기/넉백 등이 합성됨).
        private void ApplyProjectileModifiers(ProjectileSpawnContext ctx, Projectile projectile)
        {
            for (int i = 0; i < _projectileModifiers.Count; i++)
                _projectileModifiers[i].Apply(ctx, projectile);
        }

        /// <summary>
        /// 스킬 InstantAttack용 외부 공개 래퍼.
        /// </summary>
        public bool ForceAttack()
        {
            if (_isDead) return false;
            DetectTarget();
            if (_targetMonsters.Count == 0) return false;
            Attack();
            return true;
        }

        protected virtual void NormalAttack(Monster forcedTarget = null, Vector2? aimPosition = null)
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
            // 파티클 투사체(화염 등)는 스폰포인트가 포탑 중심에서 오프셋되어 있어
            // 투사체 위치 기준 LookAt2D를 쓰면 가까운 타겟에서 포탑이 바라보는 방향과 어긋난다.
            // 포탑 회전을 그대로 따라가도록 처리해 시각적 정합성을 맞춘다.
            bool alignToTurret = useParticleProjectile && isRotateTurret && turret != null;

            for (int i = 0; i < count; i++)
            {
                Projectile projectile = SpawnNormalProjectile(i);
                if (projectile != null)
                {
                    if (alignToTurret)
                    {
                        projectile.transform.rotation = turret.transform.rotation;
                    }
                    else
                    {
                        projectile.transform.LookAt2D(aimPos);
                    }

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

            Debug.Log($"[TargetedAttack] TC={_currentTurretTrainStatus.TargetCount} mon={_targetMonsters.Count} pool={_nonMovementProjectiles.Count}");

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
                Debug.Log($"[TargetedAttack] i={i} target={target.name} proj={(projectile == null ? "NULL" : projectile.gameObject.name)} active={projectile?.gameObject.activeSelf}");
                if (projectile != null)
                {
                    projectile.transform.LookAt2D(target.transform);
                }
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
        protected virtual void TargetPosAttack()
        {
            // ProjectileData 가져오기
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
                {
                    SpawnProjectileAtWorldPosition(null, overridePos.Value);
                }
                else
                {
                    SpawnProjectileAtWorldPosition(currentTarget, currentTarget.transform.position);
                }
            }

            OnTargetPosAttacked?.Invoke();
        }

        /// <summary>
        /// World Position에 Projectile 소환 및 초기화 (TargetPosAttack 공통 로직)
        /// </summary>
        private void SpawnProjectileAtWorldPosition(Monster target, Vector3 worldPosition)
        {
            if (target != null && !target.IsActive) return;

            Projectile projectile = null;

            // NonMovement 프로젝타일인 경우 풀링 사용
            if (_useNonMovementProjectilePooling)
            {
                // 사용 가능한 비활성화된 프로젝타일 찾기
                projectile = _nonMovementProjectiles.FirstOrDefault(p => p != null && !p.gameObject.activeSelf);

                // 사용 가능한 프로젝타일이 없으면 새로 생성
                if (projectile == null)
                {
                    Projectile baseProjectile = GetProjectile();
                    if (baseProjectile != null)
                    {
                        projectile = CreatePooledNonMovementProjectile(baseProjectile);
                    }
                }
            }
            else
            {
                // 일반 프로젝타일: ResourceManager에서 소환
                projectile = ResourceManager.Instance.Spawn(GetProjectile());
            }

            if (projectile == null) return;

            SetupProjectileTransform(projectile, 0, worldPosition);
            if (target != null)
                InitializeProjectile(projectile, target);
            else
                InitializeProjectileDamage(projectile);

            if (_useNonMovementProjectilePooling)
            {
                projectile.gameObject.SetActive(true);
            }
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
                if (CreatePooledNonMovementProjectile(baseProjectile) == null)
                {
                    Debug.LogError($"TurretTrain: NonMovement 프로젝타일 사전 생성 실패 ({baseProjectile.name}, {i + 1}/{maxCount})");
                }
            }
        }

        /// <summary>
        /// 터렛 소유 NonMovement 프로젝타일 생성.
        /// ReturnAll(맵 이동/상점)이 회수해 공용 풀과 이중 소유가 되지 않도록 persistent로 등록한다.
        /// </summary>
        private Projectile CreatePooledNonMovementProjectile(Projectile baseProjectile)
        {
            Projectile spawned = ResourceManager.Instance.Spawn(baseProjectile);
            if (spawned == null) return null;

            ResourceManager.Instance.RegisterPersistent(spawned.gameObject);
            spawned.gameObject.SetActive(false); // 실제 발사 시점에 활성화

            // 생성 시점에 AttackDamage와 AttackRange 초기화
            InitializeProjectileDamage(spawned);

            _nonMovementProjectiles.Add(spawned);
            return spawned;
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

                // 생성 시점에 AttackDamage와 AttackRange 초기화
                InitializeProjectileDamage(spawned);

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
            if (projectile == null) return;

            MovementType movementType = projectile.GetData().MovementType;

            if (movementType == MovementType.Linear || movementType == MovementType.TargetPos)
            {
                projectile.transform.localScale = Vector3.one;
            }

            if (worldPosition.HasValue)
            {
                // NonMovement 타입인 경우 부모 해제 후 world position 설정
                if (movementType == MovementType.NonMovement)
                {
                    projectile.transform.SetParent(null);
                    projectile.transform.position = worldPosition.Value;
                    projectile.transform.localRotation = Quaternion.identity;

                    if (useParticleProjectile)
                    {
                        projectile.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    projectile.transform.position = worldPosition.Value;
                }
            }
            else
            {
                Transform parent = null;

                if (turretProjectileSpawnPoints != null && turretProjectileSpawnPoints.Length > 0)
                {
                    parent = spawnIndex < turretProjectileSpawnPoints.Length
                        ? turretProjectileSpawnPoints[spawnIndex]
                        : turretProjectileSpawnPoints[0];
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
                        projectile.transform.SetParent(transform);
                        projectile.transform.localPosition = Vector3.zero;
                        projectile.transform.localRotation = Quaternion.identity;
                    }

                    // 빔류는 회전·비균등 스케일된 spawn point의 자식이라, SetParent(worldPositionStays)가 매 발사 루트 스케일을 누적 왜곡한다.
                    // 루트 스케일을 부모 lossyScale의 역수로 정규화해 월드 스케일을 1로 고정한다(부모 스케일 상쇄, 빔 크기는 StretchBeamModel이 model로 전담).
                    if (projectile.IsScaleByArea() && !projectile.GetData().IsSpawnTriggerHandle && projectile.transform.parent != null)
                    {
                        Vector3 parentLossyScale = projectile.transform.parent.lossyScale;
                        projectile.transform.localScale = new Vector3(
                            Mathf.Approximately(parentLossyScale.x, 0f) ? 1f : 1f / parentLossyScale.x,
                            Mathf.Approximately(parentLossyScale.y, 0f) ? 1f : 1f / parentLossyScale.y,
                            Mathf.Approximately(parentLossyScale.z, 0f) ? 1f : 1f / parentLossyScale.z);
                    }

                    if (useParticleProjectile)
                    {
                        projectile.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    projectile.transform.position = parent != null ? parent.position : transform.position;
                }
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
                this,
                target,
                projectile.IsScaleByArea() ? _currentTurretTrainStatus.AttackArea : 0f,
                _currentTurretTrainStatus.CriticalChance,
                _currentTurretTrainStatus.CriticalDamage,
                projectile.IsScaleByArea() ? _currentTurretTrainStatus.AttackRange : 0f
            );
        }

        /// <summary>
        /// Projectile의 AttackDamage와 AttackArea만 초기화 (타겟 없이)
        /// NonMovement 프로젝타일 생성 시점에 사용
        /// </summary>
        private void InitializeProjectileDamage(Projectile projectile)
        {
            if (projectile == null) return;

            projectile.Init(
                _currentTurretTrainStatus.AttackDamage,
                this,
                null,
                projectile.IsScaleByArea() ? _currentTurretTrainStatus.AttackArea : 0f,
                _currentTurretTrainStatus.CriticalChance,
                _currentTurretTrainStatus.CriticalDamage,
                projectile.IsScaleByArea() ? _currentTurretTrainStatus.AttackRange : 0f
            );
        }

        protected Projectile SpawnNormalProjectile(int index, Monster target = null)
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

            // 합성 가능한 총알 수식 적용 (크기/넉백 등 — 여러 능력이 누적된다)
            ApplyProjectileModifiers(ctx, projectile);

            return projectile;
        }
        #endregion

        public override void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            int currentLevel = CurrentLevel; // 업그레이드 전 레벨 저장
            base.Upgrade(upgradeData);

            // TurretTrain 전용 업그레이드 데이터가 있다면 적용
            // Train 초기 레벨은 -1, upgradeStats 배열은 0부터 시작
            // 업그레이드 적용 시: 업그레이드 전 레벨 + 1 인덱스 사용
            if (upgradeData is TurretTrainUpgradeData turretUpgradeData)
            {
                int upgradeLevelIndex = currentLevel + 1;
                var turretStatus = turretUpgradeData.GetTurretStatusUpgrade(upgradeLevelIndex);
                _currentTurretTrainStatus.AttackDamage += turretStatus.AttackDamage;
                _currentTurretTrainStatus.AttackRange += turretStatus.AttackRange;
                _currentTurretTrainStatus.AttackArea += turretStatus.AttackArea;
                _currentTurretTrainStatus.AttackCount += turretStatus.AttackCount;
                _currentTurretTrainStatus.AttackInterval += turretStatus.AttackInterval;
                _currentTurretTrainStatus.TargetCount += turretStatus.TargetCount;
                _currentTurretTrainStatus.CriticalChance += turretStatus.CriticalChance;
                _currentTurretTrainStatus.CriticalDamage += turretStatus.CriticalDamage;

                var passiveId = turretUpgradeData.GetPassiveSkillDataId(upgradeLevelIndex);
                if (!string.IsNullOrEmpty(passiveId))
                {
                    var passiveData = DatabaseManager.Instance.GetDB().TrainSkillDataDB.trainPassiveSkillDataList
                        .Find(s => s != null && s.Id == passiveId);
                    _skillModule.RegisterPassiveFromData(passiveData);
                }

                if (_useNonMovementProjectilePooling)
                {
                    int maxCount = Mathf.Max(_currentTurretTrainStatus.AttackCount, _currentTurretTrainStatus.TargetCount);
                    EnsureNonMovementProjectileCount(maxCount);

                    // AttackDamage나 AttackArea 변경 시 기존 프로젝타일 업데이트
                    if (_nonMovementProjectiles.Count > 0 && (turretStatus.AttackDamage != 0 || turretStatus.AttackArea != 0))
                    {
                        foreach (var projectile in _nonMovementProjectiles)
                        {
                            if (projectile != null)
                            {
                                InitializeProjectileDamage(projectile);
                            }
                        }
                    }
                }
            }
        }

        public override void StatusUpgrade(TurretTrainStatus upgradeData)
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

                // AttackDamage나 AttackArea 변경 시 기존 프로젝타일 업데이트
                if (_nonMovementProjectiles.Count > 0 && (upgradeData.AttackDamage != 0 || upgradeData.AttackArea != 0))
                {
                    foreach (var projectile in _nonMovementProjectiles)
                    {
                        if (projectile != null)
                        {
                            InitializeProjectileDamage(projectile);
                        }
                    }
                }
            }
        }

        public override string GetStatSummary() =>
            $"DMG={_currentTurretTrainStatus.AttackDamage} | RANGE={_currentTurretTrainStatus.AttackRange} | AREA={_currentTurretTrainStatus.AttackArea} | CNT={_currentTurretTrainStatus.AttackCount} | TGT={_currentTurretTrainStatus.TargetCount} | MaxHp={_currentMaxHp}";

        public override (string label, string value)[] GetStatDetails()
        {
            System.Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            var details = new System.Collections.Generic.List<(string label, string value)>
            {
                (L("Detail_HP", "HP"), $"{Mathf.RoundToInt(_currentMaxHp)}"),
                (L("Detail_Damage", "공격력"), $"{Mathf.RoundToInt(_currentTurretTrainStatus.AttackDamage)}"),
                (L("Detail_Range", "사거리"), $"{_currentTurretTrainStatus.AttackRange:F1}"),
            };

            // 범위(AttackArea)는 실제로 폭발 반경으로 쓰는 포탑만 표시. (선택 카드와 동일 조건)
            if (_currentTurretTrainStatus.AttackArea > 0f && turretTrainData != null && turretTrainData.UsesAttackArea)
                details.Add((L("Detail_Area", "범위"), $"{_currentTurretTrainStatus.AttackArea:F1}"));

            details.Add((L("Detail_Speed", "공격속도"), $"{ToAttackSpeed(_currentTurretTrainStatus.AttackInterval):F2}"));

            // 대상 수는 다중 타겟 포탑만 표시. (선택 카드와 동일 조건)
            if (_currentTurretTrainStatus.TargetCount > 1)
                details.Add((L("Detail_Targets", "대상 수"), $"{_currentTurretTrainStatus.TargetCount}"));

            details.Add((L("Detail_CritChance", "크리티컬 확률"), $"{_currentTurretTrainStatus.CriticalChance:F0}%"));
            details.Add((L("Detail_CritDamage", "크리티컬 데미지"), $"+{Projectile.BaseCriticalDamagePercent + _currentTurretTrainStatus.CriticalDamage:F0}%"));
            return details.ToArray();
        }

        public override void ApplyPassiveSkills()
        {
            var passives = turretTrainData?.PassiveSkillDatas;
            if (passives == null) return;
            // 삼중택일은 픽한 스킬 1개만 부여한다: Active 픽=패시브 미적용, Passive 픽=선택 1개,
            // None(일반 스폰)=기본 키트 전부. (Train._IsPassiveApplied 공용 규칙)
            foreach (var p in passives)
                if (_IsPassiveApplied(p.Id))
                    _skillModule.RegisterPassiveFromData(p);
        }

        public override void ApplyStatsByCurrentValue(IStat[] stats)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
            {
                if (stat == null) continue;
                float percent = stat.Value / 100f;
                switch (stat.Type)
                {
                    case StatType.AttackRange:
                        _currentTurretTrainStatus.AttackRange += _currentTurretTrainStatus.AttackRange * percent;
                        break;
                    case StatType.AttackArea:
                        _currentTurretTrainStatus.AttackArea += _currentTurretTrainStatus.AttackArea * percent;
                        if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                        {
                            float ratio = _currentTurretTrainStatus.AttackArea / turretTrainData.TurretTrainStatus.AttackArea;
                            // ScaleByArea(빔 등)는 Projectile.StretchBeamModel이 크기를 전담 → root 스케일 제외(이중 스케일 방지).
                            foreach (var p in _nonMovementProjectiles) { if (p != null && !p.IsScaleByArea()) p.transform.localScale = new Vector3(ratio, ratio, 1f); }
                        }
                        break;
                    case StatType.AttackDamage:
                        _currentTurretTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, _currentTurretTrainStatus.AttackDamage * percent);
                        if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                            foreach (var p in _nonMovementProjectiles) { if (p != null) InitializeProjectileDamage(p); }
                        break;
                    case StatType.AttackInterval:
                        // 공속은 상점과 동일하게 현재값 기준 역수 곱셈(DPS 선형, 0 이하 방지). percent 음수=공속 증가.
                        _currentTurretTrainStatus.AttackInterval *= 1f / (1f + (-percent));
                        break;
                    default:
                        ApplyStat(stat);
                        break;
                }
            }
        }

        protected override void ApplyStat(IStat stat)
        {
            base.ApplyStat(stat);
            if (stat == null) return;

            var baseStatus = turretTrainData.TurretTrainStatus;
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
                        // ScaleByArea(빔 등)는 Projectile.StretchBeamModel이 크기를 전담 → root 스케일 제외(이중 스케일 방지).
                        foreach (var projectile in _nonMovementProjectiles)
                            if (projectile != null && !projectile.IsScaleByArea()) projectile.transform.localScale = new Vector3(ratio, ratio, 1f);
                    }
                    break;

                case StatType.AttackDamage:
                    _currentTurretTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, stat.Value * turretTrainData.AttackDamageMultiplier);

                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                    {
                        foreach (var projectile in _nonMovementProjectiles)
                        {
                            if (projectile != null)
                            {
                                InitializeProjectileDamage(projectile);
                            }
                        }
                    }
                    break;

                case StatType.AttackCount:
                    _currentTurretTrainStatus.AttackCount += UtilMath.AccumulateIntDelta(ref _statAttackCountAccum, baseStatus.AttackCount * percent);
                    if (_useNonMovementProjectilePooling)
                    {
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.AttackCount);
                    }
                    break;

                case StatType.AttackInterval:
                    _currentTurretTrainStatus.AttackInterval += baseStatus.AttackInterval * percent;
                    break;

                case StatType.TargetCount:
                    _currentTurretTrainStatus.TargetCount += UtilMath.AccumulateIntDelta(ref _statTargetCountAccum, baseStatus.TargetCount * percent);
                    if (_useNonMovementProjectilePooling)
                    {
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);
                    }
                    break;

                case StatType.CriticalChance:
                    _currentTurretTrainStatus.CriticalChance += stat.Value;
                    break;

                case StatType.CriticalDamage:
                    _currentTurretTrainStatus.CriticalDamage += stat.Value;
                    break;
            }
        }

        public override void ApplyStatsLevelAware(IStat[] stats, int newLevel, int prevLevel = 0)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
                ApplyStatLevelAware(stat, newLevel, prevLevel);
        }

        protected override void ApplyStatLevelAware(IStat stat, int newLevel, int prevLevel)
        {
            if (stat == null) return;

            var baseStatus = turretTrainData.TurretTrainStatus;
            float percent = stat.Value / 100f;
            int times = newLevel - prevLevel;

            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _currentTurretTrainStatus.AttackRange = _currentTurretTrainStatus.AttackRange / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    break;

                case StatType.AttackArea:
                    _currentTurretTrainStatus.AttackArea = _currentTurretTrainStatus.AttackArea / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                    {
                        float ratio = _currentTurretTrainStatus.AttackArea / baseStatus.AttackArea;
                        // ScaleByArea(빔 등)는 Projectile.StretchBeamModel이 크기를 전담 → root 스케일 제외(이중 스케일 방지).
                        foreach (var p in _nonMovementProjectiles)
                            if (p != null && !p.IsScaleByArea()) p.transform.localScale = new Vector3(ratio, ratio, 1f);
                    }
                    break;

                case StatType.AttackDamage:
                {
                    _currentTurretTrainStatus.AttackDamage = _currentTurretTrainStatus.AttackDamage / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                        foreach (var p in _nonMovementProjectiles)
                            if (p != null) InitializeProjectileDamage(p);
                    break;
                }

                case StatType.AttackCount:
                {
                    int newTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * newLevel);
                    int oldTot = Mathf.RoundToInt(baseStatus.AttackCount * percent * prevLevel);
                    _currentTurretTrainStatus.AttackCount += newTot - oldTot;
                    if (_useNonMovementProjectilePooling)
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.AttackCount);
                    break;
                }

                case StatType.AttackInterval:
                    _currentTurretTrainStatus.AttackInterval = _currentTurretTrainStatus.AttackInterval * (1f + (-percent) * prevLevel) / (1f + (-percent) * newLevel);
                    break;

                case StatType.TargetCount:
                {
                    int newTot = Mathf.RoundToInt(baseStatus.TargetCount * percent * newLevel);
                    int oldTot = Mathf.RoundToInt(baseStatus.TargetCount * percent * prevLevel);
                    _currentTurretTrainStatus.TargetCount += newTot - oldTot;
                    if (_useNonMovementProjectilePooling)
                        EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);
                    break;
                }

                case StatType.CriticalChance:
                    _currentTurretTrainStatus.CriticalChance += stat.Value * times;
                    break;

                case StatType.CriticalDamage:
                    _currentTurretTrainStatus.CriticalDamage += stat.Value * times;
                    break;

                default:
                    base.ApplyStatLevelAware(stat, newLevel, prevLevel);
                    break;
            }
        }

        public override void CopyProgressFrom(Train source)
        {
            base.CopyProgressFrom(source);
            if (source is not TurretTrain srcTurret) return;
            if (srcTurret.turretTrainData == null || turretTrainData == null) return;

            var srcBase = srcTurret.turretTrainData.TurretTrainStatus;
            var srcCurrent = srcTurret._currentTurretTrainStatus;
            var newBase = turretTrainData.TurretTrainStatus;

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
                {
                    if (projectile != null) InitializeProjectileDamage(projectile);
                }
            }
        }

        protected virtual Projectile GetProjectile()
        {
            return turretTrainData.TurretProjectilePrefab?.GetComponent<Projectile>();
        }

        public void SpawnProjectileAtWorldPositionPublic(Monster target, Vector3 worldPosition, bool playSound = false)
        {
            // 무작위 위치 폭격 발사음 (Attack() 미경유로 무음 → 호출자가 빈도를 조절해 재생).
            if (playSound && turretTrainData.AttackSoundType != SoundType.None && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(turretTrainData.AttackSoundType);
            SpawnProjectileAtWorldPosition(target, worldPosition);
        }

        public override Transform GetSkillSpawnPoint(int index)
        {
            if (turretProjectileSpawnPoints == null || turretProjectileSpawnPoints.Length == 0)
            {
                return base.GetSkillSpawnPoint(index);
            }

            if (index < 0)
            {
                index = 0;
            }

            if (index >= turretProjectileSpawnPoints.Length)
            {
                index = turretProjectileSpawnPoints.Length - 1;
            }

            return turretProjectileSpawnPoints[index] != null
                ? turretProjectileSpawnPoints[index]
                : base.GetSkillSpawnPoint(index);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (turretTrainData != null)
            {
                Gizmos.DrawWireSphere(transform.position, _currentTurretTrainStatus.AttackRange);
            }
        }
    }
}
