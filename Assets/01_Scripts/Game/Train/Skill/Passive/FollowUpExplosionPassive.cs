using System.Collections;
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

        public float Delay { get; private set; } = 0.2f;
        public string ProjectilePrefabId { get; private set; }
        public float Radius { get; private set; } = -1f;
        public float DamageMul { get; private set; } = 1f;
        public float ShoveScale { get; private set; } = 1f;

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

            var prefab = LoadProjectilePrefab(ProjectilePrefabId);
            if (prefab == null) yield break;

            if (Owner is RangeTrain range) range.SpawnExternalProjectile(prefab, Radius, null, DamageMul, ShoveScale);
            else if (Owner is TurretTrain turret) turret.SpawnExternalProjectileAtSelf(prefab, Radius, DamageMul, ShoveScale);
        }

    }
}
