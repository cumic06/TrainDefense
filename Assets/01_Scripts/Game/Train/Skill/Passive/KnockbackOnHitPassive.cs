using System.Globalization;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 히트 시 타겟에 넉백 효과 적용. (엘리트 미사일: 약한 넉백)
    /// DSL: "KnockbackOnHit:power:duration"
    /// </summary>
    public class KnockbackOnHitPassive : TrainPassiveSkill
    {
        public float Power { get; private set; }
        public float Duration { get; private set; }

        public static KnockbackOnHitPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float power) || power <= 0f) return null;
            if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float duration) || duration <= 0f) return null;
            return new KnockbackOnHitPassive { Power = power, Duration = duration };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t)
            {
                t.ProjectileKnockbackPower = Power;
                t.ProjectileKnockbackDuration = Duration;
            }
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t)
            {
                t.ProjectileKnockbackPower = 0f;
                t.ProjectileKnockbackDuration = 0f;
            }
        }
    }
}
