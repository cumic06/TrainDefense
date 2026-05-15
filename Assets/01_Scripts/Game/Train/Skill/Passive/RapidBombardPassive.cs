using System.Collections.Generic;
using System.Globalization;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 속도/범위 스탯 변경 + 기본 TargetPosAttack을 랜덤 위치 공격으로 대체.
    /// DSL: "RapidBombard:intervalPct:areaPct:count:halfX:halfY"
    /// 예: "RapidBombard:-50:-50:2:10:7"  (count/halfX/halfY는 범위에만 사용, 발사 수는 AttackCount)
    /// </summary>
    public class RapidBombardPassive : TrainPassiveSkill
    {
        public float IntervalPct { get; private set; }
        public float AreaPct { get; private set; }
        public float HalfX { get; private set; }
        public float HalfY { get; private set; }

        public static RapidBombardPassive From(string[] parts)
        {
            if (parts.Length < 6) return null;
            if (!TryParseFloat(parts[1], out float intervalPct)) return null;
            if (!TryParseFloat(parts[2], out float areaPct)) return null;
            if (!int.TryParse(parts[3], out int _)) return null;
            if (!TryParseFloat(parts[4], out float halfX) || halfX <= 0f) return null;
            if (!TryParseFloat(parts[5], out float halfY) || halfY <= 0f) return null;

            return new RapidBombardPassive
            {
                IntervalPct = intervalPct,
                AreaPct = areaPct,
                HalfX = halfX,
                HalfY = halfY
            };
        }

        private static bool TryParseFloat(string s, out float value)
            => float.TryParse(s?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        public override void Subscribe()
        {
            if (Owner == null) return;

            var stats = new List<IStat>();
            if (IntervalPct != 0f) stats.Add(new SimpleStat { Type = StatType.AttackInterval, Value = IntervalPct });
            if (AreaPct != 0f) stats.Add(new SimpleStat { Type = StatType.AttackArea, Value = AreaPct });
            if (stats.Count > 0) Owner.ApplyStatsByCurrentValue(stats.ToArray());

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
