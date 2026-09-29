using System.Collections;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;
using Sirenix.OdinInspector;

namespace TrainDefense.Game
{
   public class Monster : MonoBehaviour, IDamageable, IProjectileTarget
   {
      #region Variables

      #region Fields
      [SerializeField]
      private string id;
      [SerializeField]
      private GameObject model;
      [SerializeField]
      private GameObject eliteEffect;
      [SerializeField]
      private Color slowColor = new Color(0.9f, 0.97f, 1f, 1f);
      [SerializeField]
      private float spawnMoveDelay = 0.1f;

      [Header("Hit Effect")]
      [SerializeField]
      private float hitPunchScale = 0.22f;
      [SerializeField]
      private float hitPunchDuration = 0.16f;
      [SerializeField]
      private float hitFlashDuration = 0.06f;
      #endregion

      [ShowInInspector, ReadOnly]
      protected MonsterData _monsterData;
      protected MonsterStatusInfo _currentMonsterStatus;
      protected float _currentHp;
      protected Vector2 _startScale;

      protected bool _isShoved;
      protected bool _isStunned;
      protected bool _isDead;
      private float _spawnTime;
      [ShowInInspector, ReadOnly]
      protected bool _isElite;

      protected Train _targetTrain;

      protected Rigidbody2D _rigidbody2D;
      protected MonsterAnimator _modelAnimator;
      protected SpriteRenderer _modelSpriteRenderer;
      protected Color _originalColor = Color.white;

      // 피격 연출 상태
      private int _facingSign = 1;
      private float _hitScaleMultiplier = 1f;
      private Coroutine _hitEffectCoroutine;
      private Material _originalMaterial;
      private static Material _sharedHitFlashMaterial;
      private const string HitFlashShaderName = "Custom/Sprite/Red";
      // 둔화 중 서리 덮인 모습: 원본 밝기에 얼음색을 입히고 흰 기운을 더한다(피격 섬광과 같은 셰이더).
      private static Material _sharedFrostMaterial;
      private static readonly Color FROST_TINT = new Color(0.6f, 0.84f, 1f, 1f);
      private const float FROST_TINT_AMOUNT = 0.5f;
      private const float FROST_EMISSION_INTENSITY = 0.22f;
      // 둔화 중이면 피격 섬광이 끝난 뒤 원래 머티리얼 대신 서리 머티리얼로 돌아간다.
      private bool _isFrosted;

      protected Coroutine _slowCoroutine;
      protected Coroutine _resetMoveSpeedCoroutine;
      protected Coroutine _shoveCoroutine;
      protected Coroutine _stunCoroutine;
      private GameObject _stunEffectInstance;

      // 엘리트 타입별 능력 상태
      private Coroutine _rangedEliteCoroutine;
      private Coroutine _speedAuraCoroutine;
      private Coroutine _auraReceiveCoroutine;
      // 노랑 엘리트 오라로 받은 이동속도 배율(기본 1). _currentMonsterStatus.MoveSpeed에 곱해 Slow와 독립 적용.
      private float _auraSpeedMultiplier = 1f;
      // 슬로우 배율(1 = 정상). 속도 스탯을 덮어쓰지 않아 갱신 중첩·엘리트 배율 잠식이 없다.
      private float _slowMultiplier = 1f;
      // 엘리트 CC 저항(0~1). 슬로우 감속량·스턴 시간·넉백을 (1-저항)배로 줄이고, 1이면 완전 면역.
      private float _ccResistance;

      private const string MoneyPrefabPath = "Prefabs/Money";
      private const string StunPrefabPath = "Prefabs/StunPaticle";
      private static GameObject _stunPrefab;

      private const string ELITE_HEALTH_BAR_PREFAB_PATH = "Prefabs/EliteHealthBar";
      // 몬스터 루트 기준 체력바 y 오프셋(아래). 모델 크기와 무관한 고정값이라 인게임 확인 후 조정.
      private const float ELITE_HEALTH_BAR_OFFSET_Y = -0.85f;
      private static GameObject _eliteHealthBarPrefab;
      // 엘리트 전용 체력바. 처음 엘리트가 될 때 자식으로 1회 생성해 계속 보유, 엘리트일 때만 켠다(eliteEffect 패턴).
      private EliteHealthBar _eliteHealthBarInstance;

