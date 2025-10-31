using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Projectile : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private float speed;
        [SerializeField]
        private float destroyDelay;
        [SerializeField]
        private bool isParticleProjectile;

        [SerializeField]
        [BoxGroup("TickProjectile")]
        private bool isTickProjectile;
        [SerializeField]
        [BoxGroup("TickProjectile")]
        private float tickDamageInterval = 0.1f;

        [SerializeField]
        [BoxGroup("SlowProjectile")]
        private bool isSlowProjectile;
        [SerializeField]
        [BoxGroup("SlowProjectile")]
        private float slowValue = 0.5f;

        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        private bool isShoveProjectile;
        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        private float shovePower = 1f;
        [SerializeField]
        [BoxGroup("ShoveProjectile")]
        private float shoveDuration = 0.5f;

        [SerializeField]
        [BoxGroup("ParticleCollider")]
        private float colliderUpdateInterval = 0.1f;
        [SerializeField]
        [BoxGroup("ParticleCollider")]
        private float particleRadius = 0.5f;
        #endregion

        private int _damage;
        private Coroutine _destroyCoroutine;

        private Dictionary<Monster, float> _monsterDamageTimers = new();

        private PolygonCollider2D _polygonCollider;
        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles;
        private float _colliderUpdateTimer;

        #region Enable/Disable
        private void Awake()
        {
            if (isParticleProjectile)
            {
                SetupParticleCollider();
            }
        }

        private void OnEnable()
        {
            if (destroyDelay <= 0) return;

            if (_destroyCoroutine != null)
            {
                StopCoroutine(_destroyCoroutine);
            }
            _destroyCoroutine = StartCoroutine(DestroyCoroutine());
        }

        private void OnDisable()
        {
            _monsterDamageTimers.Clear();
        }

        private void SetupParticleCollider()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null) return;

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
        #endregion

        public void Init(int damage)
        {
            _damage = damage;
        }

        private void FixedUpdate()
        {
            Move();

            if (isParticleProjectile)
            {
                UpdateParticleCollider();
            }
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
            List<Vector2> particlePositions = new List<Vector2>(particleCount);
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
            List<Vector2> sorted = new List<Vector2>(points);
            sorted.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.y - center.y, a.x - center.x);
                float angleB = Mathf.Atan2(b.y - center.y, b.x - center.x);
                return angleA.CompareTo(angleB);
            });

            return sorted;
        }

        protected virtual void Move()
        {
            if (speed <= 0) return;

            transform.Translate(Vector3.right * Time.deltaTime * speed);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                if (isParticleProjectile)
                {
                    // 처음 들어올 때는 즉시 데미지 적용
                    if (!_monsterDamageTimers.ContainsKey(monster))
                    {
                        _monsterDamageTimers[monster] = Time.time;
                        monster.TakeDamage(_damage);
                    }

                    if (isShoveProjectile)
                    {
                        if (monster == null) return;
                        monster.Shove(shovePower, shoveDuration);
                    }
                }
                else
                {
                    if (isTickProjectile)
                    {
                        // 처음 들어올 때는 즉시 데미지 적용
                        if (!_monsterDamageTimers.ContainsKey(monster))
                        {
                            _monsterDamageTimers[monster] = Time.time;
                            monster.TakeDamage(_damage);
                        }
                    }
                    else
                    {
                        monster.TakeDamage(_damage);
                        ResourceManager.Instance.Destroy(gameObject);
                    }

                    if (isShoveProjectile)
                    {
                        if (monster == null) return;
                        monster.Shove(shovePower, shoveDuration);
                    }
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                if (isParticleProjectile)
                {
                    // Monster가 Dictionary에 있는지 확인하고, tickDamageInterval 시간이 지났으면 데미지 적용
                    if (_monsterDamageTimers.ContainsKey(monster))
                    {
                        float lastDamageTime = _monsterDamageTimers[monster];
                        if (Time.time - lastDamageTime >= tickDamageInterval)
                        {
                            if (monster == null) return;
                            monster.TakeDamage(_damage);
                            _monsterDamageTimers[monster] = Time.time;
                        }
                    }

                    if (isSlowProjectile)
                    {
                        if (monster == null) return;
                        monster.Slow(slowValue);
                    }

                    if (isShoveProjectile)
                    {
                        monster.Shove(shovePower, shoveDuration);
                    }
                }
                else
                {
                    if (isTickProjectile)
                    {
                        // Monster가 Dictionary에 있는지 확인하고, tickDamageInterval 시간이 지났으면 데미지 적용
                        if (_monsterDamageTimers.ContainsKey(monster))
                        {
                            float lastDamageTime = _monsterDamageTimers[monster];
                            if (Time.time - lastDamageTime >= tickDamageInterval)
                            {
                                if (monster == null) return;
                                monster.TakeDamage(_damage);
                                _monsterDamageTimers[monster] = Time.time;
                            }
                        }
                    }

                    if (isSlowProjectile)
                    {
                        if (monster == null) return;
                        monster.Slow(slowValue);
                    }

                    if (isShoveProjectile)
                    {
                        monster.Shove(shovePower, shoveDuration);
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                if (isParticleProjectile)
                {
                    // Dictionary에서 제거
                    _monsterDamageTimers.Remove(monster);

                    if (isSlowProjectile)
                    {
                        if (monster == null || !monster.gameObject.activeInHierarchy) return;
                        monster.ResetMoveSpeed();
                    }
                }
                else
                {
                    if (isTickProjectile)
                    {
                        // Dictionary에서 제거
                        _monsterDamageTimers.Remove(monster);
                    }

                    if (isSlowProjectile)
                    {
                        if (monster == null || !monster.gameObject.activeInHierarchy) return;
                        monster.ResetMoveSpeed();
                    }
                }
            }
        }

        private bool IsMonster(Collider2D other, out Monster monster)
        {
            return other.TryGetComponent(out monster);
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(destroyDelay);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}