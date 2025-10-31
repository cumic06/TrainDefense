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
        private bool isParticleProjectile;

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

        private Dictionary<Monster, float> _particleTriggerCooldowns = new();

        #region Enable/Disable
        private void Awake()
        {
            if (isParticleProjectile)
            {
                SetupParticleTrigger();
            }
        }

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
            _particleTriggerCooldowns.Clear();
        }

        private void SetupParticleTrigger()
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps == null) return;

            ParticleSystem.TriggerModule triggerModule = ps.trigger;
            if (!triggerModule.enabled)
            {
                triggerModule.enabled = true;
            }

            // Trigger 모듈의 이벤트 타입을 Callback으로 설정해야 OnParticleTrigger가 호출됨
            // Unity API로는 직접 설정이 불가능하므로 Inspector에서 수동 설정 필요
            // 여기서는 설정이 올바른지 확인만 함
        }
        #endregion

        public void Init(int damage)
        {
            _damage = damage;
        }

        private void FixedUpdate()
        {
            Move();

            if (isParticleProjectile)
            {
                CleanupInvalidMonsters();
            }
        }

        protected virtual void Move()
        {
            if (speed <= 0) return;

            transform.Translate(Vector3.right * Time.deltaTime * speed);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isParticleProjectile) return;


            if (IsMonster(other, out Monster monster))
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
                    if (monster == null) return;

                    monster.Shove(shovePower, shoveDuration);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isParticleProjectile) return;

            if (IsMonster(other, out Monster monster))
            {
                if (isTickProjectile)
                {
                    if (_isInTrigger)
                    {
                        _tickCooldown -= Time.deltaTime;
                    }

                    if (_tickCooldown <= 0f)
                    {
                        if (monster == null) return;

                        monster.TakeDamage(_damage);
                        _tickCooldown = tickDamageInterval;
                    }
                }

                if (isSlowProjectile)
                {
                    if (monster == null) return;

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
            if (isParticleProjectile) return;

            if (IsMonster(other, out Monster monster))
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

        private bool IsMonster(Collider2D other, out Monster monster)
        {
            return other.TryGetComponent(out monster);
        }

        private void OnParticleTrigger()
        {
            if (!isParticleProjectile)
            {
                Debug.LogWarning("isParticleProjectile is false");
                return;
            }

            ParticleSystem ps = GetComponent<ParticleSystem>();

            if (ps == null)
            {
                Debug.LogWarning("ParticleSystem is null");
                return;
            }

            List<ParticleSystem.Particle> enter = new();
            List<ParticleSystem.Particle> inside = new();
            List<ParticleSystem.Particle> exit = new();

            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
            int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exit);

            // Enter, Inside, Exit 중 하나라도 발생하면 처리
            if (numEnter > 0 || numInside > 0 || numExit > 0)
            {
                ProcessParticleTrigger(ps, enter, inside, exit);
            }
        }

        private void ProcessParticleTrigger(ParticleSystem ps, List<ParticleSystem.Particle> enter, List<ParticleSystem.Particle> inside, List<ParticleSystem.Particle> exit)
        {
            ParticleSystem.TriggerModule triggerModule = ps.trigger;
            if (!triggerModule.enabled) return;

            int colliderCount = triggerModule.colliderCount;
            if (colliderCount == 0) return;

            for (int i = 0; i < colliderCount; i++)
            {
                Component collider = triggerModule.GetCollider(i);
                if (collider == null) continue;

                if (collider.TryGetComponent(out Monster monster))
                {
                    // Enter, Inside, Exit 중 하나라도 발생했으면 데미지 적용
                    if (enter.Count > 0 || inside.Count > 0 || exit.Count > 0)
                    {
                        Debug.Log("ApplyParticleDamage: " + monster.name);
                        ApplyParticleDamage(monster);
                    }
                }
            }
        }

        private void ApplyParticleDamage(Monster monster)
        {
            if (monster == null) return;

            if (!_particleTriggerCooldowns.ContainsKey(monster))
            {
                _particleTriggerCooldowns[monster] = tickDamageInterval;
            }

            _particleTriggerCooldowns[monster] -= tickDamageInterval;

            if (_particleTriggerCooldowns[monster] <= 0f)
            {
                monster.TakeDamage(_damage);
                _particleTriggerCooldowns[monster] = tickDamageInterval;

                if (isShoveProjectile)
                {
                    monster.Shove(shovePower, shoveDuration);
                }

                if (isSlowProjectile)
                {
                    monster.Slow(slowValue);
                }
            }
        }

        private void CleanupInvalidMonsters()
        {
            List<Monster> monstersToRemove = new();

            foreach (var kvp in _particleTriggerCooldowns)
            {
                if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy)
                {
                    monstersToRemove.Add(kvp.Key);
                }
            }

            foreach (var monster in monstersToRemove)
            {
                _particleTriggerCooldowns.Remove(monster);
            }
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(destroyDelay);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}