      public string Id => id;
      public bool IsActive => gameObject.activeInHierarchy;
      public Transform TargetTransform => transform;
      public bool IsElite => _isElite;
      #endregion

      private Vector2 _prefabScale;

      private void Awake()
      {
         _rigidbody2D = GetComponent<Rigidbody2D>();
         _prefabScale = model.transform.localScale;
         _startScale = _prefabScale;
         _modelAnimator = model.GetComponentInChildren<MonsterAnimator>();
         _modelSpriteRenderer = model.GetComponent<SpriteRenderer>();
         if (_modelSpriteRenderer != null)
         {
            _originalColor = _modelSpriteRenderer.color;
            _originalMaterial = _modelSpriteRenderer.sharedMaterial;
         }
         if (eliteEffect != null) eliteEffect.SetActive(false);
      }

      public void Initialize(MonsterData monsterData)
      {
         _monsterData = monsterData;
         _InitStats();
      }

      #region Enable/Disable
      private void OnEnable()
      {
         _isDead = false;
         _isElite = false;
         _spawnTime = Time.time;
         _startScale = _prefabScale;
         _facingSign = 1;
         _hitScaleMultiplier = 1f;
         if (model != null) model.transform.localScale = _prefabScale;
         if (_modelSpriteRenderer != null) _modelSpriteRenderer.color = _originalColor;
         _isFrosted = false;
         _RestoreHitFlashMaterial();
         if (eliteEffect != null) eliteEffect.SetActive(false);

         // 풀 재사용 전 엘리트 상태 초기화(이전 타입의 오라 배율 잔존 방지).
         _auraSpeedMultiplier = 1f;
         _slowMultiplier = 1f;
         _ccResistance = 0f;
         if (_eliteHealthBarInstance != null)
            _eliteHealthBarInstance.gameObject.SetActive(false);

         if (_modelAnimator != null)
         {
            _modelAnimator.OnAttackHit += _OnAttackHit;
         }
      }

      private void OnDisable()
      {
         if (_slowCoroutine != null)
         {
            StopCoroutine(_slowCoroutine);
         }

         if (_stunCoroutine != null)
         {
            StopCoroutine(_stunCoroutine);
         }

         if (_modelAnimator != null)
         {
            _modelAnimator.OnAttackHit -= _OnAttackHit;
         }

         if (_hitEffectCoroutine != null)
         {
            StopCoroutine(_hitEffectCoroutine);
            _hitEffectCoroutine = null;
         }
         _hitScaleMultiplier = 1f;
         _isFrosted = false;
         _RestoreHitFlashMaterial();

         _isStunned = false;
         _isShoved = false;
         _ReleaseStunEffect();
         _StopEliteRoutines();
         _auraSpeedMultiplier = 1f;
         _slowMultiplier = 1f;

         _targetTrain = null;
      }
      #endregion

      private void _InitStats()
      {
         _currentMonsterStatus = _monsterData.MonsterStatusData;

         if (StageManager.Instance != null)
         {
            float hpScale = StageManager.Instance.GetHPScale();
            float attackScale = StageManager.Instance.GetAttackScale();

            _currentMonsterStatus.MaxHp = _currentMonsterStatus.MaxHp * hpScale;
            _currentMonsterStatus.Damage = _currentMonsterStatus.Damage * attackScale;

            Debug.Log($"[Monster] Init Stats Scaled - HP: {_monsterData.MonsterStatusData.MaxHp} -> {_currentMonsterStatus.MaxHp} (x{hpScale}), DMG: {_monsterData.MonsterStatusData.Damage} -> {_currentMonsterStatus.Damage} (x{attackScale})");
         }

         _currentHp = _currentMonsterStatus.MaxHp;
      }

