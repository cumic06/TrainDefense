using System.Collections.Generic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 날씨·선택 이벤트의 코드 기본값. TrackEventManager.eventDatas(인스펙터)에 해당 분류가 하나도 없을 때만 쓴다
    /// (EliteData가 variants가 비면 코드 기본 4종을 쓰는 것과 같은 방식). 인스펙터에 한 개라도 넣으면 그 목록이 우선한다.
    /// 날씨·선택은 지속 시간과 무관하게 역 도착까지 유지된다.
    /// magnitude = 첫 배율, subMagnitude = 두 번째 배율 (ModifierTrackEventEffect 규칙). 선택지 효과는 일부러 작게 잡았다.
    /// </summary>
    public static class TrackEventDefaults
    {
        private const string NAME_KEY_FORMAT = "TrackEvent_{0}_Name";

        public static IReadOnlyList<TrackEventData> WeatherDatas { get; } = new[]
        {
            // 몬스터 이속 -10% / 포탑 사거리 -5%
            _Create("weather_rain", TrackEventType.Rain, TrackEventCategory.Weather, 0.9f, 0.95f, null),
            // 포탑 사거리 -15% / 처치 경험치 +20%
            _Create("weather_fog", TrackEventType.Fog, TrackEventCategory.Weather, 0.85f, 1.2f, null),
            // 포탑 공속 +10% / 몬스터 이속 +10%
            _Create("weather_heatwave", TrackEventType.Heatwave, TrackEventCategory.Weather, 1.1f, 1.1f, null),
            // 포탑 사거리 -15% (무작위 모래폭풍 0.7보다 약하게 — 구간 내내 유지되므로)
            _Create("weather_sandstorm", TrackEventType.Sandstorm, TrackEventCategory.Weather, 0.85f, 1f, null),
            // 몬스터 이속 -15%
            _Create("weather_coldsnap", TrackEventType.ColdSnap, TrackEventCategory.Weather, 0.85f, 1f, null),
        };

        public static IReadOnlyList<TrackEventData> ChoiceDatas { get; } = new[]
        {
            _Create("choice_gold", TrackEventType.ChoiceGoldBonus, TrackEventCategory.Choice, 1.1f, 1f, "Upgrade/Upgrade_110004"),
            _Create("choice_exp", TrackEventType.ChoiceExpBonus, TrackEventCategory.Choice, 1.1f, 1f, null),
            _Create("choice_attack_speed", TrackEventType.ChoiceAttackSpeed, TrackEventCategory.Choice, 1.08f, 1f, "Upgrade/Upgrade_110002"),
            _Create("choice_range", TrackEventType.ChoiceRange, TrackEventCategory.Choice, 1.08f, 1f, null),
            _Create("choice_monster_slow", TrackEventType.ChoiceMonsterSlow, TrackEventCategory.Choice, 0.92f, 1f, null),
            // 기차·포탑 체력 최대치의 10% 즉시 회복
            _Create("choice_repair", TrackEventType.ChoiceRepair, TrackEventCategory.Choice, 0.1f, 1f, "Upgrade/Upgrade_110003"),
        };

        private static TrackEventData _Create(string id, TrackEventType type, TrackEventCategory category,
                                              float magnitude, float subMagnitude, string iconId)
        {
            return new TrackEventData(id, type, category, 1f, 0, magnitude, subMagnitude,
                                      string.Format(NAME_KEY_FORMAT, id), iconId);
        }
    }
}
