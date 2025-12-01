using System;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public struct TurretTrainStatus
    {
        public float AttackRange;
        public int AttackDamage;
        public int AttackCount;
        public float AttackInterval;
        public int TargetCount;
    }
}