using System;

namespace TrainDefense.Game.Stats
{
    [Serializable]
    public enum StatType
    {
        // 공통
        MaxHp = 0,

        // TurretTrain / RangeTrain 공통
        AttackRange = 1,
        AttackDamage = 2,
        AttackCount = 3,
        AttackInterval = 4,
        TargetCount = 5,

        // 치명타
        CriticalChance = 6,
        CriticalDamage = 7,

        // 공격 범위 (855f466에서 중간 삽입되어 기존 값 밀림 방지 — 맨 뒤 고정)
        AttackArea = 8,

        // 둔화율 (RangeTrain 전용, 맨 뒤 고정)
        SlowRate = 9,

        // 분사 지속시간 (버스트 포탑 전용, 맨 뒤 고정)
        BurstDuration = 10,
    }
}