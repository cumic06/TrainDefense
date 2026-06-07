using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TriggerHandle : MonoBehaviour
    {
        [BoxGroup("Damage Settings")]
        [SerializeField]
        private bool hasTurretDamage = false;

        [BoxGroup("Damage Settings")]
        [HideIf("hasTurretDamage")]
        [SerializeField]
        private float damage = 10f;

        [BoxGroup("Damage Settings")]
        [SerializeField]
        private DamageType damageType = DamageType.Direct;

        [BoxGroup("Damage Settings")]
        [ShowIf("damageType", DamageType.Tick)]
        [SerializeField]
        private float tickInterval = 0.5f;

        [BoxGroup("Status Effects")]
        [SerializeField]
        private bool hasStunEffect = false;

        [BoxGroup("Status Effects")]
        [ShowIf("hasStunEffect")]
        [SerializeField]
        private float stunDuration = 1f;

        [SerializeField]
        private bool destroyOnTriggerEnter = false;

        [SerializeField]
        private float destroyDelay = 0.2f;

        [BoxGroup("Wave Settings")]
        [SerializeField]
        private ExpandingWave expandingWave;

        private Dictionary<IProjectileTarget, float> _damageTimers = new();
        private IProjectileTarget _owner;
        private bool _isCritical;

        public bool HasTurretDamage => hasTurretDamage;

        private void OnEnable()
        {
            _owner = null;

            float lifetime = destroyDelay;

            if (expandingWave != null)
            {
                lifetime = Mathf.Max(lifetime, expandingWave.RequiredLifetime);
            }

            Destroy(gameObject, lifetime);
        }

        private void OnDisable()
        {
            _damageTimers.Clear();
        }

        public void Init(float damage, IProjectileTarget owner = null, bool isCritical = false)
        {
            if (hasTurretDamage)
            {
                this.damage = damage;
                _isCritical = isCritical;
            }
            _owner = owner;
        }

        public void ApplyWaveHit(IProjectileTarget target)
        {
            ProcessEnter(target);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (expandingWave != null) return;

            if (other.TryGetComponent<IProjectileTarget>(out var target))
            {
                ProcessEnter(target);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (expandingWave != null) return;

            if (other.TryGetComponent<IProjectileTarget>(out var target))
            {
                ProcessStay(target);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (expandingWave != null) return;

            if (other.TryGetComponent<IProjectileTarget>(out var target))
            {
                ProcessExit(target);
            }
        }

        private void ProcessEnter(IProjectileTarget target)
        {
            if (target == null || !target.IsActive) return;
            if (_owner is Train && target is Train) return;

            if (damageType == DamageType.Tick)
            {
                // 틱 데미지는 진입 시 즉시 1회 피해를 주고 타이머 시작
                if (!_damageTimers.ContainsKey(target))
                {
                    _damageTimers[target] = Time.time;
                    target.TakeDamage(damage, _isCritical);
                }
            }
            else
            {
                // 직접 데미지는 즉시 피해
                target.TakeDamage(damage, _isCritical);

                if (destroyOnTriggerEnter && expandingWave == null)
                {
                    if (destroyDelay > 0)
                    {
                        Destroy(gameObject, destroyDelay);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }

            if (hasStunEffect)
            {
                target.Stun(stunDuration);
            }
        }

        private void ProcessStay(IProjectileTarget target)
        {
            if (target == null || !target.IsActive) return;

            if (damageType == DamageType.Tick)
            {
                if (_damageTimers.TryGetValue(target, out float lastTime))
                {
                    if (Time.time - lastTime >= tickInterval)
                    {
                        target.TakeDamage(damage, _isCritical);
                        _damageTimers[target] = Time.time;
                    }
                }
            }
        }

        private void ProcessExit(IProjectileTarget target)
        {
            if (target == null) return;

            if (damageType == DamageType.Tick)
            {
                _damageTimers.Remove(target);
            }
        }
    }
}
