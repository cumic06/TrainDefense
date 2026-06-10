using System.Collections.Generic;
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
        #region Field
        [SerializeField]
        [BoxGroup("ParticleCollider")]
        private float colliderUpdateInterval = 0.1f;
        [SerializeField]
        [BoxGroup("ParticleCollider")]
        private float particleRadius = 0.5f;
        #endregion

        private PolygonCollider2D _polygonCollider;
        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles;
        private float _colliderUpdateTimer;
        private float _baseStartLifetime;
        private float _baseShapeAngle;
        private float _baseRateOverTime;

        [SerializeField]
        [BoxGroup("ParticleCollider")]
        [Tooltip("파티클이 localScale=1일 때 대응하는 AttackArea 기준값. 엑셀 기본 AttackArea와 맞춰 설정.")]
        private float baseScaleRadius = 8f;

        [SerializeField]
        [BoxGroup("ParticleCollider")]
        [Tooltip("파티클이 localScale.x=1일 때 대응하는 AttackRange 기준값. 화염 포탑 기본 AttackRange와 맞춰 설정.")]
        private float baseRangeRadius = 8f;

        private void Awake()
        {
            SetupParticleCollider();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            UpdateParticleCollider();
        }

        /// <summary>
        /// 포탑 AttackArea/AttackRange 변경 시 영역만 키운다. localScale을 건드리지 않아 입자 크기는 보존된다.
        /// 길이(AttackRange)는 수명으로, 폭(AttackArea)은 Cone 각도와 방출량으로 조정한다.
        /// 데미지 콜라이더는 파티클 실제 위치를 추적하므로 영역에 맞춰 자동으로 갱신된다.
        /// scaleRange가 0이면(주입 안 됨) AttackArea를 길이·폭 양쪽에 적용한다.
        /// </summary>
        protected override void ApplyScaleByArea(float scaleRadius, float scaleRange = 0f)
        {
            if (scaleRadius <= 0f) return;
            if (_particleSystem == null) return;

            float areaRatio = scaleRadius / baseScaleRadius;
            float rangeRatio = scaleRange > 0f ? scaleRange / baseRangeRadius : areaRatio;

            // 길이(range)는 수명으로 늘린다. 동시 입자 수가 비례해 늘어 길이 방향 밀도는 유지된다.
            ParticleSystem.MainModule main = _particleSystem.main;
            main.startLifetimeMultiplier = _baseStartLifetime * rangeRatio;

            // 폭(area)은 Cone 각도로 부채꼴을 벌린다.
            ParticleSystem.ShapeModule shape = _particleSystem.shape;
            shape.angle = _baseShapeAngle * areaRatio;

            // 폭이 넓어진 만큼 방출량을 올려 폭 방향 밀도를 유지한다.
            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            emission.rateOverTimeMultiplier = _baseRateOverTime * areaRatio;
        }

        #region ParticleCollider
        private void SetupParticleCollider()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null) return;

            _baseStartLifetime = _particleSystem.main.startLifetimeMultiplier;
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

            // World Space 파티클 위치 수집
            List<Vector2> particlePositions = new(particleCount);
            for (int i = 0; i < particleCount; i++)
            {
                Vector3 worldPos = _particles[i].position;
                // World Space에서 Local Space로 변환 (PolygonCollider는 Local Space 사용)
                Vector3 localPos = transform.InverseTransformPoint(worldPos);
                particlePositions.Add(new Vector2(localPos.x, localPos.y));
            }

            // 파티클의 가장 끝부분 3개 점 찾기
            List<Vector2> trianglePoints = GetBoundaryPoints(particlePositions);

            if (trianglePoints.Count == 3)
            {
                // 삼각형을 시계방향으로 정렬 (PolygonCollider는 시계방향으로 정렬된 점 필요)
                trianglePoints = SortTriangleClockwise(trianglePoints);
                _polygonCollider.pathCount = 1;
                _polygonCollider.SetPath(0, trianglePoints);
            }
        }

        private List<Vector2> GetBoundaryPoints(List<Vector2> points)
        {
            if (points.Count == 0)
            {
                return new List<Vector2>();
            }

            if (points.Count == 1)
            {
                // 점이 1개면 삼각형을 만들 수 없음
                return new List<Vector2>();
            }

            if (points.Count == 2)
            {
                // 점이 2개면 세 번째 점을 추가 (위쪽으로 약간 오프셋)
                Vector2 midPoint = (points[0] + points[1]) * 0.5f;
                Vector2 dir = (points[1] - points[0]).normalized;
                Vector2 perp = new Vector2(-dir.y, dir.x); // 수직 방향
                Vector2 thirdPoint = midPoint + perp * particleRadius;
                return new List<Vector2> { points[0], points[1], thirdPoint };
            }

            // 가장 왼쪽, 오른쪽, 위쪽 점 찾기
            Vector2 leftmost = points[0];
            Vector2 rightmost = points[0];
            Vector2 topmost = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].x < leftmost.x)
                {
                    leftmost = points[i];
                }
                if (points[i].x > rightmost.x)
                {
                    rightmost = points[i];
                }
                if (points[i].y > topmost.y)
                {
                    topmost = points[i];
                }
            }

            // 3개 점 반환 (왼쪽, 오른쪽, 위쪽)
            return new List<Vector2> { leftmost, rightmost, topmost };
        }

        private List<Vector2> SortTriangleClockwise(List<Vector2> points)
        {
            if (points.Count != 3) return points;

            // 중심점 계산
            Vector2 center = (points[0] + points[1] + points[2]) / 3f;

            // 중심점 기준으로 각도 순으로 정렬
            List<Vector2> sorted = new(points);
            sorted.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.y - center.y, a.x - center.x);
                float angleB = Mathf.Atan2(b.y - center.y, b.x - center.x);
                return angleA.CompareTo(angleB);
            });

            return sorted;
        }
        #endregion
    }
}

