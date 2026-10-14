
namespace TrainDefense.Game
{
    /// <summary>
    /// N번째 공격마다 사거리 안 모든 적에게 공격력 %만큼 피해를 주고 기절시킨다. (엘리트 전기 방전: 5번째마다 200%, 1초)
    /// DSL: "DischargeEveryN:N:damage_percent:stun_seconds"
    /// </summary>
    public class DischargeEveryNAttacksPassive : TrainPassiveSkill
    {
        public int EveryN { get; private set; }
        public float DamagePercent { get; private set; }
        public float StunDuration { get; private set; }

        private int _attackCount;

        public static DischargeEveryNAttacksPassive From(string[] parts)
        {
            if (parts.Length < 4) return null;
            if (!int.TryParse(parts[1].Trim(), out int n) || n <= 0) return null;
            if (!TryParseFloat(parts[2], out float damagePercent) || damagePercent <= 0f) return null;
            if (!TryParseFloat(parts[3], out float stunDuration) || stunDuration < 0f) return null;
            return new DischargeEveryNAttacksPassive { EveryN = n, DamagePercent = damagePercent, StunDuration = stunDuration };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t) t.OnAttacked += _OnAttacked;
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.OnAttacked -= _OnAttacked;
        }

        private void _OnAttacked(Monster _)
        {
            _attackCount++;
            if (_attackCount % EveryN != 0) return;
            if (Owner is not TurretTrain turret || turret.IsDead) return;

            // 포탑이 이번 틱에 탐지한 사거리 안 적을 그대로 쓴다. (다시 탐지하지 않음)
            foreach (Monster monster in turret.TargetMonsters)
            {
                if (monster == null || !monster.IsActive) continue;

                turret.SpawnProjectileAtWorldPositionPublic(monster, monster.transform.position);
                turret.DealDirectDamage(monster, DamagePercent / 100f);
                if (StunDuration > 0f)
                    TrainCombatRecorder.ApplyStun(monster, StunDuration, turret);
            }
        }
    }
}
