using System;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public struct TurretTrainStatus
    {
        public int AttackDamage;
        public float AttackRange;
        public float AttackArea;
        public int AttackCount;
        public float AttackInterval;
        public int TargetCount;
        public float CriticalChance;
        public float CriticalDamage;
    }
}