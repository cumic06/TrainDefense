using System.Collections.Generic;
using System.Globalization;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 속도/범위 스탯 변경 + TargetPosAttack 후 랜덤 위치 추가 폭격.
    /// DSL: "RapidBombard:intervalPct:areaPct:count:halfX:halfY"
    /// 예: "RapidBombard:-50:-50:2:10:7"
    /// </summary>
    public class RapidBombardPassive : TrainPassiveSkill
    {
        public float IntervalPct { get; private set; }
        public float AreaPct { get; private set; }
        public int Count { get; private set; }
        public float HalfX { get; private set; }
        public float HalfY { get; private set; }

        public static RapidBombardPassive From(string[] parts)
        {
            if (parts.Length < 6) return null;
            if (!TryParseFloat(parts[1], out float intervalPct)) return null;
            if (!TryParseFloat(parts[2], out float areaPct)) return null;
            if (!int.TryParse(parts[3], out int count) || count <= 0) return null;
            if (!TryParseFloat(parts[4], out float halfX) || halfX <= 0f) return null;
            if (!TryParseFloat(parts[5], out float halfY) || halfY <= 0f) return null;

            return new RapidBombardPassive
            {
                IntervalPct = intervalPct,
                AreaPct = areaPct,
                Count = count,
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

            if (Owner is TurretTrain t) t.OnTargetPosAttacked += HandleTargetPosAttacked;
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.OnTargetPosAttacked -= HandleTargetPosAttacked;
        }

        private void HandleTargetPosAttacked()
        {
            if (Owner is not TurretTrain turret) return;
            var target = turret.GetNearTargetMonsterPublic();
            if (target == null) return;

            for (int i = 0; i < Count; i++)
            {
                var pos = new Vector3(
                    Random.Range(-HalfX, HalfX),
                    Random.Range(-HalfY, HalfY),
                    0f);
                turret.SpawnProjectileAtWorldPositionPublic(target, pos);
            }
        }
    }
}
