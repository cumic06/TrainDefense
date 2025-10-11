using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Projectile : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private float speed;
        [SerializeField]
        private float destroyDelay;

        [SerializeField]
        [BoxGroup("TickProjectile")]
        private bool isTickProjectile;
        [SerializeField]
        [BoxGroup("TickProjectile")]
        private float tickDamageInterval = 0.1f;

        [SerializeField]
        [BoxGroup("SlowProjectile")]
        private bool isSlowProjectile;
        [SerializeField]
        [BoxGroup("SlowProjectile")]
        private float slowValue = 0.5f;

        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        private bool isShoveProjectile;
        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        private float shovePower = 1f;
        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        private float shoveDuration = 0.5f;
        #endregion

        private int _damage;
        private Coroutine _destroyCoroutine;

        private float _tickCooldown;
        private bool _isInTrigger;

        private void OnEnable()
        {
            if (destroyDelay <= 0) return;

            if (_destroyCoroutine != null)
            {
                StopCoroutine(_destroyCoroutine);
            }
            _destroyCoroutine = StartCoroutine(DestroyCoroutine());
        }

        private void OnDisable()
        {
            _isInTrigger = false;
            _tickCooldown = 0f;
        }

        public void Init(int damage)
        {
            _damage = damage;
        }

        private void FixedUpdate()
        {
            Move();
        }

        protected virtual void Move()
        {
            transform.Translate(Vector3.right * Time.deltaTime * speed);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            bool isMonster = other.TryGetComponent(out Monster monster);

            if (isMonster)
            {
                if (isTickProjectile)
                {
                    _isInTrigger = true;
                    _tickCooldown = 0f;
                }
                else
                {
                    monster.TakeDamage(_damage);
                    ResourceManager.Instance.Destroy(gameObject);
                }

                if (isShoveProjectile)
                {
                    monster.Shove(shovePower, shoveDuration);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            bool isMonster = other.TryGetComponent(out Monster monster);

            if (isMonster)
            {
                if (isTickProjectile)
                {
                    if (_isInTrigger)
                    {
                        _tickCooldown -= Time.deltaTime;
                    }

                    if (_tickCooldown <= 0f)
                    {
                        if (monster == null || !monster.gameObject.activeInHierarchy) return;

                        monster.TakeDamage(_damage);
                        _tickCooldown = tickDamageInterval;
                    }
                }

                if (isSlowProjectile)
                {
                    if (monster == null || !monster.gameObject.activeInHierarchy) return;

                    monster.Slow(slowValue);
                }

                if (isShoveProjectile)
                {
                    monster.Shove(shovePower, shoveDuration);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            bool isMonster = other.TryGetComponent(out Monster monster);

            if (isMonster)
            {
                if (isTickProjectile)
                {
                    _isInTrigger = false;
                    _tickCooldown = 0f;
                }

                if (isSlowProjectile)
                {
                    if (monster == null || !monster.gameObject.activeInHierarchy) return;

                    monster.ResetMoveSpeed();
                }
            }
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(destroyDelay);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}