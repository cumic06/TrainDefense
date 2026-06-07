using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 원거리 기차 상태 정보 구조체
    /// </summary>
    [Serializable]
    public struct RangeTrainStatus
    {
        public float AttackDamage;
        public float AttackRange;
        public float AttackArea;
        public int AttackCount;
        public float AttackInterval;
        public float CriticalChance;
        public float CriticalDamage;
        public float SlowRate;
    }
}