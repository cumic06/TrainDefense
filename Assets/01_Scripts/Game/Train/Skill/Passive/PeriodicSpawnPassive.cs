using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 주기적으로 owner 위치에 투사체 발사. (엘리트 냉기 눈덩이: 3s마다)
    /// 이벤트 구독 대신 Tick(deltaTime) 사용.
    /// DSL: "PeriodicSpawn:interval:projectile_prefab_id:radius[:damage_mul]"
    /// damage_mul 생략 시 1.0 (owner 공격력 그대로).
    /// </summary>
    public class PeriodicSpawnPassive : TrainPassiveSkill
    {
        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        public float Interval { get; private set; } = 3f;
        public string ProjectilePrefabId { get; private set; }
        public float Radius { get; private set; } = 4f;
        public float DamageMultiplier { get; private set; } = 1f;

        // 눈덩이 등은 owner 사거리가 아니라 화면 전체에서 가장 가까운 적을 조준해야 하므로 넉넉한 탐색 반경 사용.
        private const float ScreenSearchRadius = 100f;

        private Projectile _cachedPrefab;
        private bool _loadAttempted;
        private float _timer;

        public static PeriodicSpawnPassive From(string[] parts)
        {
            if (parts.Length < 4) return null;
            if (!float.TryParse(parts[1], out float interval) || interval <= 0f) return null;
            if (!float.TryParse(parts[3], out float radius) || radius <= 0f) return null;
            float damageMultiplier = 1f;
            if (parts.Length >= 5 && !float.TryParse(parts[4], out damageMultiplier))
            {
                damageMultiplier = 1f;
            }
            return new PeriodicSpawnPassive
            {
                Interval = interval,
                ProjectilePrefabId = parts[2].Trim(),
                Radius = radius,
                DamageMultiplier = damageMultiplier
            };
        }

        public override void Tick(float deltaTime)
        {
            if (Owner == null || Owner.IsDead) return;

            _timer += deltaTime;
            if (_timer < Interval) return;

            var prefab = LoadPrefab();
            if (prefab == null) return;

            // 새 총알(눈덩이 등)은 화면 안(맵 전체)에서 가장 가까운 적을 조준해 발사한다. (owner 사거리와 무관)
            Monster nearest = FindNearestMonster(ScreenSearchRadius);
            if (nearest == null) return; // 타겟 없으면 쿨 유지(다음 프레임 재시도) → 적 등장 시 즉시 발사

            _timer = 0f;

            if (Owner is RangeTrain range)
            {
                range.SpawnExternalProjectile(prefab, Radius, nearest, DamageMultiplier);
            }
            else if (Owner is TurretTrain turret)
            {
                turret.SpawnExternalProjectileAtSelf(prefab, Radius, DamageMultiplier, target: nearest);
            }
        }

        private Monster FindNearestMonster(float radius)
        {
            var colliders = Physics2D.OverlapCircleAll(Owner.transform.position, radius);
            Monster nearest = null;
            float minSqrDist = float.MaxValue;
            foreach (var col in colliders)
            {
                if (!col.TryGetComponent<Monster>(out var m) || !m.IsActive) continue;
                float sqrDist = (Owner.transform.position - m.transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist) { minSqrDist = sqrDist; nearest = m; }
            }
            return nearest;
        }

        private Projectile LoadPrefab()
        {
            if (_cachedPrefab != null) return _cachedPrefab;
            if (_loadAttempted) return null;
            _loadAttempted = true;
            if (string.IsNullOrEmpty(ProjectilePrefabId)) return null;

            var go = Resources.Load<GameObject>($"{PROJECTILE_PREFAB_PATH}{ProjectilePrefabId}");
            if (go == null)
            {
                Debug.LogWarning($"PeriodicSpawnPassive: prefab not found at '{PROJECTILE_PREFAB_PATH}{ProjectilePrefabId}'");
                return null;
            }
            _cachedPrefab = go.GetComponent<Projectile>();
            return _cachedPrefab;
        }
    }
}