      public void ApplyElite(EliteData data, EliteVariant variant)
      {
         if (_isDead || data == null) return;
         _isElite = true;

         // 배율은 variant(타입별) 우선, 없으면 EliteData 단일 배율(빨강 fallback).
         float hpMul = variant != null ? variant.hpMultiplier : data.hpMultiplier;
         float damageMul = variant != null ? variant.damageMultiplier : data.damageMultiplier;
         float moveSpeedMul = variant != null ? variant.moveSpeedMultiplier : data.moveSpeedMultiplier;
         float sizeMul = variant != null ? variant.sizeScale : data.sizeScale;
         float dropExpMul = variant != null ? variant.dropExpMultiplier : data.dropExpMultiplier;
         float dropMoneyMul = variant != null ? variant.dropMoneyMultiplier : data.dropMoneyMultiplier;

         _currentMonsterStatus.MaxHp *= hpMul;
         _currentMonsterStatus.Damage *= damageMul;
         _currentMonsterStatus.MoveSpeed *= moveSpeedMul;
         _currentMonsterStatus.DropExpMin = Mathf.RoundToInt(_currentMonsterStatus.DropExpMin * dropExpMul);
         _currentMonsterStatus.DropExpMax = Mathf.RoundToInt(_currentMonsterStatus.DropExpMax * dropExpMul);
         _currentMonsterStatus.DropMoneyMin = Mathf.RoundToInt(_currentMonsterStatus.DropMoneyMin * dropMoneyMul);
         _currentMonsterStatus.DropMoneyMax = Mathf.RoundToInt(_currentMonsterStatus.DropMoneyMax * dropMoneyMul);
         _currentHp = _currentMonsterStatus.MaxHp;

         _ccResistance = variant != null ? variant.ccResistance : 0f;

         _startScale *= sizeMul;
         model.transform.localScale = _startScale;

         _ApplyEliteVisual(variant);
         _ApplyEliteAbility(variant);
         _SpawnEliteHealthBar();
      }

      // 엘리트 불 이펙트를 켜고 타입 색으로 틴트한다. (프리팹마다 EliteEffect 자식에 SpriteRenderer 보유)
      private void _ApplyEliteVisual(EliteVariant variant)
      {
         if (eliteEffect == null) return;

         eliteEffect.SetActive(true);
         if (variant == null) return;

         SpriteRenderer effectRenderer = eliteEffect.GetComponentInChildren<SpriteRenderer>(true);
         if (effectRenderer != null)
         {
            effectRenderer.color = variant.tintColor;
         }
      }

      // 타입 고유 능력을 활성화한다. (빨강/초록은 배율 프로파일만이라 별도 능력 없음)
      private void _ApplyEliteAbility(EliteVariant variant)
      {
         if (variant == null) return;

         switch (variant.type)
         {
            case EliteType.Blue:
               _rangedEliteCoroutine = StartCoroutine(_RangedEliteAttackCoroutine(variant));

               break;

            case EliteType.Yellow:
               _speedAuraCoroutine = StartCoroutine(_SpeedAuraCoroutine(variant));

               break;
         }
      }

      private void FixedUpdate()
      {
         _OrderSprite();
         _DetectTrain();

         if (Time.time - _spawnTime < spawnMoveDelay)
            return;

         if (_isShoved)
            return;
         if (_isStunned)
            return;

         _MoveHandler();
         _AttackHandler();
         _LookAtTarget();
      }

      private void _DetectTrain()
      {
         if (TrainManager.Instance == null)
            return;

         _targetTrain = TrainManager.Instance.GetNearTrain(transform.position);
      }

      private void _MoveHandler()
      {
         if (!_IsAttackRange())
         {
            _Move();
         }
      }

      private void _Move()
      {
         // 오라·슬로우 배율을 곱한다(각각 기본 1). 속도 스탯은 안 건드려 엘리트 이속 배율과도 독립 적용된다.
         float moveSpeed = _currentMonsterStatus.MoveSpeed * _auraSpeedMultiplier * _slowMultiplier;
         transform.Translate(moveSpeed * Time.deltaTime * _MoveDirection().normalized);
      }

      private Vector3 _MoveDirection()
      {
         if (_targetTrain == null)
         {
            return Vector3.zero;
         }

         Vector3 dir = _targetTrain.transform.position - transform.position;

         return dir;
      }

