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
      private Color slowColor = new Color(0.5f, 0.85f, 1f, 1f);
      #endregion

      [ShowInInspector, ReadOnly]
      protected MonsterData _monsterData;
      protected MonsterStatusInfo _currentMonsterStatus;
      protected float _currentHp;
      protected Vector2 _startScale;

      protected bool _isShoved;
      protected bool _isStunned;
      protected bool _isDead;
      [ShowInInspector, ReadOnly]
      protected bool _isElite;

      protected Train _targetTrain;

      protected Rigidbody2D _rigidbody2D;
      protected MonsterAnimator _modelAnimator;
      protected SpriteRenderer _modelSpriteRenderer;
      protected Color _originalColor = Color.white;

      protected Coroutine _slowCoroutine;
      protected Coroutine _resetMoveSpeedCoroutine;
      protected Coroutine _shoveCoroutine;
      protected Coroutine _stunCoroutine;

      private const string MoneyPrefabPath = "Prefabs/Money";

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
         if (_modelSpriteRenderer != null) _originalColor = _modelSpriteRenderer.color;
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
         _startScale = _prefabScale;
         if (model != null) model.transform.localScale = _prefabScale;
         if (_modelSpriteRenderer != null) _modelSpriteRenderer.color = _originalColor;
         if (eliteEffect != null) eliteEffect.SetActive(false);

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

         if (_modelAnimator != null)
         {
            _modelAnimator.OnAttackHit -= _OnAttackHit;
         }

         _isStunned = false;
         _isShoved = false;

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

      public void ApplyElite(EliteData data)
      {
         if (_isDead || data == null) return;
         _isElite = true;

         _currentMonsterStatus.MaxHp = _currentMonsterStatus.MaxHp * data.hpMultiplier;
         _currentMonsterStatus.Damage = _currentMonsterStatus.Damage * data.damageMultiplier;
         _currentMonsterStatus.MoveSpeed *= data.moveSpeedMultiplier;
         _currentMonsterStatus.DropExpMin = Mathf.RoundToInt(_currentMonsterStatus.DropExpMin * data.dropExpMultiplier);
         _currentMonsterStatus.DropExpMax = Mathf.RoundToInt(_currentMonsterStatus.DropExpMax * data.dropExpMultiplier);
         _currentMonsterStatus.DropMoneyMin = Mathf.RoundToInt(_currentMonsterStatus.DropMoneyMin * data.dropMoneyMultiplier);
         _currentMonsterStatus.DropMoneyMax = Mathf.RoundToInt(_currentMonsterStatus.DropMoneyMax * data.dropMoneyMultiplier);
         _currentHp = _currentMonsterStatus.MaxHp;

         _startScale *= data.sizeScale;
         model.transform.localScale = _startScale;

         if (eliteEffect != null)
         {
            eliteEffect.SetActive(true);
         }
      }

      private void FixedUpdate()
      {
         _OrderSprite();
         _DetectTrain();

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
         transform.Translate(_currentMonsterStatus.MoveSpeed * Time.deltaTime * _MoveDirection().normalized);
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
         int x = _MoveDirection().x > 0 ? 1 : -1;
         model.transform.localScale = new Vector2(x * _startScale.x, _startScale.y);
      }

      private void _AttackHandler()
      {
         if (_IsAttackDelay())
         {
            if (_IsAttackRange())
            {
               _Attack();
            }

            _currentMonsterStatus.AttackDelay = _monsterData.MonsterStatusData.AttackDelay;
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
            projectile.Init(_monsterData.MonsterStatusData.Damage, _targetTrain);
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
      public void Slow(float slowValue)
      {
         if (!gameObject.activeInHierarchy)
            return;

         if (_slowCoroutine != null)
         {
            StopCoroutine(_slowCoroutine);
         }
         _slowCoroutine = StartCoroutine(_SlowCoroutine(slowValue));
         if (_modelSpriteRenderer != null) _modelSpriteRenderer.color = slowColor;
      }

      private IEnumerator _SlowCoroutine(float slowValue)
      {
         var slowSpeed = _currentMonsterStatus.MoveSpeed * slowValue;
         var startSpeed = _currentMonsterStatus.MoveSpeed;
         float elapsedTime = 0f;
         float duration = 1f;

         while (elapsedTime < duration)
         {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            _currentMonsterStatus.MoveSpeed = Mathf.Lerp(startSpeed, slowSpeed, t);
            yield return null;
         }

         _currentMonsterStatus.MoveSpeed = slowSpeed;
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
      }

      /// <summary>
      /// 즉발 슬로우 + duration 초 뒤 자동 복원 (냉기 눈덩이 전용).
      /// </summary>
      public void SlowForDuration(float slowValue, float duration)
      {
         if (!gameObject.activeInHierarchy)
            return;

         if (_slowCoroutine != null)
         {
            StopCoroutine(_slowCoroutine);
         }
         _slowCoroutine = StartCoroutine(_SlowForDurationCoroutine(slowValue, duration));
         if (_modelSpriteRenderer != null) _modelSpriteRenderer.color = slowColor;
      }

      private IEnumerator _SlowForDurationCoroutine(float slowValue, float duration)
      {
         var slowSpeed = _currentMonsterStatus.MoveSpeed * slowValue;
         _currentMonsterStatus.MoveSpeed = slowSpeed;
         yield return new WaitForSeconds(duration);
         ResetMoveSpeed();
      }

      private IEnumerator _ResetMoveSpeedCoroutine()
      {
         var targetSpeed = _monsterData.MonsterStatusData.MoveSpeed;
         var startSpeed = _currentMonsterStatus.MoveSpeed;
         float elapsedTime = 0f;
         float duration = 1f;

         while (elapsedTime < duration)
         {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            _currentMonsterStatus.MoveSpeed = Mathf.Lerp(startSpeed, targetSpeed, t);
            yield return null;
         }

         _currentMonsterStatus.MoveSpeed = targetSpeed;
      }


      #endregion

      #region Shove
      public void Shove(float shovePower, float shoveDuration)
      {
         if (!gameObject.activeInHierarchy)
            return;

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

         if (_stunCoroutine != null)
         {
            StopCoroutine(_stunCoroutine);
         }
         _stunCoroutine = StartCoroutine(_StunCoroutine(stunDuration));
      }

      private IEnumerator _StunCoroutine(float stunDuration)
      {
         _isStunned = true;
         yield return new WaitForSeconds(stunDuration);
         _isStunned = false;
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

         if (_currentHp <= 0)
         {
            OnDead();
         }
      }

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

         ResourceManager.Instance.Spawn(Resources.Load<GameObject>(MoneyPrefabPath), transform.position);

         if (UserDataManager.Instance != null)
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
