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
        #region Field
        [SerializeField]
        private string id;
        #endregion

        [ShowInInspector, ReadOnly]
        protected MonsterData _monsterData;
        protected MonsterStatusInfo _currentMonsterStatus;
        protected int _currentHp;

        protected bool _isShoved;
        protected bool _isDead;

        protected Train _targetTrain;

        protected Rigidbody2D _rigidbody2D;

        protected Coroutine _slowCoroutine;
        protected Coroutine _resetMoveSpeedCoroutine;
        protected Coroutine _shoveCoroutine;

        public string Id => id;
        public bool IsActive => gameObject.activeInHierarchy;
        public Transform TargetTransform => transform;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
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
            _currentHp = _currentMonsterStatus.MaxHp;
        }

        private void FixedUpdate()
        {
            DetectTrain();

            if (_isShoved) return;
            Move();
            AttackHandler();
        }

        private void DetectTrain()
        {
            if (TrainManager.Instance == null) return;

            _targetTrain = TrainManager.Instance.GetNearTrain(transform.position);
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

        private void AttackHandler()
        {
            if (_currentMonsterStatus.AttackDelay <= 0)
            {
                _currentMonsterStatus.AttackDelay = _monsterData.MonsterStatusData.AttackDelay;
                Attack();
            }
            else
            {
                _currentMonsterStatus.AttackDelay -= Time.deltaTime;
            }
        }

        private void Attack()
        {
            if (_targetTrain == null) return;

            if (Vector3.Distance(transform.position, _targetTrain.transform.position) <= _currentMonsterStatus.AttackRange)
            {
                _targetTrain.TakeDamage(_currentMonsterStatus.Damage);
            }
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

            GameEventSystem.Publish(new MonsterDeadEvent());
            ResourceManager.Instance.Destroy(gameObject);
        }

        private void DropExp()
        {
            int dropExp = Random.Range(_currentMonsterStatus.DropExpMin, _currentMonsterStatus.DropExpMax);
            GameEventSystem.Publish(new AddExpEvent(dropExp));
        }

        private void DropMoney()
        {
            int dropMoney = Random.Range(_currentMonsterStatus.DropMoneyMin, _currentMonsterStatus.DropMoneyMax);
            ResourceManager.Instance.Spawn(Resources.Load<GameObject>("Prefabs/Money"), transform.position);
            GameEventSystem.Publish(new AddCoinEvent(dropMoney));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _currentMonsterStatus.AttackRange);
        }
#endif
    }
}