      private void _OrderSprite()
      {
         _modelSpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
      }

      private void _LookAtTarget()
      {
         _facingSign = _MoveDirection().x > 0 ? 1 : -1;
         _ApplyModelScale();
      }

      // 바라보는 방향(_facingSign)과 피격 펀치 배율(_hitScaleMultiplier)을 합쳐 모델 스케일을 적용한다.
      // 스케일을 쓰는 단일 지점 — 피격 연출은 transform을 직접 건드리지 않고 배율만 바꿔 좌우반전과 충돌하지 않는다.
      private void _ApplyModelScale()
      {
         if (model == null)
            return;

         model.transform.localScale = new Vector2(
            _facingSign * _startScale.x * _hitScaleMultiplier,
            _startScale.y * _hitScaleMultiplier);
      }

      private void _AttackHandler()
      {
         if (_IsAttackDelay())
         {
            // 공격 범위 안일 때만 공격하고 딜레이를 리셋한다.
            // 범위 밖에서 리셋하면 진입 시 딜레이가 남아 바로 공격하지 못한다.
            if (_IsAttackRange())
            {
               _Attack();
               _currentMonsterStatus.AttackDelay = _monsterData.MonsterStatusData.AttackDelay;
            }
         }
         else
         {
            _currentMonsterStatus.AttackDelay -= Time.deltaTime;
         }
      }

      private void _Attack()
      {
         if (_targetTrain == null)
            return;

         if (_modelAnimator != null)
         {
            _modelAnimator.Attack();
         }
      }

      private void _OnAttackHit()
      {
         if (_targetTrain == null)
            return;

         if (_currentMonsterStatus.AttackType == MonsterAttackType.Ranged)
         {
            var projectile = ResourceManager.Instance.Spawn(_monsterData.RangedProjectilePrefab);
            projectile.transform.position = transform.position;
            projectile.transform.LookAt2D(_targetTrain.transform);
            // 엘리트 배율이 반영된 현재 데미지를 쓴다(base SO 직접참조 시 엘리트 강화가 무시됨).
            projectile.Init(_currentMonsterStatus.Damage, this);
         }
         else if (_currentMonsterStatus.AttackType == MonsterAttackType.Melee)
         {
            _targetTrain.TakeDamage(_currentMonsterStatus.Damage);
         }
      }

      private bool _IsAttackRange()
      {
         if (_targetTrain == null)
            return false;

         return Vector3.Distance(transform.position, _targetTrain.transform.position) <= _currentMonsterStatus.AttackRange;
      }

      private bool _IsAttackDelay()
      {
         return _currentMonsterStatus.AttackDelay <= 0;
      }

      #region Slow N Reset Move Speed
      public void Slow(float slowValue, float duration)
      {
         if (!gameObject.activeInHierarchy)
            return;
         if (_ccResistance >= 1f)
            return;

         // slowValue = 유지 속도 비율(0.5 = 절반) → 감속량(1-slowValue)만 저항으로 줄인다.
         slowValue = 1f - (1f - slowValue) * (1f - _ccResistance);

         if (_slowCoroutine != null)
         {
            StopCoroutine(_slowCoroutine);
            _slowCoroutine = null;
         }
         if (_resetMoveSpeedCoroutine != null)
         {
            StopCoroutine(_resetMoveSpeedCoroutine);
            _resetMoveSpeedCoroutine = null;
         }

         // 속도 스탯을 덮어쓰지 않고 배율만 세팅 — 매 틱 갱신받아도 중첩되지 않고, 엘리트 이속 배율·오라도 보존된다.
         _slowMultiplier = slowValue;

         if (duration > 0f)
         {
            _slowCoroutine = StartCoroutine(_SlowForDurationCoroutine(duration));
         }
         // duration <= 0(갱신형)은 장판 이탈(ProcessExit)의 ResetMoveSpeed로 해제된다.

         if (_modelSpriteRenderer != null) _modelSpriteRenderer.color = slowColor;
         _SetFrosted(true);
      }

