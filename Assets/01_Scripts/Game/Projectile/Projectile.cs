using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Projectile : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private ProjectileData data;
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

            if (data != null && data.DestroyDelay > 0)
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

            Debug.Log($"projectile Init Damage{damage}");

            if (data != null)
            {
                InitializeWithConfig(attackRange);
            }
        }

        private void InitializeWithConfig(float attackRange = 0f)
        {
            // 이동 전략 초기화
            _movementStrategy = CreateMovementStrategy(data.MovementType);
            _movementStrategy?.Initialize(this, data, _target);

            // AttackRange에 따른 스케일 조정
            if (data.ScaleByAttackRange && attackRange > 0f)
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
                MovementType.TargetPos => new TargetPosMovementStrategy(),
                MovementType.NonMovement => new NonMovementStrategy(),
                _ => new LinearMovementStrategy()
            };
        }

        protected virtual void FixedUpdate()
        {
            if (data == null || _movementStrategy == null) return;

            float deltaTime = Time.fixedDeltaTime;
            _age += deltaTime;

            _movementStrategy.UpdateMovement(this, deltaTime);

            // 타겟 위치 이동 타입의 경우 충돌 없이 타겟 위치에 도달했을 때 처리
            if (_movementStrategy.ShouldImpact(this))
            {
                ProcessTargetPosImpact();
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
            if (data != null && data.IsTargeting && _target != null)
            {
                if (!ReferenceEquals(target, _target))
                {
                    return;
                }
            }

            // 데미지 처리
            if (data == null) return;

            if (data.DamageType == DamageType.Tick)
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

                if (data.DestroyOnTriggerEnter)
                {
                    ReturnToPool();
                    return;
                }
            }

            // 상태 효과 적용
            if (data.HasShoveEffect)
            {
                target.Shove(data.ShovePower, data.ShoveDuration);
            }

            if (data.HasStunEffect)
            {
                target.Stun(data.StunDuration);
            }
        }

        internal void ProcessStay(IProjectileTarget target)
        {
            if (target == null || !target.IsActive)
            {
                return;
            }

            if (data == null)
            {
                return;
            }

            // 틱 데미지 처리
            if (data.DamageType == DamageType.Tick)
            {
                if (_damageTimers.TryGetValue(target, out float lastTime))
                {
                    if (_age - lastTime >= data.TickDamageInterval)
                    {
                        target.TakeDamage(_damage);
                        _damageTimers[target] = _age;
                    }
                }
            }

            // 슬로우 효과 (Stay 중 지속 적용)
            if (data.HasSlowEffect)
            {
                target.Slow(data.SlowValue);
            }

            // 넉백 효과 (Stay 중에도 적용)
            if (data.HasShoveEffect)
            {
                target.Shove(data.ShovePower, data.ShoveDuration);
            }
        }

        internal void ProcessExit(IProjectileTarget target)
        {
            if (target == null)
            {
                return;
            }

            if (data == null)
            {
                return;
            }

            // 틱 데미지 타이머 제거
            if (data.DamageType == DamageType.Tick)
            {
                _damageTimers.Remove(target);
            }

            // 슬로우 효과 해제
            if (data.HasSlowEffect && target.IsActive)
            {
                target.ResetMoveSpeed();
            }
        }

        private void ProcessTargetPosImpact()
        {
            // 타겟 위치에 도달했을 때 범위 내 모든 몬스터에 데미지
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
            if (data == null) return false;
            return data.ScaleByAttackRange;
        }

        public ProjectileData GetData()
        {
            return data;
        }

        public void ReturnToPool()
        {
            ResourceManager.Instance.Destroy(gameObject);
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(data.DestroyDelay);
            ReturnToPool();
        }
    }
}