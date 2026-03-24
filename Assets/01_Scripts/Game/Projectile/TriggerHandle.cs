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
        private int damage = 10;

        [BoxGroup("Damage Settings")]
        [SerializeField]
        private DamageType damageType = DamageType.Direct;

        [BoxGroup("Damage Settings")]
        [ShowIf("damageType", DamageType.Tick)]
        [SerializeField]
        private float tickInterval = 0.5f;

        [SerializeField]
        private bool destroyOnTriggerEnter = false;

        [SerializeField]
        private float destroyDelay = 0.2f;

        private Dictionary<IProjectileTarget, float> _damageTimers = new();

        public bool HasTurretDamage => hasTurretDamage;

        private void OnEnable()
        {
            Destroy(gameObject, destroyDelay);
        }

        private void OnDisable()
        {
            _damageTimers.Clear();
        }

        public void Init(int damage)
        {
            if (hasTurretDamage)
            {
                this.damage = damage;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IProjectileTarget>(out var target))
            {
                ProcessEnter(target);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.TryGetComponent<IProjectileTarget>(out var target))
            {
                ProcessStay(target);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent<IProjectileTarget>(out var target))
            {
                ProcessExit(target);
            }
        }

        private void ProcessEnter(IProjectileTarget target)
        {
            if (target == null || !target.IsActive) return;

            if (damageType == DamageType.Tick)
            {
                // 틱 데미지는 진입 시 즉시 1회 피해를 주고 타이머 시작
                if (!_damageTimers.ContainsKey(target))
                {
                    _damageTimers[target] = Time.time;
                    target.TakeDamage(damage);
                }
            }
            else
            {
                // 직접 데미지는 즉시 피해
                target.TakeDamage(damage);

                if (destroyOnTriggerEnter)
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
                        target.TakeDamage(damage);
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