      public void ResetMoveSpeed()
      {
         if (!gameObject.activeInHierarchy)
            return;

         if (_resetMoveSpeedCoroutine != null)
         {
            StopCoroutine(_resetMoveSpeedCoroutine);
         }
         _resetMoveSpeedCoroutine = StartCoroutine(_ResetMoveSpeedCoroutine());
         if (_modelSpriteRenderer != null) _modelSpriteRenderer.color = _originalColor;
         _SetFrosted(false);
      }

      // duration 초 뒤 자동 복원. 갱신(재호출) 시 코루틴이 재시작돼 타이머가 연장된다.
      private IEnumerator _SlowForDurationCoroutine(float duration)
      {
         yield return new WaitForSeconds(duration);
         ResetMoveSpeed();
      }

      // 슬로우 배율을 1초에 걸쳐 1로 되돌린다(부드러운 회복 연출).
      private IEnumerator _ResetMoveSpeedCoroutine()
      {
         var startMultiplier = _slowMultiplier;
         float elapsedTime = 0f;
         float duration = 1f;

         while (elapsedTime < duration)
         {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            _slowMultiplier = Mathf.Lerp(startMultiplier, 1f, t);
            yield return null;
         }

         _slowMultiplier = 1f;
      }


      #endregion

      #region Shove
      public void Shove(float shovePower, float shoveDuration)
      {
         if (!gameObject.activeInHierarchy)
            return;
         if (_ccResistance >= 1f)
            return;

         shovePower *= 1f - _ccResistance;
         shoveDuration *= 1f - _ccResistance;

         if (_targetTrain != null)
         {
            if (_shoveCoroutine != null)
            {
               StopCoroutine(_shoveCoroutine);
            }
            _shoveCoroutine = StartCoroutine(_ShoveCoroutine(shovePower, shoveDuration));
         }
      }

      private IEnumerator _ShoveCoroutine(float shovePower, float shoveDuration)
      {
         _isShoved = true;
         _rigidbody2D.AddForce(-_MoveDirection().normalized * shovePower, ForceMode2D.Impulse);
         yield return new WaitForSeconds(shoveDuration);
         _isShoved = false;
         _rigidbody2D.linearVelocity = Vector2.zero;
      }
      #endregion

      #region Stun
      public void Stun(float stunDuration)
      {
         if (!gameObject.activeInHierarchy)
            return;
         if (_isDead)
            return;
         if (_ccResistance >= 1f)
            return;

         stunDuration *= 1f - _ccResistance;

         if (_stunCoroutine != null)
         {
            StopCoroutine(_stunCoroutine);
         }
         _SpawnStunEffect();
         _stunCoroutine = StartCoroutine(_StunCoroutine(stunDuration));
      }

      private IEnumerator _StunCoroutine(float stunDuration)
      {
         _isStunned = true;
         yield return new WaitForSeconds(stunDuration);
         _isStunned = false;
         _ReleaseStunEffect();
      }

      private void _SpawnStunEffect()
      {
         if (_stunEffectInstance != null)
            return;

         if (_stunPrefab == null)
            _stunPrefab = Resources.Load<GameObject>(StunPrefabPath);
         if (_stunPrefab == null || ResourceManager.Instance == null)
            return;

         _stunEffectInstance = ResourceManager.Instance.Spawn(_stunPrefab, transform.position, Quaternion.identity);
      }

      private void _ReleaseStunEffect()
      {
         if (_stunEffectInstance == null)
            return;

         if (ResourceManager.Instance != null)
            ResourceManager.Instance.Destroy(_stunEffectInstance);
         _stunEffectInstance = null;
      }
      #endregion

      #region Elite Ability
      // 파랑 엘리트: interval마다 타겟 기차로 투사체를 발사한다(근접 몬스터여도 추가 원거리 공격).
      private IEnumerator _RangedEliteAttackCoroutine(EliteVariant variant)
      {
         Projectile projectilePrefab = variant.rangedProjectile != null
            ? variant.rangedProjectile
            : _monsterData.RangedProjectilePrefab;

         if (projectilePrefab == null)
            yield break;

         WaitForSeconds wait = new(variant.rangedAttackInterval);
         while (!_isDead)
         {
            yield return wait;

            if (_isDead || _targetTrain == null || ResourceManager.Instance == null)
               continue;

            Projectile projectile = ResourceManager.Instance.Spawn(projectilePrefab);
            projectile.transform.position = transform.position;
            projectile.transform.LookAt2D(_targetTrain.transform);
            projectile.Init(_currentMonsterStatus.Damage, this);
         }
      }

