using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 모래폭풍 — 지속 시간 동안 전 포탑 사거리 배율(기본 0.7).
    /// 배율은 TrackEventModifiers에만 걸고, 포탑이 전투 판정 시 곱한다. 시간 만료는 TrackEventManager가 End로 처리한다.
    /// 화면: 모래빛 틴트 + 옆으로 빠르게 날리는 모래 알갱이.
    /// </summary>
    public class SandstormTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_MULTIPLIER = 0.7f;

        private static readonly TrackEventWeatherEffect.WeatherStyle WEATHER_STYLE = new TrackEventWeatherEffect.WeatherStyle
        {
            TintColor = new Color(0.9f, 0.74f, 0.5f, 0.4f),
            ParticleColor = new Color(1f, 0.92f, 0.72f, 0.8f),
            ParticleCount = 160,
            Velocity = new Vector2(-14f, -1.5f),
            VelocityJitter = 0.35f,
            ParticleSize = new Vector2(1.4f, 0.4f),
            SwayAmplitude = 0.15f,
            SwayFrequency = 6f,
        };

        private TrackEventWeatherEffect _weatherEffect;

        public void Begin(TrackEventData data)
        {
            float multiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MULTIPLIER;
            TrackEventModifiers.TurretRangeMultiplier = multiplier;
            _weatherEffect = TrackEventWeatherEffect.Create(WEATHER_STYLE);
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.TurretRangeMultiplier = 1f;

            if (_weatherEffect != null)
                _weatherEffect.FadeOutAndDestroy();
        }
    }
}
