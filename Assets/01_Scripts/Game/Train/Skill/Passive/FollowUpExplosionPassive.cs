using System.Collections;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 직후 delay초 후 owner 위치에 투사체 발사. (엘리트 폭발: 0.2s 후 ExpandingWave)
    /// DSL: "FollowUpExplosion:delay:projectile_prefab_id[:radius]"
    /// radius 생략 시 owner의 AttackArea 사용 (음수로 전달).
    /// </summary>
    public class FollowUpExplosionPassive : TrainPassiveSkill
    {
        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        public float Delay { get; private set; } = 0.2f;
        public string ProjectilePrefabId { get; private set; }
        public float Radius { get; private set; } = -1f;

        private Projectile _cachedPrefab;
        private bool _loadAttempted;

        public static FollowUpExplosionPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!float.TryParse(parts[1], out float delay) || delay < 0f) return null;
            float radius = -1f;
            if (parts.Length >= 4) float.TryParse(parts[3], out radius);
            return new FollowUpExplosionPassive
            {
                Delay = delay,
                ProjectilePrefabId = parts[2].Trim(),
                Radius = radius
            };
        }

        public override void Subscribe()
        {
            switch (Owner)
            {
                case TurretTrain t: t.OnAttacked += HandleTurretAttacked; break;
                case RangeTrain r: r.OnAttacked += HandleRangeAttacked; break;
            }
        }

        public override void Unsubscribe()
        {
            switch (Owner)
            {
                case TurretTrain t: t.OnAttacked -= HandleTurretAttacked; break;
                case RangeTrain r: r.OnAttacked -= HandleRangeAttacked; break;
            }
        }

        private void HandleTurretAttacked(Monster _) => StartFollowUp();
        private void HandleRangeAttacked() => StartFollowUp();

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

            if (Owner is RangeTrain range) range.SpawnExternalProjectile(prefab, Radius);
            else if (Owner is TurretTrain turret) turret.SpawnExternalProjectileAtSelf(prefab, Radius);
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
