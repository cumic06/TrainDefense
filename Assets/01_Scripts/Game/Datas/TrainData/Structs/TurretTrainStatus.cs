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
        // 버스트 지속시간 증가량(초). config의 BurstDuration(base)에 가산 — 버스트 포탑(화염)만 의미 있음.
        public float BurstDuration;
    }
}