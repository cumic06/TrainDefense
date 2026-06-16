using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 공격형 기차 데이터(포탑/범위) 공통 마커 + 공통 접근자.
    /// 타입별 status 구조체는 서로 달라 인터페이스로 묶지 않지만, "공격 기차인지" 판별과
    /// 공통 필드(공격력 배율/투사체)는 이 인터페이스로 구체 타입 캐스팅 없이 접근한다.
    /// </summary>
    public interface IAttackTrainData
    {
        float AttackDamageMultiplier { get; }
        GameObject ProjectilePrefab { get; }
    }
}
