using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 기본 공격 넉백을 제거하고 공격력을 상승시키는 패시브.
    /// DSL: "Strengthen:damage_percent"
    /// 예: "Strengthen:100" → 공격력 100% 상승, 기본 공격 넉백 제거
    /// </summary>
    public class StrengthenPassive : TrainPassiveSkill
    {
        private float _damagePercent;

        public static StrengthenPassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!TryParseFloat(parts[1], out float percent))
            {
                Debug.LogWarning($"StrengthenPassive: invalid damage_percent '{parts[1]}'");
                return null;
            }
            return new StrengthenPassive { _damagePercent = percent };
        }

        public override void Subscribe()
        {
            if (Owner == null) return;

            Owner.ApplyStatsToFinalValue(new IStat[]
            {
                new SimpleStat { Type = StatType.AttackDamage, Value = _damagePercent }
            });

            if (Owner is RangeTrain rangeTrain)
                rangeTrain.SetSuppressMainProjectileShove(true);
        }

        // 공격력은 StatModifierPassive와 같이 되돌리지 않는다(재초기화 때 스탯을 새로 계산). 넉백 억제만 푼다.
        public override void Unsubscribe()
        {
            if (Owner is RangeTrain rangeTrain)
                rangeTrain.SetSuppressMainProjectileShove(false);
        }
    }
}
