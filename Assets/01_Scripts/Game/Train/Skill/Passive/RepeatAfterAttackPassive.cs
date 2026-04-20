using System.Collections;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 직후 일정 간격으로 NormalAttack을 N회 추가 발사. (엘리트 레이저: 0.2s × 2회)
    /// DSL: "RepeatAfterAttack:delay:count"
    /// </summary>
    public class RepeatAfterAttackPassive : TrainPassiveSkill
    {
        public float Delay { get; private set; } = 0.2f;
        public int RepeatCount { get; private set; } = 2;

        public static RepeatAfterAttackPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!float.TryParse(parts[1], out float delay) || delay < 0f) return null;
            if (!int.TryParse(parts[2], out int count) || count <= 0) return null;
            return new RepeatAfterAttackPassive { Delay = delay, RepeatCount = count };
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
            if (target == null || RepeatCount <= 0) return;
            if (Owner is not TurretTrain turret) return;
            turret.StartCoroutine(Run(turret));
        }

        private IEnumerator Run(TurretTrain turret)
        {
            for (int i = 0; i < RepeatCount; i++)
            {
                yield return new WaitForSeconds(Delay);
                if (turret == null || turret.IsDead) yield break;
                turret.RepeatNormalAttack();
            }
        }
    }
}
