using System.Collections;
using System.Collections.Generic;
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
        private bool destroyOnTriggerEnter = true;

        [SerializeField]
        private bool isTargeting = false;

        [SerializeField]
        [BoxGroup("TickProjectile")]
        protected bool isTickProjectile;
        [SerializeField]
        [BoxGroup("TickProjectile")]
        protected float tickDamageInterval = 0.1f;

        [SerializeField]
        [BoxGroup("SlowProjectile")]
        protected bool isSlowProjectile;
        [SerializeField]
        [BoxGroup("SlowProjectile")]
        protected float slowValue = 0.5f;

        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        protected bool isShoveProjectile;
        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        protected float shovePower = 1f;
        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        protected float shoveDuration = 0.5f;
        [SerializeField]
        [BoxGroup("StunProjectile")]
        protected bool isStunProjectile;
        [SerializeField]
        [BoxGroup("StunProjectile")]
        protected float stunDuration = 0.5f;
        #endregion

        protected int _damage;
        private Coroutine _destroyCoroutine;

        protected Dictionary<Monster, float> _monsterDamageTimers = new();
        protected Monster targetMonster;

        #region Enable/Disable

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
            _monsterDamageTimers.Clear();
        }

        #endregion

        public void Init(int damage, Monster targetMonster = null)
        {
            _damage = damage;

            this.targetMonster = targetMonster;
        }

        protected virtual void FixedUpdate()
        {
            Move();
        }

        protected virtual void Move()
        {
            if (speed <= 0) return;

            transform.Translate(Vector3.right * Time.deltaTime * speed);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                if (isTargeting)
                {
                    if (monster == targetMonster)
                    {
                        monster.TakeDamage(_damage);
                    }
                }

                if (isTickProjectile)
                {
                    // 처음 들어올 때는 즉시 데미지 적용
                    if (!_monsterDamageTimers.ContainsKey(monster))
                    {
                        _monsterDamageTimers[monster] = Time.time;
                        monster.TakeDamage(_damage);
                    }
                }
                else
                {
                    monster.TakeDamage(_damage);

                    if (destroyOnTriggerEnter)
                    {
                        ResourceManager.Instance.Destroy(gameObject);
                    }
                }

                if (isShoveProjectile)
                {
                    if (monster == null) return;
                    monster.Shove(shovePower, shoveDuration);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                if (isTickProjectile)
                {
                    // Monster가 Dictionary에 있는지 확인하고, tickDamageInterval 시간이 지났으면 데미지 적용
                    if (_monsterDamageTimers.ContainsKey(monster))
                    {
                        float lastDamageTime = _monsterDamageTimers[monster];
                        if (Time.time - lastDamageTime >= tickDamageInterval)
                        {
                            if (monster == null) return;
                            monster.TakeDamage(_damage);
                            _monsterDamageTimers[monster] = Time.time;
                        }
                    }
                }

                if (isSlowProjectile)
                {
                    if (monster == null) return;
                    monster.Slow(slowValue);
                }

                if (isShoveProjectile)
                {
                    if (monster == null) return;
                    monster.Shove(shovePower, shoveDuration);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                if (isTickProjectile)
                {
                    // Dictionary에서 제거
                    _monsterDamageTimers.Remove(monster);
                }

                if (isSlowProjectile)
                {
                    if (monster == null || !monster.gameObject.activeInHierarchy) return;
                    monster.ResetMoveSpeed();
                }
            }
        }

        protected bool IsMonster(Collider2D other, out Monster monster)
        {
            return other.TryGetComponent(out monster);
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(destroyDelay);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}