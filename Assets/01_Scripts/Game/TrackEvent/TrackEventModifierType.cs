namespace TrainDefense.Game.TrackEvent
{
    /// <summary>TrackEventModifiers의 배율 하나를 가리킨다. 여러 효과가 같은 배율을 곱해 쓰도록 공통 진입점(Multiply/Divide)에 쓴다.</summary>
    public enum TrackEventModifierType
    {
        KillGold = 0,
        KillExp = 1,
        MonsterMoveSpeed = 2,
        TurretAttackSpeed = 3,
        TurretRange = 4,
        TurretDamage = 5,
        MonsterAttack = 6,
        MonsterHealth = 7,
    }
}
