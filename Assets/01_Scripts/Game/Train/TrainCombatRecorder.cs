namespace TrainDefense.Game
{
    /// <summary>
    /// 아군 포탑이 몬스터에게 피해·기절·둔화를 거는 지점을 한 곳으로 모아, 그 포탑의 전투 기록(TrainCombatRecord)을 쌓는다.
    /// 공격 주체가 Train이 아니거나(몬스터·플레이어 개입 포격) 대상이 Monster가 아니면 기록 없이 그대로 적용만 한다.
    /// </summary>
    public static class TrainCombatRecorder
    {
        public static void DealDamage(IProjectileTarget target, float damage, bool isCritical, IProjectileTarget attacker)
        {
            if (target == null)
                return;

            if (attacker is not Train train || target is not Monster monster)
            {
                target.TakeDamage(damage, isCritical);

                return;
            }

            // 이미 죽은 몬스터에 들어간 피해(같은 프레임 중복 판정)는 세지 않는다.
            bool wasAlive = !monster.IsDead;
            monster.TakeDamage(damage, isCritical);

            if (!wasAlive)
                return;

            var record = train.CombatRecord;
            record.DamageDealt += damage;

            if (isCritical)
                record.CriticalHits++;

            if (monster.IsDead)
                record.Kills++;
        }

        public static void ApplyStun(IProjectileTarget target, float stunDuration, IProjectileTarget attacker)
        {
            if (target == null)
                return;

            if (attacker is not Train train || target is not Monster monster)
            {
                target.Stun(stunDuration);

                return;
            }

            // 기절 중인 적의 기절을 연장한 건 새 기절로 세지 않는다.
            bool wasStunned = monster.IsStunned;
            monster.Stun(stunDuration);

            if (!wasStunned && monster.IsStunned)
                train.CombatRecord.Stuns++;
        }

        public static void ApplySlow(IProjectileTarget target, float slowValue, float duration, IProjectileTarget attacker)
        {
            if (target == null)
                return;

            if (attacker is not Train train || target is not Monster monster)
            {
                target.Slow(slowValue, duration);

                return;
            }

            // 장판은 머무는 동안 매 프레임 둔화를 갱신하므로, 안 걸려 있던 적에게 새로 걸었을 때만 센다.
            bool wasSlowed = monster.IsSlowed;
            monster.Slow(slowValue, duration);

            if (!wasSlowed && monster.IsSlowed)
                train.CombatRecord.Slows++;
        }
    }
}
