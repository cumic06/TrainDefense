using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 경험치 순풍 — 지속 동안 몬스터 처치 경험치 배율을 올린다. 실제 곱셈은 Monster._DropExp가 KillExpMultiplier를 읽어 수행한다.
    /// </summary>
    public class ExpBoostTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_MULTIPLIER = 1.5f;

        private float _appliedMultiplier = 1f;

        public void Begin(TrackEventData data)
        {
            _appliedMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MULTIPLIER;
            TrackEventModifiers.Multiply(TrackEventModifierType.KillExp, _appliedMultiplier);
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.Divide(TrackEventModifierType.KillExp, _appliedMultiplier);
        }
    }
}
