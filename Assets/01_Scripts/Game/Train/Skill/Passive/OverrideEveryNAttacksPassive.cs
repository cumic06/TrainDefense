using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 매 N공격마다 i번째 투사체를 다른 프리팹으로 교체. (엘리트 기관총: 5발마다 광역 투사체)
    /// DSL: "OverrideEveryN:N:projectile_prefab_id"
    /// </summary>
    public class OverrideEveryNAttacksPassive : TrainPassiveSkill
    {
        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        public int EveryN { get; private set; }
        public string ProjectilePrefabId { get; private set; }

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
            if (Owner is TurretTrain t) t.RegisterProjectileOverride(Provide);
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.UnregisterProjectileOverride(Provide);
        }

        private Projectile Provide(int attackIndex)
        {
            if (EveryN <= 0) return null;
            if (attackIndex <= 0 || attackIndex % EveryN != 0) return null;
            return LoadPrefab();
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
                Debug.LogWarning($"OverrideEveryNAttacksPassive: prefab not found at '{PROJECTILE_PREFAB_PATH}{ProjectilePrefabId}'");
                return null;
            }
            _cachedPrefab = go.GetComponent<Projectile>();
            return _cachedPrefab;
        }
    }
}
