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
            if (other.TryGetComponent(out Monster monster))
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
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isTickProjectile && other.TryGetComponent(out Monster monster))
            {
                if (isTickProjectile && _isInTrigger)
                {
                    _tickCooldown -= Time.deltaTime;
                }

                if (_tickCooldown <= 0f)
                {
                    monster.TakeDamage(_damage);
                    _tickCooldown = tickDamageInterval;
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (isTickProjectile && other.TryGetComponent(out Monster monster))
            {
                _isInTrigger = false;
                _tickCooldown = 0f;
            }
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(destroyDelay);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}