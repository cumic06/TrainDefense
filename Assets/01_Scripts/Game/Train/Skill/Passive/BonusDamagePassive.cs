using System;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 조건에 맞는 적에게 주는 피해를 늘린다. 포탑이 주는 모든 피해(투사체·장판·즉시 피해)에 적용된다.
    /// DSL: "BonusDamage:Condition:percent"
    /// Condition:
    ///   Elite   — 엘리트 적에게 +percent%                                   예: "BonusDamage:Elite:60"
    ///   Stunned — 기절한 적에게 +percent%                                   예: "BonusDamage:Stunned:50"
    ///   Slowed  — 둔화된 적에게 +percent%                                   예: "BonusDamage:Slowed:50"
    ///   Far     — 사거리 끝에 가까울수록 최대 +percent% (거리 비례)           예: "BonusDamage:Far:100"
    /// </summary>
    public class BonusDamagePassive : TrainPassiveSkill
    {
        private enum BonusCondition
        {
            Elite,
            Stunned,
            Slowed,
            Far,
        }

        private BonusCondition _condition;
        private float _percent;

        public static BonusDamagePassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!Enum.TryParse(parts[1].Trim(), true, out BonusCondition condition)) return null;
            if (!TryParseFloat(parts[2], out float percent) || percent <= 0f) return null;

            return new BonusDamagePassive { _condition = condition, _percent = percent };
        }

        public override void Subscribe()
        {
            Owner?.RegisterOutgoingDamageModifier(_ModifyDamage);
        }

        public override void Unsubscribe()
        {
            Owner?.UnregisterOutgoingDamageModifier(_ModifyDamage);
        }

        private float _ModifyDamage(Monster target, float damage)
        {
            if (target == null) return damage;

            switch (_condition)
            {
                case BonusCondition.Elite:
                    return target.IsElite ? damage * (1f + _percent / 100f) : damage;

                case BonusCondition.Stunned:
                    return target.IsStunned ? damage * (1f + _percent / 100f) : damage;

                case BonusCondition.Slowed:
                    return target.IsSlowed ? damage * (1f + _percent / 100f) : damage;

                case BonusCondition.Far:
                    return damage * (1f + _percent / 100f * _GetDistanceRatio(target));

                default:
                    return damage;
            }
        }

        // 포탑~적 거리 ÷ 사거리 (0~1). 사거리를 모르면 보너스 없음.
        private float _GetDistanceRatio(Monster target)
        {
            if (Owner == null) return 0f;

            float range = Owner.CurrentAttackRange;
            if (range <= 0f) return 0f;

            float distance = Vector2.Distance(Owner.transform.position, target.transform.position);

            return Mathf.Clamp01(distance / range);
        }
    }
}
