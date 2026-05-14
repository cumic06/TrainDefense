using System.Globalization;

namespace TrainDefense.Game
{
    /// <summary>
    /// owner가 발사하는 투사체의 모델 스케일 배율 변경. (엘리트 미사일: 미사일 크기 증가)
    /// DSL: "ScaleProjectile:scale"
    /// 예: "ScaleProjectile:2" → 미사일 크기 2배
    /// </summary>
    public class ScaleProjectilePassive : TrainPassiveSkill
    {
        public float Scale { get; private set; }

        public static ScaleProjectilePassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) || scale <= 0f) return null;
            return new ScaleProjectilePassive { Scale = scale };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t) t.ProjectileModelScale = Scale;
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.ProjectileModelScale = 1f;
        }
    }
}
