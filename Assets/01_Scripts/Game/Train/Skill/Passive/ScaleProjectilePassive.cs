using System.Globalization;

namespace TrainDefense.Game
{
    /// <summary>
    /// owner가 발사하는 투사체의 모델 스케일 배율 변경. (엘리트 미사일: 미사일 크기 증가)
    /// DSL: "ScaleProjectile:scale"
    /// 예: "ScaleProjectile:2" → 미사일 크기 2배
    /// 합성 가능한 IProjectileModifier로 동작하며, 여러 스케일 능력은 곱연산으로 누적된다.
    /// </summary>
    public class ScaleProjectilePassive : TrainPassiveSkill, IProjectileModifier
    {
        public float Scale { get; private set; }

        // 비주얼/스탯 수식은 프리팹 선택 이후 적용되도록 높은 Order.
        public int Order => 100;

        public static ScaleProjectilePassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) || scale <= 0f) return null;
            return new ScaleProjectilePassive { Scale = scale };
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
            // 스폰 시 _scale=1로 리셋되므로 현재 값에 곱하면 여러 스케일 능력이 자연스럽게 누적된다.
            if (projectile != null) projectile.SetScale(projectile.Scale * Scale);
        }
    }
}