      // 노랑 엘리트: interval마다 오라 반경 안의 다른 몬스터에게 이동속도 버프를 건다(자신 제외).
      private IEnumerator _SpeedAuraCoroutine(EliteVariant variant)
      {
         Collider2D[] results = new Collider2D[32];
         WaitForSeconds wait = new(variant.auraInterval);
         // 갱신 주기보다 약간 길게 유지해 갱신 사이의 끊김을 막는다.
         float buffDuration = variant.auraInterval * 1.5f;

         while (!_isDead)
         {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, variant.auraRadius, results);
            for (int i = 0; i < count; i++)
            {
               if (results[i].TryGetComponent(out Monster monster) && monster != this)
               {
                  monster.ApplySpeedAura(variant.auraSpeedMultiplier, buffDuration);
               }
            }

            yield return wait;
         }
      }

      // 노랑 오라 수신: duration초 동안 이동속도 배율을 적용하고 자동 복원한다.
      public void ApplySpeedAura(float multiplier, float duration)
      {
         if (!gameObject.activeInHierarchy || _isDead)
            return;

         _auraSpeedMultiplier = multiplier;
         if (_auraReceiveCoroutine != null)
         {
            StopCoroutine(_auraReceiveCoroutine);
         }
         _auraReceiveCoroutine = StartCoroutine(_AuraReceiveCoroutine(duration));
      }

      private IEnumerator _AuraReceiveCoroutine(float duration)
      {
         yield return new WaitForSeconds(duration);
         _auraSpeedMultiplier = 1f;
      }

      // 엘리트 체력바: 엘리트 불꽃(eliteEffect)과 동일한 자식 SetActive 토글 패턴.
      // 처음 엘리트가 될 때 한 번만 자식으로 생성해 계속 보유한다 — 풀 반환(SetParent)을 안 쓰므로
      // 몬스터 비활성화(OnDisable) 중 재부모화 금지 제약과 무관하고, 소멸 시엔 부모 따라 자동으로 꺼진다.
      private void _SpawnEliteHealthBar()
      {
         if (_eliteHealthBarInstance == null)
         {
            if (_eliteHealthBarPrefab == null)
               _eliteHealthBarPrefab = Resources.Load<GameObject>(ELITE_HEALTH_BAR_PREFAB_PATH);
            if (_eliteHealthBarPrefab == null)
               return;

            GameObject instance = Instantiate(_eliteHealthBarPrefab, transform);
            instance.transform.localPosition = new Vector3(0f, ELITE_HEALTH_BAR_OFFSET_Y, 0f);
            if (!instance.TryGetComponent(out _eliteHealthBarInstance))
               return;
         }

         _eliteHealthBarInstance.gameObject.SetActive(true);
         _eliteHealthBarInstance.SetRatio(1f);
      }

      private void _StopEliteRoutines()
      {
         if (_rangedEliteCoroutine != null)
         {
            StopCoroutine(_rangedEliteCoroutine);
            _rangedEliteCoroutine = null;
         }

         if (_speedAuraCoroutine != null)
         {
            StopCoroutine(_speedAuraCoroutine);
            _speedAuraCoroutine = null;
         }

         if (_auraReceiveCoroutine != null)
         {
            StopCoroutine(_auraReceiveCoroutine);
            _auraReceiveCoroutine = null;
         }
      }
      #endregion

      public void TakeDamage(float damage)
      {
         TakeDamage(damage, false);
      }

