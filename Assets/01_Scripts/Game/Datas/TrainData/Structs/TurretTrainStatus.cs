using System;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public struct TurretTrainStatus
    {
        public float AttackDamage;
        public float AttackRange;
        public float AttackArea;
        public int AttackCount;
        public float AttackInterval;
        public int TargetCount;
        public float CriticalChance;
        public float CriticalDamage;
        // 버스트 지속시간(초). 09-26 지속 공격 폐지로 런타임은 읽지 않는다. 엑셀 burst_duration 열과 짝이라 남겨 둔다.
        public float BurstDuration;
    }
}