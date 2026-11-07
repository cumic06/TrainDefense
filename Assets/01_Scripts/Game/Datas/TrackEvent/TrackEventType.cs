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
        MonsterRush = 1,        // 몬스터 러시: 일정 시간 제일 약한 몬스터를 평소 소환 위에 더 소환
        EliteAmbush = 2,        // 엘리트 매복: 엘리트 1마리 즉시 출현, 처치 시 엘리트 재화 보너스
        Sandstorm = 3,          // 모래폭풍: 포탑 사거리 감소

        // 기회 — 플레이어에게 이득
        SupplyDrop = 10,        // 보급 투하: 반짝이가 떠가고, 터치하면 골드
        GoldenMonster = 11,     // 황금 몬스터: 맵에서 제일 약한 몬스터를 금색으로 여러 마리 소환, 처치하면 큰 골드
        Overdrive = 12,         // 과부하 운행: 일정 시간 전 포탑 공격 속도 증가
        SupportBombard = 13,    // 지원 폭격: 일정 시간 동안 몇 초마다 몬스터 하나에 포탄 1발, 몬스터만 피해
        // 14번은 폐지한 골드 러시 자리다. 씬에 정수로 저장되므로 다른 이벤트에 재사용하지 않는다.
        ExpBoost = 15,          // 경험치 순풍: 일정 시간 처치 경험치 배율
        Tailwind = 16,          // 순풍 운행: 남은 구간 시간 단축
        RepairStop = 17,        // 긴급 정비: 기차 체력 회복
        ColdSnap = 18,          // 한파: 일정 시간 몬스터 이동속도 감소
        FreeUpgrade = 19,       // 보너스 강화: 레벨업 3지선다 1회 즉시

        // 위험/안전 선택 — TrackEventDefaults.RiskChoicePairs 로 짝지어 갈림길 팝업에 나온다 (EliteAmbush도 위험 카드로 쓴다)
        DetourChoice = 20,      // 험한 길: 역 도착까지 몬스터 추가 소환 + 처치 골드·경험치 증가 (위험/안전 선택의 위험 카드)
        SafePass = 21,          // 안전 카드: 효과 없음. 위험/안전 선택에서 위험 카드 옆에 나온다
        EngineOverload = 22,    // 엔진 과부하: 험한 길(추가 소환 + 처치 골드·경험치 증가) + 남은 구간 시간 단축 (위험/안전 선택의 위험 카드)

        // 날씨 전용 — TrackEventCategory.Weather 로 N역마다 확정 발생 (Sandstorm·ColdSnap도 약한 세기로 날씨 풀에 들어간다)
        Rain = 30,              // 비: 몬스터 이동속도↓ + 포탑 사거리 소폭↓
        Fog = 31,               // 안개: 포탑 사거리↓ + 처치 경험치↑
        Heatwave = 32,          // 폭염: 포탑 피해↑ + 몬스터 이동속도 소폭↑

        // 선택지 — 11-07 갈림길에서 뺐다(스탯만 주는 카드라 고민이 없음). 씬에 정수로 저장되므로 번호는 남긴다.
        ChoiceGoldBonus = 40,   // 처치 골드 소폭↑
        ChoiceExpBonus = 41,    // 처치 경험치 소폭↑
        ChoiceAttackSpeed = 42, // 포탑 공격 속도 소폭↑
        ChoiceRange = 43,       // 포탑 사거리 소폭↑ (11-06 선택 카드에서 뺌 — 포탑 피해로 교체)
        ChoiceMonsterSlow = 44, // 몬스터 이동속도 소폭↓ (11-06 선택 카드에서 뺌 — 날씨와 효과가 같음)
        ChoiceRepair = 45,      // 기차 체력 소량 즉시 회복 (RepairStop 효과 재사용)
        ChoiceTurretDamage = 46, // 포탑 피해 소폭↑

        // 날씨 갈림길 — 날씨 구간의 갈림길에 그 날씨 짝(TrackEventDefaults.WeatherChoicePairs)으로 나온다. 효과는 역 도착까지
        SandstormClearView = 50,    // 시야 확보: 포탑 사거리↑
        SandstormPushThrough = 51,  // 폭풍 돌파: 몬스터 이동속도↑ + 처치 골드↑
        RainCover = 52,             // 방수 덮개: 포탑 사거리↑ + 포탑 공격 속도↓
        RainCooling = 53,           // 빗물 냉각: 포탑 공격 속도↑ + 포탑 사거리↓
        FogMarch = 54,              // 안개 행군: 포탑 사거리↓ + 처치 경험치↑
        FogSearch = 55,             // 안개 속 수색: 포탑 사거리↓ + 처치 골드↑
        HeatwaveOverheat = 56,      // 과열 사격: 포탑 피해↑ + 몬스터 이동속도↑
        HeatwaveExhausted = 57,     // 더위 먹은 무리: 몬스터 공격력↑ + 몬스터 체력↓ (엘리트가 나오기 전 역)
        HeatwaveElites = 58,        // 달아오른 엘리트: 남은 구간 엘리트 추가 출현 + 포탑 공격 속도↑
        ColdSnapIceSlide = 59,      // 빙판 미끄럼: 몬스터 추가 소환 + 처치 골드↑
        ColdSnapBlizzardPush = 60,  // 눈보라 강행: 포탑 사거리↓ + 처치 골드↑
    }
}
