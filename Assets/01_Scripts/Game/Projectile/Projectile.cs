using System.Collections;
using System.Collections.Generic;
using Cumic;
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
        protected float _criticalChance;
        protected float _criticalDamage;
        protected IProjectileTarget _target;
        protected IProjectileTarget _owner;
        protected IMovementStrategy _movementStrategy;

        public bool SuppressShoveEffect { get; set; }
        public float ShoveScale { get; set; } = 1f;
        protected Coroutine _destroyCoroutine;
        protected Dictionary<IProjectileTarget, float> _damageTimers = new();
        protected float _age;
        protected bool _isSpawnedTrigger;
        protected float _scaleRadius;
        protected int _hitCount;
        protected readonly HashSet<IProjectileTarget> _hitSet = new();

        #region Enable/Disable

        protected virtual void OnEnable()
        {
            _age = 0f;
            _damageTimers.Clear();
            _hitSet.Clear();
            _hitCount = 0;
            _isSpawnedTrigger = false;
            SuppressShoveEffect = false;
            ShoveScale = 1f;

            if (data != null && data.DestroyDelay > 0)
            {
                if (_destroyCoroutine != null)
                {
                    StopCoroutine(_destroyCoroutine);
                }
                _destroyCoroutine = StartCoroutine(DestroyCoroutine());
            }

            if (data.ScaleByArea && data.ScaleRangeType == ScaleByRangeType.TargetRange && _target != null)
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
        /// <param name="scaleRadius">AoE/스케일 반경 (AttackArea 값)</param>
        public virtual void Init(int damage, IProjectileTarget owner, IProjectileTarget target = null, float scaleRadius = 0f, float criticalChance = 0f, float criticalDamage = 0f)
        {
            _damage = damage;
            _owner = owner;
            _target = target;
            _scaleRadius = scaleRadius;
            _criticalChance = criticalChance;
            _criticalDamage = criticalDamage;

            if (data != null)
            {
                InitializeWithConfig(scaleRadius);
            }
        }

        private void InitializeWithConfig(float scaleRadius = 0f)
        {
            // 이동 전략 초기화
            _movementStrategy = CreateMovementStrategy(data.MovementType);
            _movementStrategy?.Initialize(this, data, _target);

            // AttackArea에 따른 스케일 조정
            if (data.ScaleByArea && data.ScaleRangeType == ScaleByRangeType.Area && scaleRadius > 0f)
            {
                ApplyScaleByArea(scaleRadius);
            }
        }

        private void ApplyScaleByArea(float scaleRadius)
        {
            StretchBeamModel(scaleRadius);
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

        /// <summary>
        /// 치명타 판정 후 최종 데미지와 치명타 여부를 반환합니다.
        /// </summary>
        private const float BaseCriticalDamagePercent = 30f;

        protected (int finalDamage, bool isCritical) CalculateCriticalDamage()
        {
            bool isCritical = _criticalChance > 0f && UtilMath.CheckProbability(_criticalChance);
            int finalDamage = _damage;
            if (isCritical)
            {
                // 기본 30% + 추가 치명타 피해량 스탯
                finalDamage += Mathf.RoundToInt(_damage * (BaseCriticalDamagePercent + _criticalDamage) / 100f);
            }
            return (finalDamage, isCritical);
        }

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
                    var (finalDamage, isCritical) = CalculateCriticalDamage();
                    target.TakeDamage(finalDamage, isCritical);
                }
            }
            else
            {
                // 관통 모드: 동일 타겟 중복 히트 방지
                if (data.Pierce && !_hitSet.Add(target))
                {
                    return;
                }

                var (finalDamage, isCritical) = CalculateCriticalDamage();
                if (data.TriggerHandlePrefab != null)
                {
                    if (!data.TriggerHandlePrefab.HasTurretDamage)
                    {
                        target.TakeDamage(finalDamage, isCritical);
                    }
                }
                else
                {
                    target.TakeDamage(finalDamage, isCritical);
                }

                if (data.Pierce)
                {
                    _hitCount++;
                    if (_hitCount >= data.MaxPenetration)
                    {
                        ReturnToPool();
                        return;
                    }
                }
                else if (data.DestroyOnTriggerEnter)
                {
                    ReturnToPool();
                    return;
                }
            }

            // 상태 효과 적용
            if (data.HasShoveEffect && !SuppressShoveEffect)
            {
                target.Shove(data.ShovePower * ShoveScale, data.ShoveDuration);
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
                        var (finalDamage, isCritical) = CalculateCriticalDamage();
                        target.TakeDamage(finalDamage, isCritical);
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
            if (data.HasShoveEffect && !SuppressShoveEffect)
            {
                target.Shove(data.ShovePower * ShoveScale, data.ShoveDuration);
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

        public bool IsScaleByArea()
        {
            if (data == null)
                return false;

            return data.ScaleByArea;
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

            // ScaleByArea + Area 타입이면 TriggerHandle의 반경을 AttackArea 기준으로 보정
            if (data.ScaleByArea && data.ScaleRangeType == ScaleByRangeType.Area && _scaleRadius > 0f)
            {
                var circle = triggerHandle.GetComponent<CircleCollider2D>();
                float baseRadius = circle != null ? circle.radius : 1f;
                if (baseRadius > 0f)
                {
                    float scale = _scaleRadius / baseRadius;
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