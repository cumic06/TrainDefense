using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Projectile : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private ProjectileConfig config;
        [SerializeField]
        private GameObject model;
        #endregion

        private int _damage;
        private IProjectileTarget _target;
        private IMovementStrategy _movementStrategy;
        private Coroutine _destroyCoroutine;
        private Dictionary<IProjectileTarget, float> _damageTimers = new();
        private float _age;

        #region Enable/Disable

        private void OnEnable()
        {
            _age = 0f;
            _damageTimers.Clear();

            if (config != null && config.DestroyDelay > 0)
            {
                if (_destroyCoroutine != null)
                {
                    StopCoroutine(_destroyCoroutine);
                }
                _destroyCoroutine = StartCoroutine(DestroyCoroutine());
            }
        }

        private void OnDisable()
        {
            _damageTimers.Clear();
            _movementStrategy = null;
        }

        #endregion

        /// <summary>
        /// 투사체 초기화
        /// </summary>
        /// <param name="damage">데미지</param>
        /// <param name="target">타겟 (Monster 또는 null)</param>
        /// <param name="attackRange">공격 범위 (스케일 조정에 사용)</param>
        public void Init(int damage, IProjectileTarget target = null, float attackRange = 0f)
        {
            _damage = damage;
            _target = target;

            if (config != null)
            {
                InitializeWithConfig(attackRange);
            }
        }

        private void InitializeWithConfig(float attackRange = 0f)
        {
            // 이동 전략 초기화
            _movementStrategy = CreateMovementStrategy(config.MovementType);
            _movementStrategy?.Initialize(this, config, _target);

            // AttackRange에 따른 스케일 조정
            if (config.ScaleByAttackRange && attackRange > 0f)
            {
                ApplyScaleByAttackRange(attackRange);
            }
        }

        private void ApplyScaleByAttackRange(float attackRange)
        {
            if (model == null) return;

            Vector3 currentScale = model.transform.localScale;

            model.transform.localPosition = new Vector3(attackRange / 2, 0, 0);
            model.transform.localScale = new Vector3(attackRange, currentScale.y, currentScale.z);
        }

        private IMovementStrategy CreateMovementStrategy(MovementType movementType)
        {
            return movementType switch
            {
                MovementType.Linear => new LinearMovementStrategy(),
                MovementType.DelayedDrop => new DelayedDropMovementStrategy(),
                MovementType.NonMovement => new NonMovementStrategy(),
                _ => new LinearMovementStrategy()
            };
        }

        protected virtual void FixedUpdate()
        {
            if (config == null || _movementStrategy == null) return;

            float deltaTime = Time.fixedDeltaTime;
            _age += deltaTime;

            _movementStrategy.UpdateMovement(this, deltaTime);

            // 지연 낙하 타입의 경우 충돌 없이 타겟 위치에 도달했을 때 처리
            if (_movementStrategy.ShouldImpact(this))
            {
                ProcessDelayedDropImpact();
            }
        }



        #region Trigger Events
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<IProjectileTarget>(out var target))
            {
                return;
            }

            ProcessEnter(target);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.TryGetComponent<IProjectileTarget>(out var target))
            {
                return;
            }

            ProcessStay(target);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<IProjectileTarget>(out var target))
            {
                return;
            }

            ProcessExit(target);
        }
        #endregion

        #region Process
        internal void ProcessEnter(IProjectileTarget target)
        {
            if (target == null || !target.IsActive)
            {
                return;
            }

            // 타겟팅 체크
            if (config != null && config.IsTargeting && _target != null)
            {
                if (!ReferenceEquals(target, _target))
                {
                    return;
                }
            }

            // 데미지 처리
            if (config == null) return;

            if (config.DamageType == DamageType.Tick)
            {
                // 틱 데미지: 처음 진입 시 즉시 데미지
                if (!_damageTimers.ContainsKey(target))
                {
                    _damageTimers[target] = _age;
                    target.TakeDamage(_damage);
                }
            }
            else
            {
                // 직접 데미지
                target.TakeDamage(_damage);

                if (config.DestroyOnTriggerEnter)
                {
                    ReturnToPool();
                    return;
                }
            }

            // 상태 효과 적용
            if (config.HasShoveEffect)
            {
                target.Shove(config.ShovePower, config.ShoveDuration);
            }
        }

        internal void ProcessStay(IProjectileTarget target)
        {
            if (target == null || !target.IsActive)
            {
                return;
            }

            if (config == null)
            {
                return;
            }

            // 틱 데미지 처리
            if (config.DamageType == DamageType.Tick)
            {
                if (_damageTimers.TryGetValue(target, out float lastTime))
                {
                    if (_age - lastTime >= config.TickDamageInterval)
                    {
                        target.TakeDamage(_damage);
                        _damageTimers[target] = _age;
                    }
                }
            }

            // 슬로우 효과 (Stay 중 지속 적용)
            if (config.HasSlowEffect)
            {
                target.Slow(config.SlowValue);
            }

            // 넉백 효과 (Stay 중에도 적용)
            if (config.HasShoveEffect)
            {
                target.Shove(config.ShovePower, config.ShoveDuration);
            }
        }

        internal void ProcessExit(IProjectileTarget target)
        {
            if (target == null)
            {
                return;
            }

            if (config == null)
            {
                return;
            }

            // 틱 데미지 타이머 제거
            if (config.DamageType == DamageType.Tick)
            {
                _damageTimers.Remove(target);
            }

            // 슬로우 효과 해제
            if (config.HasSlowEffect && target.IsActive)
            {
                target.ResetMoveSpeed();
            }
        }

        private void ProcessDelayedDropImpact()
        {
            // 지연 낙하가 타겟 위치에 도달했을 때 범위 내 모든 몬스터에 데미지
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1f);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<IProjectileTarget>(out var target))
                {
                    ProcessEnter(target);
                }
            }
        }
        #endregion


        public bool IsScaleByAttackRange()
        {
            if (config == null) return false;
            return config.ScaleByAttackRange;
        }

        public ProjectileConfig GetConfig()
        {
            return config;
        }

        public void ReturnToPool()
        {
            ResourceManager.Instance.Destroy(gameObject);
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(config.DestroyDelay);
            ReturnToPool();
        }
    }
}