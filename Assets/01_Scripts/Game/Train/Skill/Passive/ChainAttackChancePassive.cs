using System.Collections;
using System.Globalization;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 직후 percent% 확률로 NormalAttack 1회 추가.
    /// DSL: "ChainAttackChance:percent:delay"
    /// delay 생략 시 즉시 발사.
    /// </summary>
    public class ChainAttackChancePassive : TrainPassiveSkill
    {
        public float ChancePercent { get; private set; }
        public float Delay { get; private set; }

        public static ChainAttackChancePassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!float.TryParse(parts[1], out float pct) || pct <= 0f) return null;

            float delay = 0f;
            if (parts.Length >= 3)
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out delay);

            return new ChainAttackChancePassive { ChancePercent = pct, Delay = Mathf.Max(0f, delay) };
        }

        public override void Subscribe()
        {
            if (Owner is IAttackEvents e) e.OnAttacked += HandleAttacked;
        }

        public override void Unsubscribe()
        {
            if (Owner is IAttackEvents e) e.OnAttacked -= HandleAttacked;
        }

        private void HandleAttacked(Monster target)
        {
            if (target == null) return;
            if (Owner is not IProjectileAttacker attacker) return;
            if (!UtilMath.CheckProbability(ChancePercent)) return;

            GameEventSystem.Publish(new LuckyEvent(Owner.transform.position));

            if (Delay > 0f)
                Owner.StartCoroutine(FireAfterDelay(attacker));
            else
                attacker.RepeatNormalAttack();
        }

        private IEnumerator FireAfterDelay(IProjectileAttacker attacker)
        {
            yield return new WaitForSeconds(Delay);
            if (Owner == null || Owner.IsDead) yield break;
            attacker.RepeatNormalAttack();
        }
    }
}
