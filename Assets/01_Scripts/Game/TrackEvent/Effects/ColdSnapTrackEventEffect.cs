using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 한파 — 지속 동안 몬스터 이동속도 배율을 낮춘다. 실제 곱셈은 Monster._Move가 MonsterMoveSpeedMultiplier를 읽어 수행한다.
    /// 화면: 푸른 틴트 + 천천히 흔들리며 내리는 눈송이.
    /// </summary>
    public class ColdSnapTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_MULTIPLIER = 0.7f;

        private static readonly TrackEventWeatherEffect.WeatherStyle WEATHER_STYLE = new TrackEventWeatherEffect.WeatherStyle
        {
            TintColor = new Color(0.55f, 0.78f, 1f, 0.22f),
            ParticleColor = new Color(1f, 1f, 1f, 0.9f),
            ParticleCount = 90,
            Velocity = new Vector2(-0.8f, -2.2f),
            VelocityJitter = 0.4f,
            ParticleSize = new Vector2(0.7f, 0.7f),
            SwayAmplitude = 0.35f,
            SwayFrequency = 1.6f,
        };

        private TrackEventWeatherEffect _weatherEffect;

        public void Begin(TrackEventData data)
        {
            TrackEventModifiers.MonsterMoveSpeedMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MULTIPLIER;
            _weatherEffect = TrackEventWeatherEffect.Create(WEATHER_STYLE);
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.MonsterMoveSpeedMultiplier = 1f;

            if (_weatherEffect != null)
                _weatherEffect.FadeOutAndDestroy();
        }
    }
}
