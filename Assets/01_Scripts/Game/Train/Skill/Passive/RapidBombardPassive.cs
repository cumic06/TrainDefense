using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 기본 TargetPosAttack을 포탑 사거리 안 무작위 위치 공격으로 대체. 포탄 수(TargetCount)·위력은 같은 행의 StatModifier가 맡는다.
    /// DSL: "RapidBombard"
    /// 예: "StatModifier:AttackInterval:-50:TargetCount:5|RapidBombard"
    /// </summary>
    public class RapidBombardPassive : TrainPassiveSkill
    {
        public static RapidBombardPassive From(string[] parts)
        {
            return new RapidBombardPassive();
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t)
                t.TargetPosOverride = () =>
                {
                    Vector2 offset = Random.insideUnitCircle * t.CurrentAttackRange;
                    return (Vector3?)(t.transform.position + new Vector3(offset.x, offset.y, 0f));
                };
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.TargetPosOverride = null;
        }
    }
}
