using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 파티클 기반 총알
    /// Particle 오브젝트에 맞게 PolygonCollider2D 모양이 동적으로 바뀝니다.
    /// </summary>
    public class ParticleProjectile : Projectile
    {
        // 분사 각도(콘 반각) 상한. 범위가 사거리에 비해 커져 이 각도를 넘으면 입자가 조준 방향이 아니라 옆으로 퍼져
        // 화염이 직각으로 누운 띠가 되고 피해 판정도 사라진다.
        public const float MAX_CONE_ANGLE = 60f;

        #region Field
        [SerializeField]
        [BoxGroup("ParticleCollider")]
        private float colliderUpdateInterval = 0.1f;
        [SerializeField]
        [BoxGroup("ParticleCollider")]
        private float particleRadius = 0.5f;
        [SerializeField]
        [BoxGroup("ParticleDensity")]
        [Tooltip("이 사거리(유닛)에서의 입자 밀도를 기준으로, 사거리가 길어지면 방출량을 길이 비례로 올려 면적당 밀도를 유지한다. 0이면 길이 보정 없음")]
        private float densityReferenceLength = 6f;
        #endregion

        private PolygonCollider2D _polygonCollider;
        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles;
        private readonly Vector2[] _trianglePoints = new Vector2[3];
        private readonly Vector2[] _fanPoints = new Vector2[4];
        private float _colliderUpdateTimer;
        private float _baseShapeAngle;
        private float _baseRateOverTime;

        private bool _isStoppingEmission;

        private void Awake()
        {
            SetupParticleCollider();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _isStoppingEmission = false;
            if (_particleSystem != null && !_particleSystem.isEmitting)
                _particleSystem.Play(true);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            UpdateParticleCollider();

            // 방출을 멈춘 뒤 이미 나간 입자가 다 사라지면 스스로 꺼진다.
            if (_isStoppingEmission && _particleSystem != null && !_particleSystem.IsAlive(true))
                gameObject.SetActive(false);
        }

        /// <summary>
        /// 새 입자 방출만 멈춘다. 이미 나간 입자는 끝까지 날아가며 판정도 유지하고, 전부 사라지면 꺼진다.
        /// </summary>
        public void StopEmission()
        {
            if (_particleSystem == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _isStoppingEmission = true;
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // 이 사거리에서 분사 각도가 상한에 닿는 범위(반폭). 이보다 큰 범위는 모양과 판정에 반영되지 않는다.
        public static float GetMaxAttackArea(float attackRange)
        {
            return attackRange * Mathf.Sin(MAX_CONE_ANGLE * Mathf.Deg2Rad);
        }

        /// <summary>
        /// 포탑 AttackArea/AttackRange를 실단위(유닛)로 받아 목표 크기에서 역산해 직접 설정한다.
        /// 길이(도달 거리) = 수명 × 입자 속도, 반폭 = 길이 × tan(콘 각도) — localScale을 건드리지 않아 입자 크기는 보존된다.
        /// 원본 대비 배율 방식이 아니라 기준값 실측이 필요 없고, 파티클 원본(속도·각도)을 바꿔도 스탯 수치가 실단위로 유지된다.
        /// 데미지 콜라이더는 파티클 실제 위치를 추적하므로 영역에 맞춰 자동으로 갱신된다.
        /// scaleRange가 0이면(주입 안 됨) AttackArea를 길이에도 사용한다.
        /// </summary>
        protected override void ApplyScaleByArea(float scaleRadius, float scaleRange = 0f)
        {
            if (scaleRadius <= 0f) return;
            if (_particleSystem == null) return;

            float particleSpeed = _particleSystem.main.startSpeedMultiplier;
            if (particleSpeed <= 0f) return;

            // 길이(도달 거리): 수명 = 사거리 ÷ 입자 속도. 동시 입자 수가 비례해 늘어 길이 방향 밀도는 유지된다.
            float targetLength = scaleRange > 0f ? scaleRange : scaleRadius;
            ParticleSystem.MainModule main = _particleSystem.main;
            main.startLifetimeMultiplier = targetLength / particleSpeed;

            // 반폭: 입자는 기울인 방향으로 사거리만큼 직진하므로 횡 반폭 = 사거리 × sin(각도) + 방출 반경.
            // 역산 = asin((반폭 − 방출반경) ÷ 사거리) — 도달 지점의 중심~가장자리가 정확히 AttackArea 유닛이 된다.
            // 각도는 MAX_CONE_ANGLE에서 멈춘다 — 그 너머로 열리면 입자가 앞으로 나가지 않는다.
            ParticleSystem.ShapeModule shape = _particleSystem.shape;
            float lateralReach = Mathf.Max(0f, scaleRadius - shape.radius);
            float targetAngle = Mathf.Min(Mathf.Asin(Mathf.Clamp01(lateralReach / targetLength)) * Mathf.Rad2Deg, MAX_CONE_ANGLE);
            shape.angle = targetAngle;

            // 폭이 넓어진 만큼 방출량을 원본 각도 대비 비율로 올려 폭 방향 밀도를 유지한다.
            // 길이도 기준 사거리 대비 비율로 곱한다 — 부채꼴 면적이 길이²에 비례하는데 동시 입자 수는 수명(길이)에만 비례해서,
            // 이 보정이 없으면 사거리가 길어질수록 면적당 밀도가 1/길이로 옅어진다.
            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            if (_baseShapeAngle > 0f)
            {
                float lengthFactor = densityReferenceLength > 0f ? targetLength / densityReferenceLength : 1f;
                emission.rateOverTimeMultiplier = _baseRateOverTime * (targetAngle / _baseShapeAngle) * lengthFactor;

                // 동시 입자 수(방출량 × 수명)가 상한을 넘으면 방출이 끊겨 구멍이 생기므로 상한을 함께 올린다.
                int requiredParticles = Mathf.CeilToInt(emission.rateOverTimeMultiplier * main.startLifetimeMultiplier * 1.25f);
                if (requiredParticles > main.maxParticles)
                    main.maxParticles = requiredParticles;
            }
        }

        #region ParticleCollider
        private void SetupParticleCollider()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null) return;

            _baseShapeAngle = _particleSystem.shape.angle;
            _baseRateOverTime = _particleSystem.emission.rateOverTimeMultiplier;

            _polygonCollider = GetComponent<PolygonCollider2D>();
            if (_polygonCollider == null)
            {
                _polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
            }

            _polygonCollider.isTrigger = true;

            // 파티클 배열 초기화 (최대 파티클 수만큼)
            int maxParticles = _particleSystem.main.maxParticles;
            if (maxParticles <= 0) maxParticles = 1000; // 기본값
            _particles = new ParticleSystem.Particle[maxParticles];
        }

        private void UpdateParticleCollider()
        {
            if (_particleSystem == null || _polygonCollider == null) return;

            _colliderUpdateTimer -= Time.fixedDeltaTime;
            if (_colliderUpdateTimer > 0f) return;

            _colliderUpdateTimer = colliderUpdateInterval;

            // 활성 파티클 수 가져오기
            int particleCount = _particleSystem.GetParticles(_particles);
            if (particleCount == 0)
            {
                // 파티클이 없으면 Collider 비활성화
                _polygonCollider.pathCount = 0;
                return;
            }

            // 점이 1개면 삼각형을 만들 수 없음 — 직전 콜라이더를 유지한다.
            if (particleCount == 1) return;

            // 변환 행렬을 1회만 받아 입자당 네이티브 호출을 줄인다 (PolygonCollider는 로컬 좌표 사용).
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Vector2 first = worldToLocal.MultiplyPoint3x4(_particles[0].position);

            if (particleCount == 2)
            {
                // 점이 2개면 세 번째 점을 수직 방향으로 만들어 삼각형을 완성한다.
                Vector2 second = worldToLocal.MultiplyPoint3x4(_particles[1].position);
                Vector2 midPoint = (first + second) * 0.5f;
                Vector2 dir = (second - first).normalized;
                Vector2 perp = new Vector2(-dir.y, dir.x); // 수직 방향
                _trianglePoints[0] = first;
                _trianglePoints[1] = second;
                _trianglePoints[2] = midPoint + perp * particleRadius;
            }
            else
            {
                // 분사는 로컬 +x로 나가며 위아래(±y)로 퍼지는 부채꼴이다. 극점 4개(뒤·위·앞·아래)를 이으면
                // 총구~양쪽 모서리~끝을 덮는 사각형이 된다. 극점 3개(뒤·앞·위) 삼각형은 부채꼴의 위쪽 절반만 덮는다.
                // 위치 목록을 만들지 않고 순회하며 바로 찾는다 (colliderUpdateInterval마다 도는 경로라 갱신당 GC 할당 0 유지).
                Vector2 leftmost = first;
                Vector2 rightmost = first;
                Vector2 topmost = first;
                Vector2 bottommost = first;

                for (int i = 1; i < particleCount; i++)
                {
                    Vector2 localPos = worldToLocal.MultiplyPoint3x4(_particles[i].position);

                    if (localPos.x < leftmost.x) leftmost = localPos;
                    if (localPos.x > rightmost.x) rightmost = localPos;
                    if (localPos.y > topmost.y) topmost = localPos;
                    if (localPos.y < bottommost.y) bottommost = localPos;
                }

                // 뒤 → 위 → 앞 → 아래 순서는 볼록 껍질을 한 방향으로 도는 순서라 감김을 따로 맞출 필요가 없다.
                _fanPoints[0] = leftmost;
                _fanPoints[1] = topmost;
                _fanPoints[2] = rightmost;
                _fanPoints[3] = bottommost;

                if (_polygonCollider.pathCount != 1)
                    _polygonCollider.pathCount = 1;
                _polygonCollider.SetPath(0, _fanPoints);
                return;
            }

            // 감김 방향만 외적 부호로 일정하게 맞춘다 (PolygonCollider는 일관된 감김이면 충분).
            Vector2 edge1 = _trianglePoints[1] - _trianglePoints[0];
            Vector2 edge2 = _trianglePoints[2] - _trianglePoints[0];
            if (edge1.x * edge2.y - edge1.y * edge2.x > 0f)
                (_trianglePoints[1], _trianglePoints[2]) = (_trianglePoints[2], _trianglePoints[1]);

            if (_polygonCollider.pathCount != 1)
                _polygonCollider.pathCount = 1;
            _polygonCollider.SetPath(0, _trianglePoints);
        }
        #endregion
    }
}

