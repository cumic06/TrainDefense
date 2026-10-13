using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 기본 TargetPosAttack을 화면 중앙 기준 무작위 위치 공격으로 대체. 공격 속도 등 스탯은 같은 행의 StatModifier가 맡는다.
    /// DSL: "RapidBombard:halfX:halfY"
    /// 예: "StatModifier:AttackInterval:-50|RapidBombard:10:7"  (발사 수는 TargetCount)
    /// </summary>
    public class RapidBombardPassive : TrainPassiveSkill
    {
        public float HalfX { get; private set; }
        public float HalfY { get; private set; }

        public static RapidBombardPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!TryParseFloat(parts[1], out float halfX) || halfX <= 0f) return null;
            if (!TryParseFloat(parts[2], out float halfY) || halfY <= 0f) return null;

            return new RapidBombardPassive
            {
                HalfX = halfX,
                HalfY = halfY
            };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t)
                t.TargetPosOverride = () =>
                {
                    Vector3 center = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                    return (Vector3?)new Vector3(
                        center.x + Random.Range(-HalfX, HalfX),
                        center.y + Random.Range(-HalfY, HalfY),
                        0f);
                };
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.TargetPosOverride = null;
        }
    }
}
