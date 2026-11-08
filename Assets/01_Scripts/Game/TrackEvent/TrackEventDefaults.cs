using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 날씨·갈림길 이벤트의 코드 기본값. 날씨는 TrackEventManager.eventDatas(인스펙터)에 날씨가 하나도 없을 때만 쓴다
    /// (EliteData가 variants가 비면 코드 기본 4종을 쓰는 것과 같은 방식). 갈림길 짝은 여기에만 있다.
    /// 날씨·갈림길 효과는 지속 시간과 무관하게 역 도착까지 유지된다.
    /// magnitude = 첫 배율, subMagnitude = 두 번째 배율 (ModifierTrackEventEffect 규칙). 선택지 효과는 일부러 작게 잡았다.
    /// </summary>
    public static class TrackEventDefaults
    {
        private const string NAME_KEY_FORMAT = "TrackEvent_{0}_Name";
        // 위험 카드 엘리트 매복은 무작위 이벤트 때 쓰던 이름 문구를 그대로 쓴다.
        private const string ELITE_AMBUSH_NAME_KEY = "TrackEvent_EliteAmbush_Name";
        // 엘리트 매복 짝이 처음 뜨는 누적 상점 수(초반 엘리트 부담).
        private const int ELITE_AMBUSH_FIRST_STATION_COUNT = 8;
        // 폭염 엘리트 카드는 정규 엘리트가 처음 나오는 역부터 뜬다(그 전엔 SpawnEliteNow 적격 몬스터가 없어 카드가 빈다).
        private const int HEATWAVE_ELITE_FIRST_STATION_COUNT = MonsterSpawner.ELITE_START_STATION_COUNT;

        // 날씨는 무작위·선택 이벤트와 한 구간에 겹치므로 효과 하나당 5~10%로 약하게 둔다. 화면 문구는 숫자 없이 증가/감소만 보여 준다.
        public static IReadOnlyList<TrackEventData> WeatherDatas { get; } = new[]
        {
            // 몬스터 이속 -8% / 포탑 사거리 -5%
            _Create("weather_rain", TrackEventType.Rain, TrackEventCategory.Weather, 0.92f, 0.95f, null),
            // 포탑 사거리 -7% (이득은 날씨 갈림길 카드에서 고른다)
            _Create("weather_fog", TrackEventType.Fog, TrackEventCategory.Weather, 0.93f, 1f, null),
            // 포탑 피해 +8% / 몬스터 이속 +3% (같은 %라도 적 이속이 훨씬 크게 체감돼 적 쪽을 낮게 둔다)
            _Create("weather_heatwave", TrackEventType.Heatwave, TrackEventCategory.Weather, 1.08f, 1.03f, null),
            // 포탑 사거리 -8%
            _Create("weather_sandstorm", TrackEventType.Sandstorm, TrackEventCategory.Weather, 0.92f, 1f, null),
            // 몬스터 이속 -8%
            _Create("weather_coldsnap", TrackEventType.ColdSnap, TrackEventCategory.Weather, 0.92f, 1f, null),
        };

        /// <summary>
        /// 날씨 없는 갈림길 차례에 뜨는 위험/안전 짝. 위험 카드는 위험을 지는 대신 보상이 크고, 안전 카드는 아무 효과가 없다.
        /// 카드의 minStationCount 전에는 그 짝이 뽑히지 않는다.
        /// </summary>
        public static IReadOnlyList<ChoicePair> RiskChoicePairs { get; } = new[]
        {
            // 엘리트 1마리 즉시 출현, 역 도착 전에 잡으면 엘리트 코인 2배 / 피해 간다. 엘리트 부담이 커서 8역부터
            new ChoicePair(
                _Create("risk_elite_ambush", TrackEventType.EliteAmbush, TrackEventCategory.Choice, 2f, 1f, null, ELITE_AMBUSH_NAME_KEY, ELITE_AMBUSH_FIRST_STATION_COUNT),
                _Create("safe_elite_ambush", TrackEventType.SafePass, TrackEventCategory.Choice, 1f, 1f, null)),
            // 역 도착까지 몬스터 +15%, 처치 골드·경험치 +50% / 안전한 길
            new ChoicePair(
                _Create("risk_detour", TrackEventType.DetourChoice, TrackEventCategory.Choice, 1.15f, 1.5f, null),
                _Create("safe_detour", TrackEventType.SafePass, TrackEventCategory.Choice, 1f, 1f, null)),
            // 남은 구간 시간 -30%(EngineOverloadTrackEventEffect.SHORTEN_RATIO), 역 도착까지 몬스터 +25%, 처치 골드·경험치 ×1.8 / 정속 운행
            new ChoicePair(
                _Create("risk_engine_overload", TrackEventType.EngineOverload, TrackEventCategory.Choice, 1.25f, 1.8f, null),
                _Create("safe_engine_overload", TrackEventType.SafePass, TrackEventCategory.Choice, 1f, 1f, null)),
        };

        /// <summary>
        /// 날씨 구간의 갈림길에 뜨는 날씨별 짝. 짝마다 고르는 방식이 다르다(안전/위험·맞바꾸기·보상 고르기·세기 고르기·위험 고르기).
        /// 같은 날씨에 짝이 여럿이면 카드 minStationCount를 모두 넘긴 짝 중 가장 늦은 짝이 뜬다(폭염: 엘리트가 나오는 역부터 엘리트 카드).
        /// 효과는 날씨 위에 곱해진다. 적 둔화 카드와 처치 골드·경험치를 깎는 카드는 넣지 않는다.
        /// </summary>
        // 폭염 두 짝(엘리트 전/후)이 같이 쓰는 카드 A. 값을 한 곳에서만 고치도록 한 인스턴스로 둔다. WeatherChoicePairs보다 먼저 초기화돼야 한다.
        private static readonly TrackEventData _heatwaveOverheat =
            _Create("weather_heatwave_overheat", TrackEventType.HeatwaveOverheat, TrackEventCategory.Choice, 1.08f, 1.05f, null);

        public static IReadOnlyList<ChoicePair> WeatherChoicePairs { get; } = new[]
        {
            // 모래폭풍(안전/위험): 포탑 사거리 +5% / 몬스터 이속 +7%·처치 골드 +25%
            new ChoicePair(
                _Create("weather_sandstorm_clear_view", TrackEventType.SandstormClearView, TrackEventCategory.Choice, 1.05f, 1f, null),
                _Create("weather_sandstorm_push_through", TrackEventType.SandstormPushThrough, TrackEventCategory.Choice, 1.07f, 1.25f, null),
                TrackEventType.Sandstorm),
            // 비(맞바꾸기): 포탑 사거리 +5%·공속 -5% / 포탑 공속 +8%·사거리 -5%
            new ChoicePair(
                _Create("weather_rain_cover", TrackEventType.RainCover, TrackEventCategory.Choice, 1.05f, 0.95f, null),
                _Create("weather_rain_cooling", TrackEventType.RainCooling, TrackEventCategory.Choice, 1.08f, 0.95f, null),
                TrackEventType.Rain),
            // 안개(보상 고르기): 둘 다 포탑 사거리 -5%, 처치 경험치 +25% / 처치 골드 +20%
            new ChoicePair(
                _Create("weather_fog_march", TrackEventType.FogMarch, TrackEventCategory.Choice, 0.95f, 1.25f, null),
                _Create("weather_fog_search", TrackEventType.FogSearch, TrackEventCategory.Choice, 0.95f, 1.2f, null),
                TrackEventType.Fog),
            // 폭염(엘리트 전): 포탑 피해 +8%·몬스터 이속 +5% / 몬스터 공격력 +20%·체력 -8%
            new ChoicePair(
                _heatwaveOverheat,
                _Create("weather_heatwave_exhausted", TrackEventType.HeatwaveExhausted, TrackEventCategory.Choice, 1.2f, 0.92f, null),
                TrackEventType.Heatwave),
            // 폭염(엘리트가 나오는 역부터): 과열 사격 / 남은 구간 엘리트 2배·포탑 공속 +8%
            new ChoicePair(
                _heatwaveOverheat,
                _Create("weather_heatwave_elites", TrackEventType.HeatwaveElites, TrackEventCategory.Choice, 2f, 1.08f, null, null, HEATWAVE_ELITE_FIRST_STATION_COUNT),
                TrackEventType.Heatwave),
            // 한파(위험 고르기): 둘 다 처치 골드 +20%, 몬스터 +10% / 포탑 사거리 -8%
            new ChoicePair(
                _Create("weather_coldsnap_ice_slide", TrackEventType.ColdSnapIceSlide, TrackEventCategory.Choice, 1.1f, 1.2f, null),
                _Create("weather_coldsnap_blizzard_push", TrackEventType.ColdSnapBlizzardPush, TrackEventCategory.Choice, 0.92f, 1.2f, null),
                TrackEventType.ColdSnap),
        };

        private static TrackEventData _Create(string id, TrackEventType type, TrackEventCategory category,
                                              float magnitude, float subMagnitude, string iconId, string nameKey = null,
                                              int minStationCount = 0)
        {
            return new TrackEventData(id, type, category, 1f, minStationCount, magnitude, subMagnitude,
                                      nameKey ?? string.Format(NAME_KEY_FORMAT, id), iconId);
        }
    }

    /// <summary>갈림길 한 짝. 팝업에 First, Second 순서로 나온다. WeatherType이 None이 아니면 그 날씨 구간 전용이다.</summary>
    public readonly struct ChoicePair
    {
        public TrackEventData First { get; }
        public TrackEventData Second { get; }
        public TrackEventType WeatherType { get; }

        public ChoicePair(TrackEventData first, TrackEventData second, TrackEventType weatherType = TrackEventType.None)
        {
            First = first;
            Second = second;
            WeatherType = weatherType;
        }

        /// <summary>두 카드의 등장 역 조건 중 늦은 쪽. 이 상점 수 전에는 이 짝이 뜨지 않는다.</summary>
        public int MinStationCount => Mathf.Max(First.MinStationCount, Second.MinStationCount);
    }
}
