using System.Collections;
using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using Sirenix.OdinInspector;

namespace TrainDefense.Game
{
    public class Monster : MonoBehaviour, IDamageable, IProjectileTarget
    {
        #region Variables

        #region Field
        [SerializeField]
        private string id;
        [SerializeField]
        private GameObject model;
        #endregion

        [ShowInInspector, ReadOnly]
        protected MonsterData _monsterData;
        protected MonsterStatusInfo _currentMonsterStatus;
        protected int _currentHp;
        protected Vector2 _startScale;

        protected bool _isShoved;
        protected bool _isStunned;
        protected bool _isDead;

        protected Train _targetTrain;

        protected Rigidbody2D _rigidbody2D;
        protected Animator _modelAnimator;
        protected SpriteRenderer _modelSpriteRenderer;

        protected Coroutine _slowCoroutine;
        protected Coroutine _resetMoveSpeedCoroutine;
        protected Coroutine _shoveCoroutine;
        protected Coroutine _stunCoroutine;

        public string Id => id;
        public bool IsActive => gameObject.activeInHierarchy;
        public Transform TargetTransform => transform;
        #endregion

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _startScale = model.transform.localScale;
            _modelAnimator = model.GetComponentInChildren<Animator>();
            _modelSpriteRenderer = model.GetComponent<SpriteRenderer>();
        }

        public void Initialize(MonsterData monsterData)
        {
            _monsterData = monsterData;
            InitStats();
        }

        #region Enable/Disable
        private void OnEnable()
        {
            _isDead = false;
            _targetTrain = null;
        }

        private void OnDisable()
        {
            if (_slowCoroutine != null)
            {
                StopCoroutine(_slowCoroutine);
            }
        }
        #endregion

        private void InitStats()
        {
            _currentMonsterStatus = _monsterData.MonsterStatusData;

            if (StageManager.Instance != null)
            {
                float hpScale = StageManager.Instance.GetHPScale();
                float attackScale = StageManager.Instance.GetAttackScale();

                _currentMonsterStatus.MaxHp = Mathf.RoundToInt(_currentMonsterStatus.MaxHp * hpScale);
                _currentMonsterStatus.Damage = Mathf.RoundToInt(_currentMonsterStatus.Damage * attackScale);

                Debug.Log($"[Monster] Init Stats Scaled - HP: {_monsterData.MonsterStatusData.MaxHp} -> {_currentMonsterStatus.MaxHp} (x{hpScale}), DMG: {_monsterData.MonsterStatusData.Damage} -> {_currentMonsterStatus.Damage} (x{attackScale})");
            }

            _currentHp = _currentMonsterStatus.MaxHp;
        }

        private void FixedUpdate()
        {
            OrderSprite();
            DetectTrain();

            if (_isShoved) return;
            if (_isStunned) return;

            MoveHandler();
            AttackHandler();
            LookAtTarget();
        }

        private void DetectTrain()
        {
            if (TrainManager.Instance == null) return;

            _targetTrain = TrainManager.Instance.GetNearTrain(transform.position);
        }

        private void MoveHandler()
        {
            if (!IsAttackRange())
            {
                Move();
            }
        }

        private void Move()
        {
            transform.Translate(MoveDirection().normalized * Time.deltaTime * _currentMonsterStatus.MoveSpeed);
        }

        private Vector3 MoveDirection()
        {
            if (_targetTrain == null) return Vector3.zero;

            return _targetTrain.transform.position - transform.position;
        }

        private void OrderSprite()
        {
            _modelSpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }

        private void LookAtTarget()
        {
            int x = MoveDirection().x > 0 ? 1 : -1;
            model.transform.localScale = new Vector2(x * _startScale.x, _startScale.y);
        }

        private void AttackHandler()
        {
            if (IsAttackDelay())
            {
                if (IsAttackRange())
                {
                    Attack();
                }

                _currentMonsterStatus.AttackDelay = _monsterData.MonsterStatusData.AttackDelay;
            }
            else
            {
                _currentMonsterStatus.AttackDelay -= Time.deltaTime;
            }
        }

        private void Attack()
        {
            if (_targetTrain == null) return;

            _modelAnimator.CrossFade("Attack", 0);

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

        private bool IsAttackRange()
        {
            if (_targetTrain == null) return false;

            return Vector3.Distance(transform.position, _targetTrain.transform.position) <= _currentMonsterStatus.AttackRange;
        }

        private bool IsAttackDelay()
        {
            return _currentMonsterStatus.AttackDelay <= 0;
        }

        #region Slow N Reset Move Speed
        public void Slow(float slowValue)
        {
            if (!gameObject.activeInHierarchy) return;

            if (_slowCoroutine != null)
            {
                StopCoroutine(_slowCoroutine);
            }
            _slowCoroutine = StartCoroutine(SlowCoroutine(slowValue));
        }

        private IEnumerator SlowCoroutine(float slowValue)
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
            if (!gameObject.activeInHierarchy) return;

            if (_resetMoveSpeedCoroutine != null)
            {
                StopCoroutine(_resetMoveSpeedCoroutine);
            }
            _resetMoveSpeedCoroutine = StartCoroutine(ResetMoveSpeedCoroutine());
        }

        private IEnumerator ResetMoveSpeedCoroutine()
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
            if (!gameObject.activeInHierarchy) return;

            if (_targetTrain != null)
            {
                if (_shoveCoroutine != null)
                {
                    StopCoroutine(_shoveCoroutine);
                }
                _shoveCoroutine = StartCoroutine(ShoveCoroutine(shovePower, shoveDuration));
            }
        }

        private IEnumerator ShoveCoroutine(float shovePower, float shoveDuration)
        {
            _isShoved = true;
            _rigidbody2D.AddForce(-MoveDirection().normalized * shovePower, ForceMode2D.Impulse);
            yield return new WaitForSeconds(shoveDuration);
            _isShoved = false;
            _rigidbody2D.linearVelocity = Vector2.zero;
        }
        #endregion

        #region Stun
        public void Stun(float stunDuration)
        {
            if (!gameObject.activeInHierarchy) return;
            if (_isDead) return;

            if (_stunCoroutine != null)
            {
                StopCoroutine(_stunCoroutine);
            }
            _stunCoroutine = StartCoroutine(StunCoroutine(stunDuration));
        }

        private IEnumerator StunCoroutine(float stunDuration)
        {
            _isStunned = true;
            yield return new WaitForSeconds(stunDuration);
            _isStunned = false;
        }
        #endregion

        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp -= damage;
            GameEventSystem.Publish(new HitEvent(_currentHp, _currentMonsterStatus.MaxHp, this, transform.position, damage));

            if (_currentHp <= 0)
            {
                OnDead();
            }
        }

        private void OnDead()
        {
            _isDead = true;

            DropExp();
            DropMoney();

            // 부모가 MonsterSpawner인 경우 List에서 제거
            if (transform.parent != null && transform.parent.TryGetComponent<MonsterSpawner>(out var spawner))
            {
                spawner.RemoveMonster(this);
            }

            ResourceManager.Instance.Destroy(gameObject);
        }

        #region Result
        private void DropExp()
        {
            int dropExp = Random.Range(_currentMonsterStatus.DropExpMin, _currentMonsterStatus.DropExpMax);
            GameEventSystem.Publish(new AddExpEvent(dropExp));
        }

        private void DropMoney()
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

            ResourceManager.Instance.Spawn(Resources.Load<GameObject>("Prefabs/Money"), transform.position);

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