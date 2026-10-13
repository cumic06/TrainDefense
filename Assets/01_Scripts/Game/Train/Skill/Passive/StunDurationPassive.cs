using System.Globalization;

namespace TrainDefense.Game
{
    /// <summary>
    /// 즉시 피해 기절 시간을 늘린다. (엘리트 전기 고압 전류: 0.15 → 0.5초)
    /// DSL: "StunDuration:seconds"
    /// </summary>
    public class StunDurationPassive : TrainPassiveSkill
    {
        public float Duration { get; private set; }

        public static StunDurationPassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float duration) || duration <= 0f) return null;
            return new StunDurationPassive { Duration = duration };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t) t.MinimumStunDuration = Duration;
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.MinimumStunDuration = 0f;
        }
    }
}
