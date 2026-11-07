using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>안전 카드 — 아무 효과가 없다. 위험/안전 선택에서 위험을 피하는 쪽이다.</summary>
    public class SafePassTrackEventEffect : ITrackEventEffect
    {
        public void Begin(TrackEventData data)
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
        }
    }
}