      public void TakeDamage(float damage, bool isCritical)
      {
         if (_isDead)
            return;

         _currentHp -= damage;
         GameEventSystem.Publish(new HitEvent(_currentHp, _currentMonsterStatus.MaxHp, this, transform.position, damage, isCritical));

         if (_eliteHealthBarInstance != null)
            _eliteHealthBarInstance.SetRatio(_currentHp / _currentMonsterStatus.MaxHp);

         if (_currentHp <= 0)
         {
            OnDead();

            return;
         }

         _PlayHitEffect();
      }

      #region Hit Effect
      // 피격 시 흰색 깜빡임 + 커졌다 작아지는 펀치 연출을 재생한다(인디게임 표준 히트 피드백).
      private void _PlayHitEffect()
      {
         if (model == null)
            return;

         if (_hitEffectCoroutine != null)
         {
            StopCoroutine(_hitEffectCoroutine);
         }

         _hitEffectCoroutine = StartCoroutine(_HitEffectCoroutine());
      }

      // 흰색 머티리얼로 깜빡이며 모델이 sin 곡선으로 1 → 1+punch → 1 로 커졌다 작아진다.
      // 일시정지/슬로우모(timeScale 변동)에도 일정하게 보이도록 unscaledDeltaTime을 쓴다(DOTween SetUpdate 함정 회피).
      private IEnumerator _HitEffectCoroutine()
      {
         Material flashMaterial = _GetHitFlashMaterial();
         if (_modelSpriteRenderer != null && flashMaterial != null)
         {
            _modelSpriteRenderer.sharedMaterial = flashMaterial;
         }

         float elapsedTime = 0f;
         bool isFlashRestored = false;

         while (elapsedTime < hitPunchDuration)
         {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / hitPunchDuration);
            _hitScaleMultiplier = 1f + hitPunchScale * Mathf.Sin(Mathf.PI * progress);
            _ApplyModelScale();

            if (!isFlashRestored && elapsedTime >= hitFlashDuration)
            {
               _RestoreHitFlashMaterial();
               isFlashRestored = true;
            }

            yield return null;
         }

         if (!isFlashRestored)
         {
            _RestoreHitFlashMaterial();
         }

