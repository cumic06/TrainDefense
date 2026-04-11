using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Projectile : MonoBehaviour
    {
        #region Field
        [SerializeField]
        protected ProjectileData data;
        [SerializeField]
        protected GameObject model;
        #endregion

        protected int _damage;
        protected IProjectileTarget _target;
        protected IProjectileTarget _owner;
        protected IMovementStrategy _movementStrategy;
        protected Coroutine _destroyCoroutine;
        protected Dictionary<IProjectileTarget, float> _damageTimers = new();
        protected float _age;
        protected bool _isSpawnedTrigger;
        protected float _attackRange;

        #region Enable/Disable

        protected virtual void OnEnable()
        {
            _age = 0f;
            _damageTimers.Clear();
            _isSpawnedTrigger = false;

            if (data != null && data.DestroyDelay > 0)
            {
                if (_destroyCoroutine != null)
                {
                    StopCoroutine(_destroyCoroutine);
                }
                _destroyCoroutine = StartCoroutine(DestroyCoroutine());
            }

            if (data.ScaleByRange && data.ScaleRangeType == ScaleByRangeType.TargetRange && _target != null)
            {
                ApplyScaleByTargetRange(_target.TargetTransform.position);
            }
        }

        protected virtual void OnDisable()
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
        public virtual void Init(int damage, IProjectileTarget owner, IProjectileTarget target = null, float attackRange = 0f)
        {
            _damage = damage;
            _owner = owner;
            _target = target;
            _attackRange = attackRange;

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
            if (data.ScaleByRange && data.ScaleRangeType == ScaleByRangeType.AttackRange && attackRange > 0f)
            {
                ApplyScaleByAttackRange(attackRange);
            }
        }

        private void ApplyScaleByAttackRange(float attackRange)
        {
            StretchBeamModel(attackRange);
        }

        private void ApplyScaleByTargetRange(Vector3 targetPos)
        {
            float distance = Vector2.Distance(transform.position, targetPos);
            StretchBeamModel(distance);
        }

        // 빔/레이저형 투사체 전용: model에 BoxCollider2D가 있을 때만 세로 길이 늘리기.
        // 캐논처럼 CircleCollider2D + 일반 SpriteRenderer 조합은 건드리지 않는다.
        private void StretchBeamModel(float length)
        {
            if (model == null)
                return;

            if (!model.TryGetComponent<BoxCollider2D>(out var box))
                return;

            model.transform.localPosition = new Vector3(length / 2, 0, 0);
            box.size = new Vector2(1, length);

            if (model.TryGetComponent<SpriteRenderer>(out var sprite))
            {
                sprite.size = new Vector2(1, length);
            }
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
            if (data == null || _movementStrategy == null)
                return;

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

            if (target == _owner) return;

            ProcessEnter(target);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.TryGetComponent<IProjectileTarget>(out var target))
            {
                return;
            }

            if (target == _owner) return;

            ProcessStay(target);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<IProjectileTarget>(out var target))
            {
                return;
            }

            if (target == _owner) return;

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

            if (data == null)
                return;

            // 타겟팅 체크
            if (data != null && data.IsTargeting && _target != null)
            {
                if (target.TargetTransform != _target.TargetTransform)
                {
                    return;
                }

                if (target == _owner) return;
            }

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
                if (data.TriggerHandlePrefab != null)
                {
                    if (!data.TriggerHandlePrefab.HasTurretDamage)
                    {
                        target.TakeDamage(_damage);
                    }
                }
                else
                {
                    target.TakeDamage(_damage);
                }

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

            if (target == _owner) return;

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

            if (target == _owner) return;

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
                    if (target == _owner) return;

                    ProcessEnter(target);
                }
            }
        }
        #endregion

        public bool IsScaleByAttackRange()
        {
            if (data == null)
                return false;

            return data.ScaleByRange;
        }

        public ProjectileData GetData()
        {
            return data;
        }

        public void ReturnToPool()
        {
            TrySpawnTriggerHandle();
            ResourceManager.Instance.Destroy(gameObject);
        }

        private void TrySpawnTriggerHandle()
        {
            if (_isSpawnedTrigger)
                return;

            if (data == null || !data.IsSpawnTriggerHandle || data.TriggerHandlePrefab == null)
                return;

            _isSpawnedTrigger = true;

            TriggerHandle triggerHandle = ResourceManager.Instance.Spawn(data.TriggerHandlePrefab, transform.position, Quaternion.identity);

            // ProjectileData.scaleByRange + AttackRange 타입이 켜졌으면 trigger의 effective 반경이 포탑 AttackRange와
            // 일치하도록 base CircleCollider2D radius를 기준으로 비율 보정해 transform을 스케일한다.
            if (data.ScaleByRange && data.ScaleRangeType == ScaleByRangeType.AttackRange && _attackRange > 0f)
            {
                var circle = triggerHandle.GetComponent<CircleCollider2D>();
                float baseRadius = circle != null ? circle.radius : 1f;
                if (baseRadius > 0f)
                {
                    float scale = _attackRange / baseRadius;
                    triggerHandle.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }

            triggerHandle.Init(_damage);
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(data.DestroyDelay);
            ReturnToPool();
        }
    }
}