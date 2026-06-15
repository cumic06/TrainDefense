using System.Collections;
using System.Globalization;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 직후 delay초 후 owner 위치에 투사체 발사. (엘리트 폭발: 0.2s 후 ExpandingWave)
    /// DSL: "FollowUpExplosion:delay:projectile_prefab_id[:radius[:damage_mul[:shove_scale]]]"
    /// radius 생략 시 owner의 AttackArea 사용 (음수로 전달).
    /// damage_mul/shove_scale 생략 시 1.0 (메인 공격과 동일).
    /// </summary>
    public class FollowUpExplosionPassive : TrainPassiveSkill
    {
        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        public float Delay { get; private set; } = 0.2f;
        public string ProjectilePrefabId { get; private set; }
        public float Radius { get; private set; } = -1f;
        public float DamageMul { get; private set; } = 1f;
        public float ShoveScale { get; private set; } = 1f;

        private Projectile _cachedPrefab;
        private bool _loadAttempted;

        public static FollowUpExplosionPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!TryParseFloat(parts[1], out float delay) || delay < 0f) return null;
            float radius = -1f;
            if (parts.Length >= 4 && !TryParseFloat(parts[3], out radius)) radius = -1f;
            float damageMul = 1f;
            if (parts.Length >= 5 && !TryParseFloat(parts[4], out damageMul)) damageMul = 1f;
            float shoveScale = 1f;
            if (parts.Length >= 6 && !TryParseFloat(parts[5], out shoveScale)) shoveScale = 1f;
            return new FollowUpExplosionPassive
            {
                Delay = delay,
                ProjectilePrefabId = parts[2].Trim(),
                Radius = radius,
                DamageMul = damageMul,
                ShoveScale = shoveScale
            };
        }

        private static bool TryParseFloat(string s, out float value)
            => float.TryParse(s?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        public override void Subscribe()
        {
            if (Owner is IAttackEvents e) e.OnAttacked += HandleAttacked;
        }

        public override void Unsubscribe()
        {
            if (Owner is IAttackEvents e) e.OnAttacked -= HandleAttacked;
        }

        private void HandleAttacked(Monster _) => StartFollowUp();

        private void StartFollowUp()
        {
            if (Owner == null) return;
            Owner.StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(Delay);
            if (Owner == null || Owner.IsDead) yield break;

            var prefab = LoadPrefab();
            if (prefab == null) yield break;

            if (Owner is IExternalProjectileSpawner spawner)
                spawner.SpawnExternalProjectile(prefab, Radius, null, DamageMul, ShoveScale);
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
                Debug.LogWarning($"FollowUpExplosionPassive: prefab not found at '{PROJECTILE_PREFAB_PATH}{ProjectilePrefabId}'");
                return null;
            }
            _cachedPrefab = go.GetComponent<Projectile>();
            return _cachedPrefab;
        }
    }
}
