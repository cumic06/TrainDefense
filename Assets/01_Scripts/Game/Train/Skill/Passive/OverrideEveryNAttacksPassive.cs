using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 매 N공격마다 투사체를 다른 프리팹으로 교체. (엘리트 기관총: 5발마다 광역 투사체)
    /// DSL: "OverrideEveryN:N:projectile_prefab_id"
    /// 합성 가능한 IProjectileModifier로 동작하므로 관통/크기 등 다른 총알 능력과 함께 적용된다.
    /// </summary>
    public class OverrideEveryNAttacksPassive : TrainPassiveSkill, IProjectileModifier
    {
        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        public int EveryN { get; private set; }
        public string ProjectilePrefabId { get; private set; }

        // 주기적 특수탄이 상시 교체(관통 등)보다 우선하도록 비교적 높은 Order.
        public int Order => 10;

        private Projectile _cachedPrefab;
        private bool _loadAttempted;

        public static OverrideEveryNAttacksPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!int.TryParse(parts[1], out int n) || n <= 0) return null;
            return new OverrideEveryNAttacksPassive
            {
                EveryN = n,
                ProjectilePrefabId = parts[2].Trim()
            };
        }

        public override void Subscribe()
        {
            if (Owner is IProjectileEmitter e) e.AddProjectileModifier(this);
        }

        public override void Unsubscribe()
        {
            if (Owner is IProjectileEmitter e) e.RemoveProjectileModifier(this);
        }

        public Projectile OverridePrefab(ProjectileSpawnContext ctx, Projectile current)
        {
            if (EveryN <= 0) return null;
            if (ctx.AttackIndex <= 0 || ctx.AttackIndex % EveryN != 0) return null;
            return LoadPrefab();
        }

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
                Debug.LogWarning($"OverrideEveryNAttacksPassive: prefab not found at '{PROJECTILE_PREFAB_PATH}{ProjectilePrefabId}'");
                return null;
            }
            _cachedPrefab = go.GetComponent<Projectile>();
            return _cachedPrefab;
        }
    }
}
