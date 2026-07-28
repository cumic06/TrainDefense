using System;
using System.Collections;
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
    public class TurretTrain : Train, ITrainable
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

        public delegate Projectile ProjectileOverrideProvider(int attackIndex);
        private readonly List<ProjectileOverrideProvider> _projectileOverrides = new();
        private float _attackCountdown;
        // 버스트(화염) 남은 분사 시간. > 0이면 분사 유지 중이고 쿨다운은 멈춰 있다(종료 후부터 흐름).
        private float _burstRemaining;

        // 공격 횟수(AttackCount) 반복 간격 = AttackInterval × 이 비율. 공속이 빨라질수록 연타 간격도 조여진다.
        private const float REPEAT_ATTACK_DELAY_RATIO = 0.15f;
        private Coroutine _repeatAttackCoroutine;

        public TurretTrainStatus BaseStatus => turretTrainData.TurretTrainStatus;
        public float CurrentAttackDamage => _currentTurretTrainStatus.AttackDamage;
        public override float CurrentAttackRange => _currentTurretTrainStatus.AttackRange;
        // 공격 간격(초). MainTrain이 터치 연속 발사의 쿨다운으로 사용한다.
        public float AttackInterval => _currentTurretTrainStatus.AttackInterval;

        // MainTrain 주무기로 장착되면 true. 자동 적 탐지/발사를 끄고, MainTrain의 터치 조준 발사만 받는다.
        public bool ManualAimMode { get; set; }

        // 경량 Turret(MainTrain 주무기)이 비주얼(회전 pivot·모델·스폰포인트)만 복제해 재사용하기 위한 읽기 전용 노출.
        public GameObject TurretVisualRoot => turret;
        public GameObject TurretModelNode => turretModel;
        public Transform[] ProjectileSpawnPointNodes => turretProjectileSpawnPoints;

        public float ProjectileScale { get; set; } = 1f;
        // 오버라이드 투사체(폭발탄 등)에만 곱할 데미지 배율. Provider가 프리팹 반환 시 세팅하고 그 발사에서만 적용된다.
        public float OverrideDamageMultiplier { get; set; } = 1f;
        public float ProjectileKnockbackPower { get; set; }
        public float ProjectileKnockbackDuration { get; set; }

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

        protected override void OnDead()
        {
            base.OnDead();

            if (IsUnDead) return;

            // 사망 시 FixedUpdate(자동 발사·ResetTarget)가 멈춰 마지막에 깔린 NonMovement 장판(냉기/화염 지속 영역)이 그대로 남는다.
            // 죽은 기차는 회색으로 씬에 남아(Destroy 안 됨) OnDestroy 정리도 타지 않으므로 즉시 끈다.
            _DeactivateNonMovementProjectiles();
            // 분사 도중 죽으면 잔여 버스트가 남아 부활 직후 그 시간만큼 공격 불능이 되므로 함께 리셋한다.
            _burstRemaining = 0f;
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
            // 분사 도중 상점 진입 시 잔여 버스트가 다음 전투 시작을 막지 않도록 리셋.
            _burstRemaining = 0f;
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
                _ApplyPermanentUpgrade();
                _attackCountdown = _currentTurretTrainStatus.AttackInterval;
                _isStatusInitialized = true;
            }

            InitializeProjectilePoolingMode();

            if (turretModel != null)
                _turretmodelScale = turretModel.transform.localScale;
        }

        // 영구(메타) 업그레이드 + 스킬트리의 TurretStat 보너스를 base 스탯에 가산한다. (struct라 값 복사 후 직접 가산)
        private void _ApplyPermanentUpgrade()
        {
            var manager = PermanentUpgradeManager.Instance;

            if (manager != null)
            {
                _currentTurretTrainStatus.AttackDamage += manager.GetBonus(StatType.AttackDamage);
                _currentTurretTrainStatus.AttackRange += manager.GetBonus(StatType.AttackRange);
                _currentTurretTrainStatus.AttackArea += manager.GetBonus(StatType.AttackArea);
                _currentTurretTrainStatus.AttackInterval += manager.GetBonus(StatType.AttackInterval);
                _currentTurretTrainStatus.CriticalChance += manager.GetBonus(StatType.CriticalChance);
                _currentTurretTrainStatus.CriticalDamage += manager.GetBonus(StatType.CriticalDamage);
                _currentTurretTrainStatus.AttackCount += Mathf.RoundToInt(manager.GetBonus(StatType.AttackCount));
                _currentTurretTrainStatus.TargetCount += Mathf.RoundToInt(manager.GetBonus(StatType.TargetCount));
            }

            var skillTreeManager = SkillTreeManager.Instance;

            if (skillTreeManager != null)
            {
                _currentTurretTrainStatus.AttackDamage += skillTreeManager.GetBonus(StatType.AttackDamage);
                _currentTurretTrainStatus.AttackRange += skillTreeManager.GetBonus(StatType.AttackRange);
                _currentTurretTrainStatus.AttackArea += skillTreeManager.GetBonus(StatType.AttackArea);
                _currentTurretTrainStatus.AttackInterval += skillTreeManager.GetBonus(StatType.AttackInterval);
                _currentTurretTrainStatus.CriticalChance += skillTreeManager.GetBonus(StatType.CriticalChance);
                _currentTurretTrainStatus.CriticalDamage += skillTreeManager.GetBonus(StatType.CriticalDamage);
                _currentTurretTrainStatus.AttackCount += Mathf.RoundToInt(skillTreeManager.GetBonus(StatType.AttackCount));
                _currentTurretTrainStatus.TargetCount += Mathf.RoundToInt(skillTreeManager.GetBonus(StatType.TargetCount));
            }
        }

        public void RegisterProjectileOverride(ProjectileOverrideProvider provider)
        {
            if (provider != null) _projectileOverrides.Add(provider);
        }

        public void UnregisterProjectileOverride(ProjectileOverrideProvider provider)
        {
            if (provider != null) _projectileOverrides.Remove(provider);
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

            TurretCombatFx.PlayAttackPunch(turretModel.transform, _turretmodelScale);
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
            // 수동 조준 모드(MainTrain 주무기)는 자동 탐지/발사를 하지 않는다. MainTrain이 터치 입력으로 RepeatNormalAttack을 직접 호출한다.
            if (ManualAimMode) return;

            DetectTarget();

            // 타겟이 있으면 공격과 무관하게 계속 조준 방향으로 회전한다. (발사 순간에만 돌면 끊겨 보임)
            _RotateTowardNearTarget();

            // 버스트(화염): 발동 후 지속시간 동안 분사를 유지한다(타겟이 빠져도 시간이 다할 때까지 계속 뿜음).
            // 버스트 중엔 여기서 return하므로 쿨다운(IsAttackDelayZero)이 멈춰 있다가 종료 후부터 흐른다.
            if (_burstRemaining > 0f)
            {
                _burstRemaining -= Time.fixedDeltaTime;
                if (_burstRemaining <= 0f)
                    _EndBurst();
                return;
            }

            // 회전 중에 발사되면 총구와 다른 방향으로 나가 어색해서, 타겟 방향을 (거의) 바라볼 때만 발사한다.
            if (IsAttackDelayZero() && _IsAimedAtNearTarget())
            {
                AttackHandler();
            }
        }

        // 포탑 조준 회전 속도(도/초). 즉시 스냅하면 '휙' 돌아 어색해서 이 속도로 보간한다.
        private const float TURRET_ROTATE_SPEED = 1440f;
        // 발사를 허용하는 조준 오차(도).
        private const float FIRE_ALIGNMENT_TOLERANCE = 10f;

        private void _RotateTowardNearTarget()
        {
            if (!isRotateTurret || turret == null)
                return;

            // 빔(레이저)은 포탑에 붙어 함께 돌아서, 공격 중에 회전하면 빔이 부채꼴로 쓸고 지나간다 → 빔이 켜진 동안 회전 고정.
            if (_IsBeamAttackActive())
                return;

            Monster nearTarget = GetNearTargetMonster();
            if (nearTarget == null)
                return;

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, turret.transform.GetAngle2D(nearTarget.transform.position));
            turret.transform.rotation = Quaternion.RotateTowards(
                turret.transform.rotation, targetRotation, TURRET_ROTATE_SPEED * Time.fixedDeltaTime);
        }

        // 포탑이 가장 가까운 타겟을 (거의) 바라보고 있는가. 회전하지 않는 포탑·타겟 없음은 항상 통과.
        private bool _IsAimedAtNearTarget()
        {
            if (!isRotateTurret || turret == null)
                return true;

            Monster nearTarget = GetNearTargetMonster();
            if (nearTarget == null)
                return true;

            float targetAngle = turret.transform.GetAngle2D(nearTarget.transform.position);
            return Mathf.Abs(Mathf.DeltaAngle(turret.transform.eulerAngles.z, targetAngle)) <= FIRE_ALIGNMENT_TOLERANCE;
        }

        // 빔(파티클이 아닌 NonMovement 부착 투사체, 레이저)이 켜져 있는가.
        private bool _IsBeamAttackActive()
        {
            if (useParticleProjectile || !_useNonMovementProjectilePooling)
                return false;

            foreach (var projectile in _nonMovementProjectiles)
            {
                if (projectile != null && projectile.gameObject.activeSelf)
                    return true;
            }

            return false;
        }

        protected void DetectTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _currentTurretTrainStatus.AttackRange);
            _targetMonsters = colliders.Where(a => a.GetComponent<Monster>() != null)
            .Select(a => a.GetComponent<Monster>())
            .OrderBy(x => transform.position.SqrDistance(x.transform.position))
            .ToList();

            _UpdateNearTarget();
        }

        // 비슷한 거리의 적 둘 사이에서 최근접이 매 틱 뒤바뀌면 포탑이 둘 사이를 오가며 떨려서,
        // 새 후보가 충분히 더 가까울 때만 타겟을 교체한다. (제곱거리 비율 0.7 = 거리로 약 16% 더 가까울 때)
        private const float TARGET_SWITCH_SQR_DISTANCE_RATIO = 0.7f;
        private Monster _nearTarget;

        private void _UpdateNearTarget()
        {
            Monster nearest = _targetMonsters.FirstOrDefault();

            // 현재 타겟이 없거나, 죽거나 사거리를 벗어났으면 즉시 최근접으로 교체.
            if (_nearTarget == null || !_targetMonsters.Contains(_nearTarget))
            {
                _nearTarget = nearest;
                return;
            }

            if (nearest == _nearTarget)
                return;

            float nearestSqrDistance = transform.position.SqrDistance(nearest.transform.position);
            float currentSqrDistance = transform.position.SqrDistance(_nearTarget.transform.position);
            if (nearestSqrDistance < currentSqrDistance * TARGET_SWITCH_SQR_DISTANCE_RATIO)
                _nearTarget = nearest;
        }

        protected Monster GetNearTargetMonster()
        {
            if (_nearTarget != null) return _nearTarget;

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

            float burstDuration = _GetBurstDuration();
            if (burstDuration > 0f)
                _burstRemaining = burstDuration;

            // 공격 횟수(AttackCount) > 1이면 방금 수행한 공격 전체를 공속 비례 간격으로 반복한다.
            // 버스트(화염)는 분사 유지가 공격의 연장이라 반복 대상에서 제외.
            if (_currentTurretTrainStatus.AttackCount > 1 && burstDuration <= 0f)
            {
                if (_repeatAttackCoroutine != null)
                    StopCoroutine(_repeatAttackCoroutine);
                _repeatAttackCoroutine = StartCoroutine(_RepeatAttackCoroutine(
                    _currentTurretTrainStatus.AttackCount - 1,
                    _currentTurretTrainStatus.AttackInterval * REPEAT_ATTACK_DELAY_RATIO));
            }
        }

        // 공격 횟수 반복: 한 번의 공격(대상 수만큼 타격)을 통째로 다시 수행한다.
        // 타겟 목록은 FixedUpdate의 DetectTarget이 계속 갱신하므로 반복마다 현재 타겟 기준으로 나간다.
        private IEnumerator _RepeatAttackCoroutine(int repeatCount, float delay)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                yield return new WaitForSeconds(delay);
                if (_isDead || _targetMonsters.Count == 0)
                    break;
                Attack();
            }
            _repeatAttackCoroutine = null;
        }

        // 버스트 지속시간. 포탑 데이터의 BurstDuration이 base(화염 4초)이고 업그레이드 누적이 가산된다.
        // base 0 = 비버스트 포탑(강화 규칙도 없어 항상 0).
        private float _GetBurstDuration()
        {
            if (!_useNonMovementProjectilePooling)
                return 0f;

            return _currentTurretTrainStatus.BurstDuration;
        }

        private void _EndBurst()
        {
            _burstRemaining = 0f;
            _DeactivateNonMovementProjectiles();
            if (TrainData.DamageType == DamageType.Tick)
            {
                SoundManager.Instance.StopSFX(turretTrainData.AttackSoundType);
            }
        }

        private void ResetTarget()
        {
            _targetMonsters.Clear();
            _nearTarget = null;

            _DeactivateNonMovementProjectiles();
        }

        // 깔아둔 NonMovement 장판(냉기/화염 지속 영역)을 모두 끈다. 풀은 유지하므로 부활·다음 전투에서 재사용된다.
        private void _DeactivateNonMovementProjectiles()
        {
            if (!_useNonMovementProjectilePooling || _nonMovementProjectiles.Count == 0)
                return;

            foreach (var projectile in _nonMovementProjectiles)
            {
                if (projectile != null)
                    projectile.gameObject.SetActive(false);
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
                // 조준 회전은 FixedUpdate의 _RotateTowardNearTarget가 보간으로 담당한다. (여기서 스냅하면 '휙' 돎)
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
        /// 등록된 ProjectileOverrideProvider 들을 순회. 첫 non-null 반환. 서브클래스가 추가 override 가능.
        /// </summary>
        protected virtual Projectile GetOverrideProjectile(int attackIndex)
        {
            for (int i = 0; i < _projectileOverrides.Count; i++)
            {
                var prefab = _projectileOverrides[i](attackIndex);
                if (prefab != null) return prefab;
            }
            return null;
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
            int count = _currentTurretTrainStatus.TargetCount;
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

                    TurretCombatFx.ApplySpread(projectile.transform, i, count, spreadAngle);
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
            int targetCount = _currentTurretTrainStatus.TargetCount;

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

            for (int i = 0; i < _currentTurretTrainStatus.TargetCount; i++)
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

            // 한 번의 공격이 동시에 쓰는 개수 = 대상 수(TargetCount). 공격 횟수(AttackCount)는 시간차 반복이라 풀을 늘리지 않는다.
            int maxCount = _currentTurretTrainStatus.TargetCount;

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

            // 필요한 개수만큼 추가 생성 — persistent 등록 포함 생성 경로 공유 (공용 풀 이중 소유 방지).
            while (_nonMovementProjectiles.Count < requiredCount)
            {
                if (CreatePooledNonMovementProjectile(baseProjectile) == null) break;
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

            TurretCombatFx.InitProjectile(projectile, _currentTurretTrainStatus, this, target);
        }

        /// <summary>
        /// Projectile의 AttackDamage와 AttackArea만 초기화 (타겟 없이)
        /// NonMovement 프로젝타일 생성 시점에 사용
        /// </summary>
        private void InitializeProjectileDamage(Projectile projectile)
        {
            TurretCombatFx.InitProjectile(projectile, _currentTurretTrainStatus, this, null);
        }

        /// <summary>
        /// ScaleByArea 투사체(화염·레이저)는 크기 재계산을 Init이 전담하므로
        /// 범위·사거리 스탯이 바뀌면 풀의 투사체를 다시 초기화한다. (안 하면 다음 재초기화까지 옛 크기 유지)
        /// </summary>
        private void _ReinitScaleByAreaProjectiles()
        {
            if (!_useNonMovementProjectilePooling) return;

            foreach (var projectile in _nonMovementProjectiles)
                if (projectile != null && projectile.IsScaleByArea()) InitializeProjectileDamage(projectile);
        }

        protected Projectile SpawnNormalProjectile(int index, Monster target = null)
        {
            Projectile projectile = null;

            // 엘리트 오버라이드 프로젝타일이 있으면 우선 사용 (풀링 우회)
            Projectile overridePrefab = GetOverrideProjectile(_attackCounter);
            if (overridePrefab != null)
            {
                projectile = ResourceManager.Instance.Spawn(overridePrefab);
            }
            else if (_useNonMovementProjectilePooling)
            {
                // NonMovement: 이미 생성된 프로젝타일 사용 (활성화만)
                projectile = GetNonMovementProjectile(index);

                // 연타(AttackCount 반복)가 아직 켜져 있는 빔을 다시 쏘면 SetActive(true)가 no-op이라
                // OnEnable 리셋(수명 코루틴·피격 기록·페이드 알파)이 안 돈다 → 껐다 켜서 새 발사로 시작한다.
                if (projectile != null && projectile.gameObject.activeSelf)
                    projectile.gameObject.SetActive(false);
            }
            else
            {
                // 일반 프로젝타일: ResourceManager에서 소환
                projectile = ResourceManager.Instance.Spawn(GetProjectile());
            }

            if (projectile == null) return null;

            SetupProjectileTransform(projectile, index);

            Monster targetMonster = target ?? GetNearTargetMonster();
            // 수동 조준(aimPosition) 발사처럼 타겟이 없을 때도 Init을 호출해 이동 전략을 생성한다.
            // (InitializeProjectile은 target==null이면 Init을 건너뛰어 투사체가 초기화되지 않아 제자리에 멈춘다.)
            if (targetMonster != null)
                InitializeProjectile(projectile, targetMonster);
            else
                InitializeProjectileDamage(projectile);

            if (overridePrefab != null && OverrideDamageMultiplier != 1f)
                projectile.MultiplyDamage(OverrideDamageMultiplier);

            if (_useNonMovementProjectilePooling)
            {
                projectile.gameObject.SetActive(true);
            }

            if (ProjectileScale != 1f)
                projectile.SetScale(ProjectileScale);

            if (ProjectileKnockbackPower > 0f)
                projectile.SetRuntimeShove(ProjectileKnockbackPower, ProjectileKnockbackDuration);

            // 날아가는 총알만 사거리에서 소멸 (NonMovement 풀링형(레이저·화염)은 제자리 지속형이라 무관)
            if (!_useNonMovementProjectilePooling)
                projectile.LimitLifetimeByRange(_currentTurretTrainStatus.AttackRange);

            return projectile;
        }
        #endregion

        public override void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            int currentLevel = CurrentLevel; // 업그레이드 전 레벨 저장
            base.Upgrade(upgradeData);

            // TurretTrain 전용 업그레이드 데이터가 있다면 적용
            // 다음에 적용할 upgradeStats 인덱스 = 업그레이드 전 레벨 (레벨 = 받은 업그레이드 횟수)
            if (upgradeData is TurretTrainUpgradeData turretUpgradeData)
            {
                int upgradeLevelIndex = currentLevel;
                var turretStatus = turretUpgradeData.GetTurretStatusUpgrade(upgradeLevelIndex);
                // 중복선택 증가분은 base와 동일하게 상점 배율을 받는다(Model B): (base+중복선택)×(1+상점%).
                _currentTurretTrainStatus.AttackDamage += turretStatus.AttackDamage * GetShopMultiplier(StatType.AttackDamage);
                _currentTurretTrainStatus.AttackRange += turretStatus.AttackRange * GetShopMultiplier(StatType.AttackRange);
                _currentTurretTrainStatus.AttackArea += turretStatus.AttackArea * GetShopMultiplier(StatType.AttackArea);
                _currentTurretTrainStatus.AttackCount += turretStatus.AttackCount;
                _currentTurretTrainStatus.AttackInterval += turretStatus.AttackInterval * GetShopMultiplier(StatType.AttackInterval);
                _currentTurretTrainStatus.TargetCount += turretStatus.TargetCount;
                _currentTurretTrainStatus.CriticalChance += turretStatus.CriticalChance;
                _currentTurretTrainStatus.CriticalDamage += turretStatus.CriticalDamage;
                _currentTurretTrainStatus.BurstDuration += turretStatus.BurstDuration;

                var passiveId = turretUpgradeData.GetPassiveSkillDataId(upgradeLevelIndex);
                if (!string.IsNullOrEmpty(passiveId))
                {
                    var passiveData = DatabaseManager.Instance.GetDB().TrainSkillDataDB.trainPassiveSkillDataList
                        .Find(s => s != null && s.Id == passiveId);
                    _skillModule.RegisterPassiveFromData(passiveData);
                }

                if (_useNonMovementProjectilePooling)
                {
                    EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);

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
            _currentTurretTrainStatus.BurstDuration += upgradeData.BurstDuration;

            if (_useNonMovementProjectilePooling)
            {
                EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);

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

            // 범위(AttackArea)는 실제로 쓰는 포탑만 표시 — 미사용 포탑(기관총·전기·저격)은 base가 0으로 정리돼 있어 값 판정으로 충분.
            if (_currentTurretTrainStatus.AttackArea > 0f)
                details.Add((L("Detail_Area", "범위"), $"{_currentTurretTrainStatus.AttackArea:F1}"));

            details.Add((L("Detail_Speed", "공격속도"), $"{_currentTurretTrainStatus.AttackInterval:F2}"));

            // 분사 지속시간은 버스트 포탑(화염)만 표시 — 비버스트는 base가 0이라 값 판정으로 충분.
            if (_currentTurretTrainStatus.BurstDuration > 0f)
                details.Add((L("Detail_BurstDuration", "지속시간"), $"{_currentTurretTrainStatus.BurstDuration:F1}"));

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
                        _ReinitScaleByAreaProjectiles();
                        break;
                    case StatType.AttackArea:
                        _currentTurretTrainStatus.AttackArea += _currentTurretTrainStatus.AttackArea * percent;
                        if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                        {
                            float ratio = _currentTurretTrainStatus.AttackArea / turretTrainData.TurretTrainStatus.AttackArea;
                            // ScaleByArea(빔 등)는 Projectile.StretchBeamModel이 크기를 전담 → root 스케일 제외(이중 스케일 방지).
                            foreach (var p in _nonMovementProjectiles) { if (p != null && !p.IsScaleByArea()) p.transform.localScale = new Vector3(ratio, ratio, 1f); }
                        }
                        _ReinitScaleByAreaProjectiles();
                        break;
                    case StatType.AttackDamage:
                        _currentTurretTrainStatus.AttackDamage += UtilMath.AccumulateIntDelta(ref _statAttackDamageAccum, _currentTurretTrainStatus.AttackDamage * percent);
                        if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                            foreach (var p in _nonMovementProjectiles) { if (p != null) InitializeProjectileDamage(p); }
                        break;
                    case StatType.AttackInterval:
                        // 공속은 현재값 기준 역수 곱셈(간격이 0 이하로 안 내려감). percent 음수=공속 증가.
                        // ★ 상점 스탯 강화는 이 방식이 아니다 — TrainStatUpgradeChoice가 base 대비 가산 감소
                        //   델타를 만들어 Upgrade 경로로 더하므로, 그쪽은 반복 구매 시 간격이 0에 도달한다.
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
                    _ReinitScaleByAreaProjectiles();
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
                    _ReinitScaleByAreaProjectiles();
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

                // 정수 스탯(공격 횟수·대상 수)은 % 아니라 flat +N (가독성 — 데이터에 +2를 2로 적음)
                case StatType.AttackCount:
                    _currentTurretTrainStatus.AttackCount += Mathf.RoundToInt(stat.Value);
                    break;

                case StatType.AttackInterval:
                    _currentTurretTrainStatus.AttackInterval += baseStatus.AttackInterval * percent;
                    break;

                case StatType.TargetCount:
                    _currentTurretTrainStatus.TargetCount += Mathf.RoundToInt(stat.Value);
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
                {
                    float shopRatio = (1f + percent * newLevel) / (1f + percent * prevLevel);
                    _currentTurretTrainStatus.AttackRange *= shopRatio;
                    AccumulateShopMultiplier(StatType.AttackRange, shopRatio);
                    _ReinitScaleByAreaProjectiles();
                    break;
                }

                case StatType.AttackArea:
                {
                    float shopRatio = (1f + percent * newLevel) / (1f + percent * prevLevel);
                    _currentTurretTrainStatus.AttackArea *= shopRatio;
                    AccumulateShopMultiplier(StatType.AttackArea, shopRatio);
                    if (_useNonMovementProjectilePooling && _nonMovementProjectiles.Count > 0)
                    {
                        float ratio = _currentTurretTrainStatus.AttackArea / baseStatus.AttackArea;
                        // ScaleByArea(빔 등)는 Projectile.StretchBeamModel이 크기를 전담 → root 스케일 제외(이중 스케일 방지).
                        foreach (var p in _nonMovementProjectiles)
                            if (p != null && !p.IsScaleByArea()) p.transform.localScale = new Vector3(ratio, ratio, 1f);
                    }
                    _ReinitScaleByAreaProjectiles();
                    break;
                }

                case StatType.AttackDamage:
                {
                    float shopRatio = (1f + percent * newLevel) / (1f + percent * prevLevel);
                    _currentTurretTrainStatus.AttackDamage *= shopRatio;
                    AccumulateShopMultiplier(StatType.AttackDamage, shopRatio);
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
                    break;
                }

                case StatType.AttackInterval:
                {
                    float shopRatio = (1f + (-percent) * prevLevel) / (1f + (-percent) * newLevel);
                    _currentTurretTrainStatus.AttackInterval *= shopRatio;
                    AccumulateShopMultiplier(StatType.AttackInterval, shopRatio);
                    break;
                }

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
            if (source is not TurretTrain sourceTurret) return;
            if (sourceTurret.turretTrainData == null || turretTrainData == null) return;

            var sourceBase = sourceTurret.turretTrainData.TurretTrainStatus;
            var sourceCurrent = sourceTurret._currentTurretTrainStatus;
            var newBase = turretTrainData.TurretTrainStatus;

            _currentTurretTrainStatus.AttackDamage = newBase.AttackDamage + (sourceCurrent.AttackDamage - sourceBase.AttackDamage);
            _currentTurretTrainStatus.AttackRange = newBase.AttackRange + (sourceCurrent.AttackRange - sourceBase.AttackRange);
            _currentTurretTrainStatus.AttackArea = newBase.AttackArea + (sourceCurrent.AttackArea - sourceBase.AttackArea);
            _currentTurretTrainStatus.AttackCount = newBase.AttackCount + (sourceCurrent.AttackCount - sourceBase.AttackCount);
            _currentTurretTrainStatus.AttackInterval = newBase.AttackInterval + (sourceCurrent.AttackInterval - sourceBase.AttackInterval);
            _currentTurretTrainStatus.TargetCount = newBase.TargetCount + (sourceCurrent.TargetCount - sourceBase.TargetCount);
            _currentTurretTrainStatus.CriticalChance = newBase.CriticalChance + (sourceCurrent.CriticalChance - sourceBase.CriticalChance);
            _currentTurretTrainStatus.CriticalDamage = newBase.CriticalDamage + (sourceCurrent.CriticalDamage - sourceBase.CriticalDamage);
            _currentTurretTrainStatus.BurstDuration = newBase.BurstDuration + (sourceCurrent.BurstDuration - sourceBase.BurstDuration);

            _statAttackDamageAccum = sourceTurret._statAttackDamageAccum;

            InheritShopMultipliers(sourceTurret);

            if (_useNonMovementProjectilePooling)
            {
                EnsureNonMovementProjectileCount(_currentTurretTrainStatus.TargetCount);
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
