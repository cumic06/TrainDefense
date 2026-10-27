using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 골드 러시 — 지속 동안 몬스터 처치 골드 배율을 올린다. 실제 곱셈은 Monster._DropMoney가 KillGoldMultiplier를 읽어 수행한다.
    /// </summary>
    public class GoldRushTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_MULTIPLIER = 2f;

        public void Begin(TrackEventData data)
        {
            TrackEventModifiers.KillGoldMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MULTIPLIER;
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.KillGoldMultiplier = 1f;
        }
    }
}
