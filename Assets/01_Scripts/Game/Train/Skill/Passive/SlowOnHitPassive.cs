
namespace TrainDefense.Game
{
    /// <summary>
    /// 공격에 닿은 적을 둔화. 닿아 있는 동안 계속 걸리고, 벗어나면 유지 시간 뒤 풀린다. (엘리트 화염 둔화: 30%, 0.5초)
    /// DSL: "SlowOnHit:rate:duration"
    /// </summary>
    public class SlowOnHitPassive : TrainPassiveSkill
    {
        public float Rate { get; private set; }
        public float Duration { get; private set; }

        public static SlowOnHitPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!TryParseFloat(parts[1], out float rate) || rate <= 0f || rate >= 100f) return null;
            if (!TryParseFloat(parts[2], out float duration) || duration <= 0f) return null;
            return new SlowOnHitPassive { Rate = rate, Duration = duration };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t)
            {
                // Rate는 %(30 = 30% 느려짐), 포탑·몬스터는 유지 속도 비율(0.7)을 쓴다.
                t.ProjectileSlowValue = 1f - Rate / 100f;
                t.ProjectileSlowDuration = Duration;
            }
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t)
            {
                t.ProjectileSlowValue = 1f;
                t.ProjectileSlowDuration = 0f;
            }
        }
    }
}
