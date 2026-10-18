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

        [BoxGroup("Status Effects")]
        [SerializeField]
        private bool hasSlowEffect = false;

        [BoxGroup("Status Effects")]
        [ShowIf("hasSlowEffect")]
        [SerializeField]
        private float slowValue = 0.5f;

        [BoxGroup("Status Effects")]
        [ShowIf("hasSlowEffect")]
        [SerializeField]
        private float slowDuration = 2f;

        [BoxGroup("Status Effects")]
        [SerializeField]
        private bool hasShove = false;

        [BoxGroup("Status Effects")]
        [ShowIf("hasShove")]
        [SerializeField]
        private float shovePower = 1f;

        [BoxGroup("Status Effects")]
        [ShowIf("hasShove")]
        [SerializeField]
        private float shoveDuration = 0.5f;

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

            _DestroyAfter(lifetime);
        }

        private void OnDisable()
        {
            _damageTimers.Clear();

            // 풀로 스폰되지만 수명이 끝나면 파괴하는 오브젝트다. 상점 진입 정리(ReturnAll)가 수명 도중 풀에 넣어 버리면
            // 예약된 파괴가 다음 판에 재사용된 뒤 실행돼 폭발이 도중에 사라졌다. 풀에 들어가는 순간(비활성화) 바로 파괴한다.
            if (_isDestroyScheduled)
            {
                _isDestroyScheduled = false;
                Destroy(gameObject);
            }
        }

        private bool _isDestroyScheduled;

        private void _DestroyAfter(float delay)
        {
            _isDestroyScheduled = true;
            StartCoroutine(_DestroyAfterRoutine(delay));
        }

        private System.Collections.IEnumerator _DestroyAfterRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            _isDestroyScheduled = false;
            Destroy(gameObject);
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

        // 런타임에 넉백 부여 (충격 미사일: 본체 넉백을 trigger로 전파)
        public void SetRuntimeShove(float power, float duration)
        {
            hasShove = true;
            shovePower = power;
            shoveDuration = duration;
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
            // 몬스터가 쏜 게 아니면 기차를 때리지 않는다. (owner는 Projectile에서 전파되며, 플레이어 개입 포격은 owner가 없다)
            if (_owner is not Monster && target is Train) return;

            if (damageType == DamageType.Tick)
            {
                // 틱 데미지는 진입 시 즉시 1회 피해를 주고 타이머 시작
                if (!_damageTimers.ContainsKey(target))
                {
                    _damageTimers[target] = Time.time;
                    TrainCombatRecorder.DealDamage(target, damage, _isCritical, _owner);
                }
            }
            else
            {
                // 직접 데미지는 즉시 피해
                TrainCombatRecorder.DealDamage(target, damage, _isCritical, _owner);

                if (destroyOnTriggerEnter && expandingWave == null)
                {
                    _DestroyAfter(destroyDelay);
                }
            }

            if (hasStunEffect)
            {
                TrainCombatRecorder.ApplyStun(target, stunDuration, _owner);
            }

            if (hasSlowEffect)
            {
                TrainCombatRecorder.ApplySlow(target, slowValue, slowDuration, _owner);
            }

            if (hasShove)
            {
                target.Shove(shovePower, shoveDuration);
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
                        TrainCombatRecorder.DealDamage(target, damage, _isCritical, _owner);
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
