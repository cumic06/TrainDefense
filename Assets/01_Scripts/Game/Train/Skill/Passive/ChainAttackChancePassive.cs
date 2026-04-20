using Cumic;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 직후 percent% 확률로 NormalAttack 1회 추가. (엘리트 저격: 30%)
    /// DSL: "ChainAttackChance:percent"
    /// </summary>
    public class ChainAttackChancePassive : TrainPassiveSkill
    {
        public float ChancePercent { get; private set; }

        public static ChainAttackChancePassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!float.TryParse(parts[1], out float pct) || pct <= 0f) return null;
            return new ChainAttackChancePassive { ChancePercent = pct };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t) t.OnAttacked += HandleAttacked;
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.OnAttacked -= HandleAttacked;
        }

        private void HandleAttacked(Monster target)
        {
            if (target == null) return;
            if (Owner is not TurretTrain turret) return;
            if (!UtilMath.CheckProbability(ChancePercent)) return;
            turret.RepeatNormalAttack();
        }
    }
}
