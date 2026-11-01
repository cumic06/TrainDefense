using UnityEngine;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 날씨 전용 이벤트(비·안개·폭염)의 화면 연출 값. 모래폭풍·한파·순풍은 각자 효과 클래스에 들고 있다.
    /// 크기·속도는 TrackEventWeatherEffect 규칙대로 orthographicSize 10 기준 값이다.
    /// </summary>
    public static class TrackEventWeatherStyles
    {
        /// <summary>비 — 회청색 틴트 + 비스듬히 빠르게 떨어지는 가는 빗줄기.</summary>
        public static readonly TrackEventWeatherEffect.WeatherStyle RAIN = new TrackEventWeatherEffect.WeatherStyle
        {
            TintColor = new Color(0.35f, 0.42f, 0.55f, 0.25f),
            ParticleColor = new Color(0.78f, 0.86f, 1f, 0.6f),
            ParticleCount = 140,
            Velocity = new Vector2(-3f, -18f),
            VelocityJitter = 0.25f,
            ParticleSize = new Vector2(1.6f, 0.12f),
            SwayAmplitude = 0f,
            SwayFrequency = 0f,
        };

        /// <summary>안개 — 옅은 회백색 틴트 + 천천히 흘러가는 큰 안개 덩어리.</summary>
        public static readonly TrackEventWeatherEffect.WeatherStyle FOG = new TrackEventWeatherEffect.WeatherStyle
        {
            TintColor = new Color(0.85f, 0.88f, 0.9f, 0.3f),
            ParticleColor = new Color(1f, 1f, 1f, 0.22f),
            ParticleCount = 26,
            Velocity = new Vector2(-1.2f, 0f),
            VelocityJitter = 0.5f,
            ParticleSize = new Vector2(9f, 5f),
            SwayAmplitude = 0.2f,
            SwayFrequency = 0.5f,
        };

        /// <summary>폭염 — 주황 틴트 + 아지랑이처럼 흔들리며 올라가는 열기 입자.</summary>
        public static readonly TrackEventWeatherEffect.WeatherStyle HEATWAVE = new TrackEventWeatherEffect.WeatherStyle
        {
            TintColor = new Color(1f, 0.55f, 0.2f, 0.2f),
            ParticleColor = new Color(1f, 0.85f, 0.6f, 0.35f),
            ParticleCount = 60,
            Velocity = new Vector2(0f, 1.6f),
            VelocityJitter = 0.4f,
            ParticleSize = new Vector2(0.5f, 1.2f),
            SwayAmplitude = 0.4f,
            SwayFrequency = 2.2f,
        };
    }
}
