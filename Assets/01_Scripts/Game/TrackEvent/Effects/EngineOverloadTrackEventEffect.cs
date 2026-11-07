using TrainDefense.Game.Datas;
using TrainDefense.Game.Manager;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 엔진 과부하(위험 카드) — 험한 길(추가 소환 + 처치 골드·경험치 증가)에 남은 구간 시간 단축을 더한다.
    /// 짧은 시간에 몬스터가 몰려오는 대신 역에 빨리 도착한다.
    /// magnitude = 몬스터 등장 배율, subMagnitude = 처치 골드·경험치 배율 (험한 길과 같은 규칙).
    /// 시간 단축은 순풍 운행과 같은 StageManager.ShortenRemainingSegmentTime을 쓴다.
    /// </summary>
    public class EngineOverloadTrackEventEffect : ITrackEventEffect
    {
        public const float SHORTEN_RATIO = 0.3f;

        private readonly DetourTrackEventEffect _detourEffect = new DetourTrackEventEffect();

        public void Begin(TrackEventData data)
        {
            _detourEffect.Begin(data);

            var stageManager = StageManager.Instance;

            if (stageManager != null)
                stageManager.ShortenRemainingSegmentTime(SHORTEN_RATIO);
        }

        public void Tick(float deltaTime)
        {
            _detourEffect.Tick(deltaTime);
        }

        public void End(bool isInterrupted)
        {
            _detourEffect.End(isInterrupted);
        }
    }
}