         _hitScaleMultiplier = 1f;
         _ApplyModelScale();
         _hitEffectCoroutine = null;
      }

      // 흰색 발광 머티리얼(코드 생성, 전 몬스터 공유). 별도 에셋/인스펙터 연결 불필요.
      // SpriteRed 셰이더의 Emission을 흰색으로 켜 알파 영역만 흰색으로 번지게 한다.
      private Material _GetHitFlashMaterial()
      {
         if (_sharedHitFlashMaterial == null)
         {
            Shader shader = Shader.Find(HitFlashShaderName);
            if (shader == null)
               return null;

            _sharedHitFlashMaterial = new Material(shader);
            _sharedHitFlashMaterial.SetFloat("_RedAmount", 0f);
            _sharedHitFlashMaterial.SetColor("_EmissionColor", Color.white);
            _sharedHitFlashMaterial.SetFloat("_EmissionIntensity", 4f);
            _sharedHitFlashMaterial.SetFloat("_Opacity", 1f);
         }

         return _sharedHitFlashMaterial;
      }

      // 원래 머티리얼로 복원한다(둔화 중이면 서리 머티리얼). 풀 재사용/사망 시 흰색이 잔존하지 않도록 보장.
      private void _RestoreHitFlashMaterial()
      {
         if (_modelSpriteRenderer != null && _originalMaterial != null)
         {
            Material frostMaterial = _isFrosted ? _GetFrostMaterial() : null;
            _modelSpriteRenderer.sharedMaterial = frostMaterial != null ? frostMaterial : _originalMaterial;
         }
      }

      // 피격 섬광 중이면 섬광이 끝날 때 _RestoreHitFlashMaterial이 서리 여부를 반영하므로 바로 바꾸지 않는다.
      private void _SetFrosted(bool isFrosted)
      {
         _isFrosted = isFrosted;
         if (_modelSpriteRenderer == null || _modelSpriteRenderer.sharedMaterial == _sharedHitFlashMaterial)
            return;

         _RestoreHitFlashMaterial();
      }

      private Material _GetFrostMaterial()
      {
         if (_sharedFrostMaterial == null)
         {
            Shader shader = Shader.Find(HitFlashShaderName);
            if (shader == null)
               return null;

            _sharedFrostMaterial = new Material(shader);
            _sharedFrostMaterial.SetColor("_RedTint", FROST_TINT);
            _sharedFrostMaterial.SetFloat("_RedAmount", FROST_TINT_AMOUNT);
            _sharedFrostMaterial.SetColor("_EmissionColor", Color.white);
            _sharedFrostMaterial.SetFloat("_EmissionIntensity", FROST_EMISSION_INTENSITY);
            _sharedFrostMaterial.SetFloat("_Opacity", 1f);
         }

         return _sharedFrostMaterial;
      }
      #endregion

      protected void OnDead()
      {
         _isDead = true;

         GameEventSystem.Publish(new MonsterDeadEvent(_monsterData != null ? _monsterData.Id : id, _isElite));

         _DropExp();
         _DropMoney();

         // 부모가 MonsterSpawner인 경우 List에서 제거
         if (transform.parent != null && transform.parent.TryGetComponent<MonsterSpawner>(out var spawner))
         {
            spawner.RemoveMonster(this);
         }

         ResourceManager.Instance.Destroy(gameObject);
      }

      #region Result
      private void _DropExp()
      {
         int dropExp = Random.Range(_currentMonsterStatus.DropExpMin, _currentMonsterStatus.DropExpMax);

         // 경험치 획득 업그레이드 보너스 (구매 레벨당 % 증가, UpgradeValue = 레벨당 퍼센트)
         if (DatabaseManager.Instance != null && UserDataManager.Instance != null)
         {
            var expBonusData = DatabaseManager.Instance.GetUpgradeData("110005");
            if (expBonusData != null)
            {
               int bonusLevel = UserDataManager.Instance.GetUpgradeLevel("110005");
               dropExp = Mathf.RoundToInt(dropExp * (1f + bonusLevel * expBonusData.UpgradeValue / 100f));
            }
         }

         GameEventSystem.Publish(new AddExpEvent(dropExp));
      }

      private void _DropMoney()
      {
         float goldScale = 1.0f;
         if (StageManager.Instance != null)
         {
            goldScale = StageManager.Instance.GetGoldScale();
         }

         int dropMoneyMin = Mathf.RoundToInt(_currentMonsterStatus.DropMoneyMin * goldScale);
         int dropMoneyMax = Mathf.RoundToInt(_currentMonsterStatus.DropMoneyMax * goldScale);

         int dropMoney = Random.Range(dropMoneyMin, dropMoneyMax);

         if (goldScale > 1.0f)
         {
            Debug.Log($"[Monster] Drop Money Scaled - {_currentMonsterStatus.DropMoneyMin}~{_currentMonsterStatus.DropMoneyMax} -> {dropMoneyMin}~{dropMoneyMax} (x{goldScale})");
         }

         // 골드 획득 업그레이드 보너스 (구매 레벨당 % 증가, UpgradeValue = 레벨당 퍼센트)
         if (DatabaseManager.Instance != null && UserDataManager.Instance != null)
         {
            var goldBonusData = DatabaseManager.Instance.GetUpgradeData("110004");
            if (goldBonusData != null)
            {
               int bonusLevel = UserDataManager.Instance.GetUpgradeLevel("110004");
               dropMoney = Mathf.RoundToInt(dropMoney * (1f + bonusLevel * goldBonusData.UpgradeValue / 100f));
            }
         }

         ResourceManager.Instance.Spawn(Resources.Load<GameObject>(MoneyPrefabPath), transform.position);

         if (UserDataManager.Instance != null && !UserDataManager.Instance.IsLobby)
         {
            int beforeCoin = UserDataManager.Instance.Coin;
            int afterCoin = beforeCoin + dropMoney;
            GameEventSystem.Publish(new ChangeCoinUIEvent(beforeCoin, afterCoin));
         }
      }
      #endregion

#if UNITY_EDITOR
      private void OnDrawGizmos()
      {
         Gizmos.color = Color.red;
         Gizmos.DrawWireSphere(transform.position, _currentMonsterStatus.AttackRange);
      }
#endif
   }
}
