using UnityEngine;
using ParticleLayer = TrainDefense.Game.TrackEvent.TrackEventWeatherEffect.ParticleLayer;
using ParticleShape = TrainDefense.Game.TrackEvent.TrackEventWeatherEffect.ParticleShape;
using WeatherStyle = TrainDefense.Game.TrackEvent.TrackEventWeatherEffect.WeatherStyle;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 자연환경 선로 이벤트 6종(비·안개·폭염·모래폭풍·한파·순풍)의 파티클 연출 값을 한 곳에 모은다.
    /// 크기·속도는 TrackEventWeatherEffect 규칙대로 orthographicSize 10 기준 값이다.
    /// 레이어는 뒤(작고 흐림, sortingOrder 낮음) → 앞(크고 진함) 순서로 깊이감을 만든다.
    /// 픽셀 아트 톤에 맞춰 알갱이는 PixelSquare/Snowflake(점 필터), 덩어리만 Cloud(부드러움)를 쓴다.
    /// </summary>
    public static class TrackEventWeatherStyles
    {
        private const int BACK_SORTING_ORDER = -60;
        private const int MIDDLE_SORTING_ORDER = -55;
        private const int FRONT_SORTING_ORDER = -50;

        /// <summary>비 — 회청색 틴트 + 앞뒤 두 겹의 비스듬한 빗줄기 + 가끔 번개.</summary>
        public static readonly WeatherStyle RAIN = new WeatherStyle
        {
            TintColor = new Color(0.3f, 0.38f, 0.52f, 0.28f),
            FlashInterval = 8f,
            Layers = new[]
            {
                new ParticleLayer
                {
                    Shape = ParticleShape.SoftDot,
                    ColorMin = new Color(0.7f, 0.8f, 0.95f, 0.3f),
                    ColorMax = new Color(0.8f, 0.88f, 1f, 0.45f),
                    MaxParticles = 90,
                    Lifetime = 1.4f,
                    Velocity = new Vector2(-3.5f, -15f),
                    VelocityJitter = 0.2f,
                    SizeRange = new Vector2(0.06f, 0.09f),
                    IsStretched = true,
                    StretchLength = 0.07f,
                    FadeEdge = 0.1f,
                    SortingOrder = BACK_SORTING_ORDER,
                },
                new ParticleLayer
                {
                    Shape = ParticleShape.SoftDot,
                    ColorMin = new Color(0.82f, 0.9f, 1f, 0.6f),
                    ColorMax = new Color(0.92f, 0.96f, 1f, 0.8f),
                    MaxParticles = 70,
                    Lifetime = 1f,
                    Velocity = new Vector2(-4.5f, -24f),
                    VelocityJitter = 0.15f,
                    SizeRange = new Vector2(0.1f, 0.14f),
                    IsStretched = true,
                    StretchLength = 0.08f,
                    FadeEdge = 0.1f,
                    SortingOrder = FRONT_SORTING_ORDER,
                },
            },
        };

        /// <summary>안개 — 회백색 틴트 + 천천히 흘러가는 큰 안개 덩어리 + 앞쪽의 빠른 옅은 자락.</summary>
        public static readonly WeatherStyle FOG = new WeatherStyle
        {
            TintColor = new Color(0.82f, 0.86f, 0.9f, 0.28f),
            Layers = new[]
            {
                new ParticleLayer
                {
                    Shape = ParticleShape.Cloud,
                    ColorMin = new Color(0.92f, 0.94f, 0.96f, 0.18f),
                    ColorMax = new Color(1f, 1f, 1f, 0.28f),
                    MaxParticles = 30,
                    Lifetime = 9f,
                    Velocity = new Vector2(-1.2f, 0f),
                    VelocityJitter = 0.5f,
                    SizeRange = new Vector2(6f, 11f),
                    NoiseStrength = 0.3f,
                    NoiseFrequency = 0.2f,
                    FadeEdge = 0.3f,
                    SortingOrder = BACK_SORTING_ORDER,
                },
                new ParticleLayer
                {
                    Shape = ParticleShape.Cloud,
                    ColorMin = new Color(1f, 1f, 1f, 0.1f),
                    ColorMax = new Color(1f, 1f, 1f, 0.18f),
                    MaxParticles = 14,
                    Lifetime = 6f,
                    Velocity = new Vector2(-2.2f, 0.1f),
                    VelocityJitter = 0.4f,
                    SizeRange = new Vector2(3f, 5f),
                    NoiseStrength = 0.4f,
                    NoiseFrequency = 0.3f,
                    FadeEdge = 0.3f,
                    SortingOrder = FRONT_SORTING_ORDER,
                },
            },
        };

        /// <summary>폭염 — 주황 틴트 + 일렁이며 올라가는 아지랑이 + 떠오르는 픽셀 불티.</summary>
        public static readonly WeatherStyle HEATWAVE = new WeatherStyle
        {
            TintColor = new Color(1f, 0.55f, 0.2f, 0.18f),
            Layers = new[]
            {
                new ParticleLayer
                {
                    Shape = ParticleShape.SoftDot,
                    ColorMin = new Color(1f, 0.88f, 0.65f, 0.12f),
                    ColorMax = new Color(1f, 0.95f, 0.8f, 0.22f),
                    MaxParticles = 40,
                    Lifetime = 2.5f,
                    Velocity = new Vector2(0f, 2f),
                    VelocityJitter = 0.4f,
                    SizeRange = new Vector2(0.25f, 0.4f),
                    IsStretched = true,
                    StretchLength = 0.6f,
                    NoiseStrength = 0.6f,
                    NoiseFrequency = 1.5f,
                    FadeEdge = 0.3f,
                    SortingOrder = BACK_SORTING_ORDER,
                },
                new ParticleLayer
                {
                    Shape = ParticleShape.PixelSquare,
                    ColorMin = new Color(1f, 0.45f, 0.15f, 0.8f),
                    ColorMax = new Color(1f, 0.78f, 0.32f, 0.95f),
                    MaxParticles = 30,
                    Lifetime = 3f,
                    Velocity = new Vector2(-0.6f, 1.4f),
                    VelocityJitter = 0.5f,
                    SizeRange = new Vector2(0.12f, 0.2f),
                    NoiseStrength = 0.8f,
                    NoiseFrequency = 1.2f,
                    FadeEdge = 0.2f,
                    SortingOrder = FRONT_SORTING_ORDER,
                },
            },
        };

        /// <summary>모래폭풍 — 모래빛 틴트 + 휩쓸려 가는 먼지 구름 + 모래 바람 줄기 + 픽셀 모래 알갱이.</summary>
        public static readonly WeatherStyle SANDSTORM = new WeatherStyle
        {
            TintColor = new Color(0.9f, 0.72f, 0.48f, 0.38f),
            Layers = new[]
            {
                new ParticleLayer
                {
                    Shape = ParticleShape.Cloud,
                    ColorMin = new Color(0.88f, 0.7f, 0.48f, 0.2f),
                    ColorMax = new Color(0.95f, 0.82f, 0.6f, 0.3f),
                    MaxParticles = 24,
                    Lifetime = 2.5f,
                    Velocity = new Vector2(-10f, -1f),
                    VelocityJitter = 0.3f,
                    SizeRange = new Vector2(5f, 9f),
                    NoiseStrength = 0.5f,
                    NoiseFrequency = 0.4f,
                    FadeEdge = 0.25f,
                    SortingOrder = BACK_SORTING_ORDER,
                },
                new ParticleLayer
                {
                    Shape = ParticleShape.SoftDot,
                    ColorMin = new Color(1f, 0.9f, 0.72f, 0.25f),
                    ColorMax = new Color(1f, 0.95f, 0.82f, 0.4f),
                    MaxParticles = 30,
                    Lifetime = 1.2f,
                    Velocity = new Vector2(-20f, -1.5f),
                    VelocityJitter = 0.25f,
                    SizeRange = new Vector2(0.08f, 0.12f),
                    IsStretched = true,
                    StretchLength = 0.08f,
                    FadeEdge = 0.15f,
                    SortingOrder = MIDDLE_SORTING_ORDER,
                },
                new ParticleLayer
                {
                    Shape = ParticleShape.PixelSquare,
                    ColorMin = new Color(0.82f, 0.62f, 0.38f, 0.85f),
                    ColorMax = new Color(1f, 0.9f, 0.68f, 0.9f),
                    MaxParticles = 160,
                    Lifetime = 1.6f,
                    Velocity = new Vector2(-16f, -1.5f),
                    VelocityJitter = 0.35f,
                    SizeRange = new Vector2(0.1f, 0.18f),
                    NoiseStrength = 1.2f,
                    NoiseFrequency = 2f,
                    FadeEdge = 0.1f,
                    SortingOrder = FRONT_SORTING_ORDER,
                },
            },
        };

        /// <summary>한파 — 푸른 틴트 + 뒤쪽 잔눈 + 앞쪽에서 빙글 돌며 흔들려 내리는 픽셀 눈송이.</summary>
        public static readonly WeatherStyle COLD_SNAP = new WeatherStyle
        {
            TintColor = new Color(0.55f, 0.78f, 1f, 0.22f),
            Layers = new[]
            {
                new ParticleLayer
                {
                    Shape = ParticleShape.PixelSquare,
                    ColorMin = new Color(0.85f, 0.93f, 1f, 0.45f),
                    ColorMax = new Color(1f, 1f, 1f, 0.65f),
                    MaxParticles = 70,
                    Lifetime = 7f,
                    Velocity = new Vector2(-0.6f, -1.6f),
                    VelocityJitter = 0.4f,
                    SizeRange = new Vector2(0.1f, 0.16f),
                    NoiseStrength = 0.5f,
                    NoiseFrequency = 0.6f,
                    FadeEdge = 0.15f,
                    SortingOrder = BACK_SORTING_ORDER,
                },
                new ParticleLayer
                {
                    Shape = ParticleShape.Snowflake,
                    ColorMin = new Color(0.9f, 0.96f, 1f, 0.85f),
                    ColorMax = new Color(1f, 1f, 1f, 1f),
                    MaxParticles = 45,
                    Lifetime = 6f,
                    Velocity = new Vector2(-1f, -2.6f),
                    VelocityJitter = 0.35f,
                    SizeRange = new Vector2(0.35f, 0.55f),
                    NoiseStrength = 0.7f,
                    NoiseFrequency = 0.5f,
                    RotationSpeed = 60f,
                    FadeEdge = 0.15f,
                    SortingOrder = FRONT_SORTING_ORDER,
                },
            },
        };

        /// <summary>순풍 — 기차 진행 방향(오른쪽)으로 지나가는 바람 줄기 + 휩쓸리는 먼지 점. 즉발형이라 3초 뒤 스스로 사라진다.</summary>
        public static readonly WeatherStyle TAILWIND = new WeatherStyle
        {
            TintColor = Color.clear,
            AutoFadeDelay = 3f,
            Layers = new[]
            {
                new ParticleLayer
                {
                    Shape = ParticleShape.PixelSquare,
                    ColorMin = new Color(0.9f, 0.96f, 0.88f, 0.5f),
                    ColorMax = new Color(1f, 1f, 1f, 0.75f),
                    MaxParticles = 25,
                    Lifetime = 1.2f,
                    Velocity = new Vector2(16f, 0.5f),
                    VelocityJitter = 0.4f,
                    SizeRange = new Vector2(0.1f, 0.15f),
                    NoiseStrength = 1f,
                    NoiseFrequency = 1f,
                    FadeEdge = 0.15f,
                    SortingOrder = MIDDLE_SORTING_ORDER,
                },
                new ParticleLayer
                {
                    Shape = ParticleShape.SoftDot,
                    ColorMin = new Color(1f, 1f, 1f, 0.4f),
                    ColorMax = new Color(1f, 1f, 1f, 0.65f),
                    MaxParticles = 40,
                    Lifetime = 0.9f,
                    Velocity = new Vector2(24f, 0f),
                    VelocityJitter = 0.3f,
                    SizeRange = new Vector2(0.12f, 0.2f),
                    IsStretched = true,
                    StretchLength = 0.12f,
                    NoiseStrength = 0.3f,
                    NoiseFrequency = 0.8f,
                    FadeEdge = 0.15f,
                    SortingOrder = FRONT_SORTING_ORDER,
                },
            },
        };
    }
}
