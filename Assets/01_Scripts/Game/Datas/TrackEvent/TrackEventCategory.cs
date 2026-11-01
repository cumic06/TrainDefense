namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 선로 이벤트가 어떤 방식으로 나오는지. TrackEventManager가 분류별로 따로 추첨한다.
    /// </summary>
    public enum TrackEventCategory
    {
        Random = 0,     // 무작위 — 구간마다 확률로 터진다 (기존 13종)
        Weather = 1,    // 날씨 — weatherStationInterval 역마다 출발과 동시에 확정. 고를 수 없고 역 도착까지 유지
        Choice = 2,     // 선택 — choiceStationInterval 역마다 구간 도중 2지선다로 하나를 고른다. 효과는 작게, 역 도착까지 유지
    }
}
