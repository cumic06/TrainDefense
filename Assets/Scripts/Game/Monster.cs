using System.Collections;
using System.Linq;
using Cumic.Events;
using Cysharp.Threading.Tasks;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Monster : MonoBehaviour, IDamageable
    {
        #region Field
        [SerializeField]
        protected MonsterData monsterData;
        #endregion

        protected MonsterStatusInfo _currentMonsterStatus;

        protected bool _isDead;
        protected Train _targetTrain;

        private Rigidbody2D _rigidbody2D;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            InitStats();
        }

        private void OnEnable()
        {
            InitStats();
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

        private void InitStats()
        {
            _currentMonsterStatus = monsterData.MonsterStatusData;
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
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _currentMonsterStatus.DetectRange);
            Train[] trains = colliders.Where(a => a.GetComponent<Train>() != null)
            .Select(a => a.GetComponent<Train>())
            .Where(a => !a.IsDead && !a.IsMainTrain)
            .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
            .ToArray();

            if (trains.Length > 0)
            {
                _targetTrain = trains.FirstOrDefault();
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

        #region Slow N Reset Move Speed
        private Coroutine _slowCoroutine;

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

        public void Slow(float slowValue)
        {
            if (_slowCoroutine != null)
            {
                StopCoroutine(_slowCoroutine);
            }
            _slowCoroutine = StartCoroutine(SlowCoroutine(slowValue));
        }

        private Coroutine _resetMoveSpeedCoroutine;
        private IEnumerator ResetMoveSpeedCoroutine()
        {
            var targetSpeed = monsterData.MonsterStatusData.MoveSpeed;
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

        public void ResetMoveSpeed()
        {
            if (_resetMoveSpeedCoroutine != null)
            {
                StopCoroutine(_resetMoveSpeedCoroutine);
            }
            _resetMoveSpeedCoroutine = StartCoroutine(ResetMoveSpeedCoroutine());
        }
        #endregion

        private bool _isShoved;
        private Coroutine _shoveCoroutine;

        public void Shove(float shovePower, float shoveDuration)
        {
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

        private void AttackHandler()
        {
            if (_currentMonsterStatus.AttackDelay <= 0)
            {
                _currentMonsterStatus.AttackDelay = monsterData.MonsterStatusData.AttackDelay;
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

        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentMonsterStatus.MaxHp -= damage;
            if (_currentMonsterStatus.MaxHp <= 0)
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
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _currentMonsterStatus.DetectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _currentMonsterStatus.AttackRange);
        }
#endif
    }
}