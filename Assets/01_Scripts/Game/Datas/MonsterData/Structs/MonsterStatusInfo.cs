using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 몬스터 상태 정보 구조체
    /// </summary>
    [Serializable]
    public struct MonsterStatusInfo
    {
        public int MaxHp;
        public int Damage;
        public float MoveSpeed;
        public float AttackDelay;
        public int DropExpMin;
        public int DropExpMax;
        public int DropMoneyMin;
        public int DropMoneyMax;
        public float AttackRange;
    }
}
