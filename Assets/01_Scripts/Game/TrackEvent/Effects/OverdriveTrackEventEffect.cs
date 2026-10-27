using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 과부하 운행 — 지속 시간 동안 전 포탑 공격 속도 배율(기본 1.5).
    /// 배율은 TrackEventModifiers에만 걸고, 포탑이 전투 판정 시 곱한다. 시간 만료는 TrackEventManager가 End로 처리한다.
    /// </summary>
    public class OverdriveTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_MULTIPLIER = 1.5f;

        public void Begin(TrackEventData data)
        {
            float multiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MULTIPLIER;
            TrackEventModifiers.TurretAttackSpeedMultiplier = multiplier;
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.TurretAttackSpeedMultiplier = 1f;
        }
    }
}
