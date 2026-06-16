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
        // 추가 발사가 첫 타겟을 유지할지 (true면 재조준 안 함). DSL 4번째 인자 "1".
        public bool KeepTarget { get; private set; } = false;

        public static RepeatAfterAttackPassive From(string[] parts)
        {
            if (parts.Length < 3) return null;
            if (!float.TryParse(parts[1], out float delay) || delay < 0f) return null;
            if (!int.TryParse(parts[2], out int count) || count <= 0) return null;
            bool keepTarget = parts.Length >= 4 && parts[3] == "1";
            return new RepeatAfterAttackPassive { Delay = delay, RepeatCount = count, KeepTarget = keepTarget };
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
            if (target == null || RepeatCount <= 0) return;
            if (Owner.AttackModule is not IProjectileAttacker attacker) return;
            // KeepTarget이면 첫 타겟 위치를 캡처 → 타겟이 죽어도 그 방향으로 발사.
            Vector2? aimPosition = KeepTarget ? (Vector2)target.transform.position : (Vector2?)null;
            Owner.StartCoroutine(Run(attacker, aimPosition));
        }

        private IEnumerator Run(IProjectileAttacker attacker, Vector2? aimPosition)
        {
            for (int i = 0; i < RepeatCount; i++)
            {
                yield return new WaitForSeconds(Delay);
                if (Owner == null || Owner.IsDead) yield break;
                attacker.RepeatNormalAttack(aimPosition);
            }
        }
    }
}
