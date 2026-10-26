namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 전투 구간(역과 역 사이) 도중 무작위로 터지는 선로 이벤트 종류.
    /// 효과 구현은 TrackEventEffectFactory가 타입별로 만든다. 구현이 없는 타입은 뽑혀도 건너뛴다.
    /// 기획 상세: plans/선로이벤트_구현계획.md
    /// </summary>
    public enum TrackEventType
    {
        None = 0,

        // 위기 — 전투 압박을 올린다
        MonsterRush = 1,        // 몬스터 러시: 스폰 간격 감소 (기존 MonsterRushEvent 재사용, 구현됨)
        EliteAmbush = 2,        // 엘리트 매복: 엘리트 1마리 즉시 출현, 처치 시 엘리트 재화 보너스
        Sandstorm = 3,          // 모래폭풍: 포탑 사거리 감소

        // 기회 — 플레이어에게 이득
        SupplyDrop = 10,        // 보급 상자: 화면에 상자 등장, 터치하면 골드
        GoldenMonster = 11,     // 황금 몬스터: 도망가는 몬스터, 놓치기 전에 잡으면 큰 골드
        Overdrive = 12,         // 과부하 운행: 일정 시간 전 포탑 공격 속도 증가

        // 선택 — 리스크/리턴을 고르게 한다
        DetourChoice = 20,      // 우회로 선택: "위험한 지름길(구간 단축 + 몬스터↑) / 안전한 길" 중 선택
    }
}
