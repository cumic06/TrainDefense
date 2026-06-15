using System.Globalization;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 히트 시 타겟에 넉백 효과 적용. (엘리트 미사일: 약한 넉백)
    /// DSL: "KnockbackOnHit:power:duration"
    /// 합성 가능한 IProjectileModifier로 동작하여 관통/크기 등 다른 총알 능력과 함께 적용된다.
    /// </summary>
    public class KnockbackOnHitPassive : TrainPassiveSkill, IProjectileModifier
    {
        public float Power { get; private set; }
        public float Duration { get; private set; }

        public int Order => 100;

        public static KnockbackOnHitPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float power) || power <= 0f) return null;
            if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float duration) || duration <= 0f) return null;
            return new KnockbackOnHitPassive { Power = power, Duration = duration };
        }

        public override void Subscribe()
        {
            if (Owner is IProjectileEmitter e) e.AddProjectileModifier(this);
        }

        public override void Unsubscribe()
        {
            if (Owner is IProjectileEmitter e) e.RemoveProjectileModifier(this);
        }

        public Projectile OverridePrefab(ProjectileSpawnContext ctx, Projectile current) => null;

        public void Apply(ProjectileSpawnContext ctx, Projectile projectile)
        {
            if (projectile != null) projectile.SetRuntimeShove(Power, Duration);
        }
    }
}
