using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 터렛 기차 상태 정보 구조체
    /// </summary>
    [Serializable]
    public struct TurretTrainStatus
    {
        public float AttackRange;
        public int AttackDamage;
        public int AttackCount;
        public float AttackDelay;
        public int TargetCount;
    }
}