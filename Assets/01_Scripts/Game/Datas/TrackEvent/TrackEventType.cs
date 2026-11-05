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
        SupplyDrop = 10,        // 보급 투하: 반짝이가 떠가고, 터치하면 골드
        GoldenMonster = 11,     // 황금 몬스터: 맵에서 제일 약한 몬스터를 금색으로 여러 마리 소환, 처치하면 큰 골드
        Overdrive = 12,         // 과부하 운행: 일정 시간 전 포탑 공격 속도 증가
        SupportBombard = 13,    // 지원 폭격: 화면 무작위 3~5곳 폭격, 몬스터만 피해
        // 14번은 폐지한 골드 러시 자리다. 씬에 정수로 저장되므로 다른 이벤트에 재사용하지 않는다.
        ExpBoost = 15,          // 경험치 순풍: 일정 시간 처치 경험치 배율
        Tailwind = 16,          // 순풍 운행: 남은 구간 시간 단축
        RepairStop = 17,        // 긴급 정비: 기차 체력 회복
        ColdSnap = 18,          // 한파: 일정 시간 몬스터 이동속도 감소
        FreeUpgrade = 19,       // 보너스 강화: 레벨업 3지선다 1회 즉시

        // 선택 — 리스크/리턴을 고르게 한다
        DetourChoice = 20,      // 우회로 선택: "위험한 지름길(구간 단축 + 몬스터↑) / 안전한 길" 중 선택

        // 날씨 전용 — TrackEventCategory.Weather 로 N역마다 확정 발생 (Sandstorm·ColdSnap도 약한 세기로 날씨 풀에 들어간다)
        Rain = 30,              // 비: 몬스터 이동속도↓ + 포탑 사거리 소폭↓
        Fog = 31,               // 안개: 포탑 사거리↓ + 처치 경험치↑
        Heatwave = 32,          // 폭염: 포탑 공격 속도↑ + 몬스터 이동속도↑

        // 선택지 — TrackEventCategory.Choice 로 2지선다 카드에 나온다. 효과는 작고 역 도착까지 유지
        ChoiceGoldBonus = 40,   // 처치 골드 소폭↑
        ChoiceExpBonus = 41,    // 처치 경험치 소폭↑
        ChoiceAttackSpeed = 42, // 포탑 공격 속도 소폭↑
        ChoiceRange = 43,       // 포탑 사거리 소폭↑
        ChoiceMonsterSlow = 44, // 몬스터 이동속도 소폭↓
        ChoiceRepair = 45,      // 기차 체력 소량 즉시 회복 (RepairStop 효과 재사용)
    }
}
