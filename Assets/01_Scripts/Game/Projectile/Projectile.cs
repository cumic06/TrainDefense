using System.Collections;
using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
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

        protected float _damage;
        protected float _criticalChance;
        protected float _criticalDamage;
        protected IProjectileTarget _target;
        protected IProjectileTarget _owner;
        protected IMovementStrategy _movementStrategy;

        public bool SuppressShoveEffect { get; set; }
        public float ShoveScale { get; set; } = 1f;
        protected Coroutine _destroyCoroutine;
        protected Dictionary<IProjectileTarget, float> _damageTimers = new();
        // 틱 범위 안에 현재 피해를 받는 대상이 있는지. (RangeTrain이 공격 루프 사운드 게이트로 사용)
        public bool HasTickTargets => _damageTimers.Count > 0;
        protected float _age;
        protected bool _isSpawnedTrigger;
        protected float _scaleRadius;
        protected int _hitCount;
        protected readonly HashSet<IProjectileTarget> _hitSet = new();

        // 런타임 넉백 오버라이드 (KnockbackOnHitPassive 등 패시브가 주입)
        private bool _runtimeHasShove;
        private float _runtimeShovePower;
        private float _runtimeShoveDuration;
        private float _scale = 1f;
        private Vector3 _baseScale = Vector3.one;
        private bool _baseScaleCaptured;
        [SerializeField] private float baseScaleArea = 3f;
        // 빔 굵기 기준 스케일(콜라이더 폭 1유닛 기준). 전체 굵기(유닛) = baseRangeScale × widthMul(= 2×AttackArea 반폭).
        [SerializeField] private float baseRangeScale = 1f;

        #region Enable/Disable

        protected virtual void OnEnable()
        {
            _age = 0f;
            _damageTimers.Clear();
            _hitSet.Clear();

            // OnDisable이 전략을 비우는데, 버스트 장판(냉기)은 Init 없이 SetActive(true)로만 재활성된다.
            // 전략이 null이면 FixedUpdate가 조기 return해 _age(틱 시계)가 멈춰 틱 데미지가 안 들어간다.
            if (data != null && _movementStrategy == null)
            {
                _movementStrategy = CreateMovementStrategy(data.MovementType);
            }
            _hitCount = 0;
            _isSpawnedTrigger = false;
            // 페이드 도중 피격 소멸로 코루틴이 끊기면 알파가 낮은 채 풀에 남는다 → 재사용 시 원복
            if (_fadeSprites != null)
                _SetFadeAlpha(1f);
            SuppressShoveEffect = false;
            ShoveScale = 1f;
            _runtimeHasShove = false;
            _runtimeShovePower = 0f;
            _runtimeShoveDuration = 0f;
            _scale = 1f;
            // trigger 스폰형(미사일)만 루트 스케일 리셋 → 풀 재사용 잔존 방지. 다른 투사체는 안 건드림.
            if (data != null && data.IsSpawnTriggerHandle)
            {
                if (!_baseScaleCaptured) { _baseScale = transform.localScale; _baseScaleCaptured = true; }
                transform.localScale = _baseScale;
            }

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
        /// <param name="scaleRange">분사 길이 스케일 (AttackRange 값). ParticleProjectile만 사용</param>
        public virtual void Init(float damage, IProjectileTarget owner, IProjectileTarget target = null, float scaleRadius = 0f, float criticalChance = 0f, float criticalDamage = 0f, float scaleRange = 0f)
        {
            _damage = damage;
            _owner = owner;
            _target = target;
            _scaleRadius = scaleRadius;
            _criticalChance = criticalChance;
            _criticalDamage = criticalDamage;

            if (data != null)
            {
                InitializeWithConfig(scaleRadius, scaleRange);
            }
        }

        private void InitializeWithConfig(float scaleRadius = 0f, float scaleRange = 0f)
        {
            // 이동 전략 초기화
            _movementStrategy = CreateMovementStrategy(data.MovementType);
            _movementStrategy?.Initialize(this, data, _target);

            // 이동이 루트 회전(+X) 기준이라 루트의 LookAt2D는 유지하고 model만 직립시킨다.
            if (!data.IsRotateModel && model != null)
            {
                model.transform.rotation = Quaternion.identity;
            }

            // AttackArea에 따른 스케일 조정
            if (data.ScaleByArea && data.ScaleRangeType == ScaleByRangeType.Area && scaleRadius > 0f)
            {
                ApplyScaleByArea(scaleRadius, scaleRange);
            }
            else if (data.ScaleByArea && data.ScaleRangeType == ScaleByRangeType.TargetRange && _target != null)
            {
                ApplyScaleByTargetRange(_target.TargetTransform.position);
            }
        }

        // 빔류(StretchBeamModel): 길이는 AttackRange(scaleRange), 폭은 AttackArea(scaleRadius)로 늘린다.
        // scaleRange가 없으면(0) 길이도 scaleRadius로 폴백.
        protected virtual void ApplyScaleByArea(float scaleRadius, float scaleRange = 0f)
        {
            // 캐논류(trigger 스폰 + 빔 아님): 투사체 루트를 AttackArea 비례로 스케일 (model은 자식이라 따라 커짐).
            // baseScaleArea = 원본 크기(localScale 1배)에 대응하는 AttackArea. 매 발사 재계산이라 풀 잔존 없음.
            if (data != null && data.IsSpawnTriggerHandle && scaleRadius > 0f && baseScaleArea > 0f)
            {
                transform.localScale = _baseScale * (scaleRadius / baseScaleArea);
                return;
            }
            // scaleRadius는 반경(중심~가장자리) 의미로 통일 — 빔 전체 굵기는 ×2.
            StretchBeamModel(scaleRange > 0f ? scaleRange : scaleRadius, scaleRadius * 2f);
        }

        protected virtual void ApplyScaleByTargetRange(Vector3 targetPos)
        {
            float distance = Vector2.Distance(transform.position, targetPos);
            StretchBeamModel(distance);
        }

        // 빔/레이저형 투사체 전용: model에 BoxCollider2D가 있을 때만 늘린다.
        // 길이는 sprite.size(Tiled)로 늘리고, 폭은 model 스케일로 stretch한다.
        // (sprite.size로 폭을 늘리면 Tiled drawMode가 가로로 반복돼 스프라이트가 여러 개로 보임)
        // 레이저 model은 -90도 회전 상태라 model 스케일 x축=폭. widthMul=1이면 원본 굵기 유지(전기 등).
        // 캐논(CircleCollider)은 건드리지 않는다.
        private void StretchBeamModel(float length, float widthMul = 1f)
        {
            if (model == null)
                return;

            if (!model.TryGetComponent<BoxCollider2D>(out var box))
                return;

            model.transform.localPosition = new Vector3(length / 2, 0, 0);
            box.size = new Vector2(1, length);
            var ls = model.transform.localScale;
            model.transform.localScale = new Vector3(baseRangeScale * widthMul, ls.y, ls.z);

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

            // 소유 포탑이 죽으면 정지형 지속 범위 공격(NonMovement: 냉기/화염 등)을 즉시 멈춘다.
            // 비활성화만 하므로 부활 후 공격 재개 시 다시 켜진다. 날아가는 투사체(Linear 등)는 그대로 둔다.
            if (data.MovementType == MovementType.NonMovement && _owner is Train ownerTrain && ownerTrain.IsDead)
            {
                gameObject.SetActive(false);
                return;
            }

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

            if (_owner is Train && target is Train) return;

            ProcessEnter(target);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.TryGetComponent<IProjectileTarget>(out var target))
            {
                return;
            }

            if (_owner is Train && target is Train) return;

            ProcessStay(target);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<IProjectileTarget>(out var target))
            {
                return;
            }

            if (_owner is Train && target is Train) return;

            ProcessExit(target);
        }
        #endregion

        /// <summary>
        /// 치명타 판정 후 최종 데미지와 치명타 여부를 반환합니다.
        /// </summary>
        // 크리티컬 시 기본 추가 데미지 비율(%). 실제 크리 보너스 = BaseCriticalDamagePercent + CriticalDamage.
        // Detail UI 표기(TurretTrain/RangeTrain.GetStatDetails)도 이 값을 참조한다.
        public const float BaseCriticalDamagePercent = 100f;

        protected (float finalDamage, bool isCritical) CalculateCriticalDamage()
        {
            bool isCritical = _criticalChance > 0f && UtilMath.CheckProbability(_criticalChance);
            float finalDamage = _damage;
            if (isCritical)
            {
                finalDamage += _damage * (BaseCriticalDamagePercent + _criticalDamage) / 100f;
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
                // Pierce 모드에서는 TriggerHandle이 마지막에만 소환되므로 직접 데미지 적용
                bool shouldDealDamage = data.Pierce
                    || data.TriggerHandlePrefab == null
                    || !data.TriggerHandlePrefab.HasTurretDamage;

                if (shouldDealDamage)
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

            if (_runtimeHasShove && !SuppressShoveEffect)
            {
                target.Shove(_runtimeShovePower * ShoveScale, _runtimeShoveDuration);
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

            // 슬로우 효과 (Stay 중 지속 적용). owner가 둔화율을 제공하면 그 값, 아니면 config 기본값.
            // 자동복원 슬로우(SlowDuration)를 매 Stay마다 갱신 — 장판이 꺼져도(버스트 종료) 그 시간 후 자연 해제.
            if (data.HasSlowEffect)
            {
                float slowValue = _owner is ISlowProvider slowProvider ? slowProvider.GetSlowValue() : data.SlowValue;
                target.Slow(slowValue, data.SlowDuration);
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
                    if (target == _owner) continue;

                    ProcessEnter(target);
                }
            }
        }
        #endregion

        public void SetScale(float scale)
        {
            _scale = scale;
            transform.localScale = _baseScale * scale;
        }

        // Init 이후 데미지에 배율을 곱한다. (오버라이드 투사체 전용 강화 — 크리 계산도 곱해진 값 기준)
        public void MultiplyDamage(float multiplier)
        {
            _damage *= multiplier;
        }

        public void SetRuntimeShove(float power, float duration)
        {
            _runtimeHasShove = true;
            _runtimeShovePower = power;
            _runtimeShoveDuration = duration;
        }

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
            if (data.ShakeOnDestroy)
                GameEventSystem.Publish(new CameraShakeEvent(data.ShakeIntensity, data.ShakeDuration));

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

            var (finalDamage, isCritical) = CalculateCriticalDamage();
            triggerHandle.Init(finalDamage, _owner, isCritical);

            if (_runtimeHasShove && !SuppressShoveEffect)
                triggerHandle.SetRuntimeShove(_runtimeShovePower * ShoveScale, _runtimeShoveDuration);

            // 미사일 스케일(거대한 미사일)을 trigger 폭발 반경에도 반영 → 데미지 범위도 비례 확대
            if (_scale != 1f)
                triggerHandle.transform.localScale *= _scale;
        }

        // 사거리 제한: 수명을 "사거리를 날아가는 시간"으로 덮어써 총알이 탐지 사거리 너머 화면 끝까지 타격하지 않게 한다.
        // 사거리 업그레이드가 있어 config 고정값이 아니라 발사 시점의 AttackRange로 매번 계산한다.
        // 소멸은 기존 시간 소멸과 동일 경로(ReturnToPool) — 미사일(trigger 스폰형)은 그 자리에서 폭발로 마감된다.
        public void LimitLifetimeByRange(float attackRange)
        {
            if (data == null || data.Speed <= 0f)
                return;

            if (_destroyCoroutine != null)
                StopCoroutine(_destroyCoroutine);
            _destroyCoroutine = StartCoroutine(DestroyCoroutine(attackRange / data.Speed));
        }

        private IEnumerator DestroyCoroutine(float overrideDelay = 0f)
        {
            yield return new WaitForSeconds(overrideDelay > 0f ? overrideDelay : data.DestroyDelay);

            if (this == null)
                yield break;

            // 만료 소멸은 점점 투명해지며 사라진다. (피격 소멸은 즉시, 미사일(trigger 스폰형)은 폭발이 마감이라 페이드 없음)
            if (!data.IsSpawnTriggerHandle)
                yield return _DespawnFadeCoroutine();

            ReturnToPool();
        }

        private const float DESPAWN_FADE_DURATION = 0.15f;
        private SpriteRenderer[] _fadeSprites;
        private float[] _fadeBaseAlphas;

        private IEnumerator _DespawnFadeCoroutine()
        {
            if (_fadeSprites == null)
            {
                _fadeSprites = GetComponentsInChildren<SpriteRenderer>();
                _fadeBaseAlphas = new float[_fadeSprites.Length];
                for (int i = 0; i < _fadeSprites.Length; i++)
                    _fadeBaseAlphas[i] = _fadeSprites[i].color.a;
            }

            for (float elapsed = 0f; elapsed < DESPAWN_FADE_DURATION; elapsed += Time.deltaTime)
            {
                _SetFadeAlpha(1f - elapsed / DESPAWN_FADE_DURATION);
                yield return null;
            }

            // 풀 재사용 대비 원복 — 같은 프레임에 비활성화되므로 화면에 되살아나 보이지 않는다.
            _SetFadeAlpha(1f);
        }

        private void _SetFadeAlpha(float ratio)
        {
            for (int i = 0; i < _fadeSprites.Length; i++)
            {
                var sprite = _fadeSprites[i];
                if (sprite == null) continue;
                var color = sprite.color;
                color.a = _fadeBaseAlphas[i] * ratio;
                sprite.color = color;
            }
        }
    }
}