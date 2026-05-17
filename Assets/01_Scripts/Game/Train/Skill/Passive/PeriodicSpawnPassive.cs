using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 주기적으로 owner 위치에 투사체 발사. (엘리트 냉기 눈덩이: 3s마다)
    /// 이벤트 구독 대신 Tick(deltaTime) 사용.
    /// DSL: "PeriodicSpawn:interval:projectile_prefab_id:radius"
    /// </summary>
    public class PeriodicSpawnPassive : TrainPassiveSkill
    {
        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        public float Interval { get; private set; } = 3f;
        public string ProjectilePrefabId { get; private set; }
        public float Radius { get; private set; } = 4f;

        private Projectile _cachedPrefab;
        private bool _loadAttempted;
        private float _timer;

        public static PeriodicSpawnPassive From(string[] parts)
        {
            if (parts.Length < 4) return null;
            if (!float.TryParse(parts[1], out float interval) || interval <= 0f) return null;
            if (!float.TryParse(parts[3], out float radius) || radius <= 0f) return null;
            return new PeriodicSpawnPassive
            {
                Interval = interval,
                ProjectilePrefabId = parts[2].Trim(),
                Radius = radius
            };
        }

        public override void Tick(float deltaTime)
        {
            if (Owner == null || Owner.IsDead) return;

            _timer += deltaTime;
            if (_timer < Interval) return;
            _timer = 0f;

            var prefab = LoadPrefab();
            if (prefab == null) return;

            if (Owner is RangeTrain range)
            {
                Monster nearest = FindNearestMonster(range.CurrentAttackRange);
                range.SpawnExternalProjectile(prefab, Radius, nearest);
            }
            else if (Owner is TurretTrain turret) turret.SpawnExternalProjectileAtSelf(prefab, Radius);
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
