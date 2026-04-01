using System;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public struct TurretTrainStatus
    {
        public int AttackDamage;
        public float AttackRange;
        public int AttackCount;
        public float AttackInterval;
        public int TargetCount;
    }
}