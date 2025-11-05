using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 원거리 기차 상태 정보 구조체
    /// </summary>
    [Serializable]
    public struct RangeAttackTrainStatus
    {
        public float AttackRange;
        public int AttackDamage;
        public int AttackCount;
        public float AttackInterval;
    }
}