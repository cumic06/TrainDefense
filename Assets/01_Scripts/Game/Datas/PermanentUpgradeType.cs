namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 영구 업그레이드 분류. 포탑 스탯 강화와 패시브(게임 전반 상시 효과)를 구분한다.
    /// </summary>
    public enum PermanentUpgradeCategory
    {
        TurretStat,   // 포탑·레인지 스탯 강화 (SimpleStat[] → GetBonus(StatType))
        Passive,      // 게임 전반 상시 효과 (PermanentUpgradeType → GetValue(type))
    }

    /// <summary>
    /// 패시브 영구 업그레이드 효과 종류. 각 효과는 해당 시스템에서 GetValue로 조회해 적용한다.
    /// </summary>
    public enum PermanentUpgradeType
    {
        MaxTurretCount,    // 0 — 최대 포탑 수 증가 (+개수)
        MaxHp,             // 1 — 최대 체력 증가
        HealthRegen,       // 2 — 5초마다 체력 회복 (%)
        FreeReroll,        // 3 — 레벨업 시 무료 리롤 횟수
    }
}
