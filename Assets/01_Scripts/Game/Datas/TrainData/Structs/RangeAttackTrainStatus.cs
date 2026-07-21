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
        // 버스트 지속시간 증가량(초). config의 BurstDuration(base)에 가산 — 버스트 포탑(냉기)만 의미 있음.
        public float BurstDuration;
    }
}