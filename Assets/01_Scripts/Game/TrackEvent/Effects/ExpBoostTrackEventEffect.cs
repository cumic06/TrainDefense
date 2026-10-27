using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 경험치 순풍 — 지속 동안 몬스터 처치 경험치 배율을 올린다. 실제 곱셈은 Monster._DropExp가 KillExpMultiplier를 읽어 수행한다.
    /// </summary>
    public class ExpBoostTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_MULTIPLIER = 1.5f;

        public void Begin(TrackEventData data)
        {
            TrackEventModifiers.KillExpMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MULTIPLIER;
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.KillExpMultiplier = 1f;
        }
    }
}
