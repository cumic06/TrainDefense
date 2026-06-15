using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 모든 공격에 관통 투사체를 사용하는 패시브. (엘리트 저격: 관통 저격)
    /// DSL: "PierceProjectile:projectile_prefab_id"
    /// 관통 횟수(maxPenetration)는 투사체 ProjectileData SO에서 설정.
    /// 합성 가능한 IProjectileModifier로 동작한다(상시 교체이므로 주기적 특수탄보다 낮은 Order).
    /// </summary>
    public class PierceProjectilePassive : TrainPassiveSkill, IProjectileModifier
    {
        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        public string ProjectilePrefabId { get; private set; }

        public int Order => 5;

        private Projectile _cachedPrefab;
        private bool _loadAttempted;

        public static PierceProjectilePassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            return new PierceProjectilePassive { ProjectilePrefabId = parts[1].Trim() };
        }

        public override void Subscribe()
        {
            if (Owner is IProjectileEmitter e) e.AddProjectileModifier(this);
        }

        public override void Unsubscribe()
        {
            if (Owner is IProjectileEmitter e) e.RemoveProjectileModifier(this);
        }

        public Projectile OverridePrefab(ProjectileSpawnContext ctx, Projectile current) => LoadPrefab();

        public void Apply(ProjectileSpawnContext ctx, Projectile projectile) { }

        private Projectile LoadPrefab()
        {
            if (_cachedPrefab != null) return _cachedPrefab;
            if (_loadAttempted) return null;
            _loadAttempted = true;

            if (string.IsNullOrEmpty(ProjectilePrefabId)) return null;

            var go = Resources.Load<GameObject>($"{PROJECTILE_PREFAB_PATH}{ProjectilePrefabId}");
            if (go == null)
            {
                Debug.LogWarning($"PierceProjectilePassive: prefab '{PROJECTILE_PREFAB_PATH}{ProjectilePrefabId}' 을 찾을 수 없음");
                return null;
            }
            _cachedPrefab = go.GetComponent<Projectile>();
            return _cachedPrefab;
        }
    }
}
