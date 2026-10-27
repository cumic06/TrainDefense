using TrainDefense.Game.Datas;
using TrainDefense.Game.Manager;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 순풍 운행 — 현재 구간의 남은 시간을 magnitude(비율) 만큼 줄여 역에 빨리 도착한다. 즉발형(권장 duration 0).
    /// 시간을 앞당길 뿐 되돌릴 상태가 없다.
    /// 화면: 기차 진행 방향(오른쪽)으로 지나가는 바람 줄기 — 즉발형이라 몇 초 뒤 스스로 사라진다(역 도착 시엔 즉시 페이드).
    /// </summary>
    public class TailwindTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_SHORTEN_RATIO = 0.2f;

        private static readonly TrackEventWeatherEffect.WeatherStyle WEATHER_STYLE = new TrackEventWeatherEffect.WeatherStyle
        {
            TintColor = Color.clear,
            ParticleColor = new Color(1f, 1f, 1f, 0.55f),
            ParticleCount = 45,
            Velocity = new Vector2(22f, 0f),
            VelocityJitter = 0.3f,
            ParticleSize = new Vector2(4f, 0.22f),
            SwayAmplitude = 0.1f,
            SwayFrequency = 3f,
            AutoFadeDelay = 3f,
        };

        private TrackEventWeatherEffect _weatherEffect;

        public void Begin(TrackEventData data)
        {
            _weatherEffect = TrackEventWeatherEffect.Create(WEATHER_STYLE);

            var stageManager = StageManager.Instance;

            if (stageManager == null)
                return;

            float ratio = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_SHORTEN_RATIO;

            stageManager.ShortenRemainingSegmentTime(ratio);
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            if (_weatherEffect != null)
                _weatherEffect.FadeOutAndDestroy();
        }
    }
}
