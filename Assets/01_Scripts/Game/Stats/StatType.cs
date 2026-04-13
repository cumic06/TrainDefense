using System;

namespace TrainDefense.Game.Stats
{
    [Serializable]
    public enum StatType
    {
        // 공통
        MaxHp,

        // TurretTrain / RangeTrain 공통
        AttackRange,
        AttackDamage,
        AttackCount,
        AttackInterval,
        TargetCount,

        // 치명타
        CriticalChance,
        CriticalDamage,
    }
}