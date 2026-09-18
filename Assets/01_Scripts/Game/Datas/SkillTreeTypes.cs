namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 스킬트리 레인(계열). 트리 화면에서 세로 선로 한 줄 = 한 레인이다.
    /// </summary>
    public enum SkillTreeLane
    {
        Firepower,   // 0 — 화력
        Defense,     // 1 — 방어
        Utility,     // 2 — 유틸
    }

    /// <summary>
    /// 스킬 노드 분류. 포탑 스탯 강화와 패시브(게임 전반 상시 효과), 포탑 해금을 구분한다.
    /// </summary>
    public enum SkillNodeCategory
    {
        TurretStat,    // 포탑·레인지 스탯 강화 (SimpleStat[] → GetBonus(StatType))
        Passive,       // 게임 전반 상시 효과 (SkillTreePassiveType → GetValue(type))
        TurretUnlock,  // 신규 포탑 해금 — 습득 시 unlockTrainId 포탑이 삼중택일 Add 풀에 등장
    }

    /// <summary>
    /// 패시브 스킬 노드 효과 종류. 각 효과는 해당 시스템에서 GetValue로 조회해 적용한다.
    /// </summary>
    public enum SkillTreePassiveType
    {
        MaxTurretCount,    // 0 — 최대 포탑 수 증가 (+개수)
        MaxHp,             // 1 — 포탑 최대 체력 증가 (%)
        HealthRegen,       // 2 — 5초마다 체력 회복 (%)
        FreeReroll,        // 3 — 레벨업 시 무료 리롤 횟수
        DamageReduction,   // 4 — 포탑이 받는 피해 감소 (%)
        ExpGain,           // 5 — 획득 경험치 증가 (%)
    }
}
