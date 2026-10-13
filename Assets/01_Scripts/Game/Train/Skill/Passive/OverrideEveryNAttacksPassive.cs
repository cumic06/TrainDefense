using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 매 N공격마다 i번째 투사체를 다른 프리팹으로 교체. (엘리트 기관총: 3발마다 광역 투사체)
    /// DSL: "OverrideEveryN:N:projectile_prefab_id[:damage_mul]" (damage_mul 생략 시 1)
    /// </summary>
    public class OverrideEveryNAttacksPassive : TrainPassiveSkill
    {

        public int EveryN { get; private set; }
        public string ProjectilePrefabId { get; private set; }
        public float DamageMultiplier { get; private set; } = 1f;

        public static OverrideEveryNAttacksPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!int.TryParse(parts[1], out int n) || n <= 0) return null;
            float damageMultiplier = 1f;
            if (parts.Length >= 4 && TryParseFloat(parts[3], out float parsedMultiplier) && parsedMultiplier > 0f)
                damageMultiplier = parsedMultiplier;
            return new OverrideEveryNAttacksPassive
            {
                EveryN = n,
                ProjectilePrefabId = parts[2].Trim(),
                DamageMultiplier = damageMultiplier
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

        private Projectile Provide(int attackIndex, out float damageMultiplier)
        {
            damageMultiplier = DamageMultiplier;
            if (EveryN <= 0) return null;
            if (attackIndex <= 0 || attackIndex % EveryN != 0) return null;

            return LoadProjectilePrefab(ProjectilePrefabId);
        }

    }
}
