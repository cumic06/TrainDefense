using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    /// <summary>
    /// interval초마다 사거리 안에서 남은 체력이 가장 많은 적에게 낙뢰를 떨어뜨린다. (전기 피뢰침)
    /// 공격력 damage_percent% 즉시 피해 + stun_seconds초 기절. 사거리 안에 적이 없으면 대기했다가 들어오는 즉시 친다.
    /// DSL: "LightningRod:interval:damage_percent:stun_seconds"
    /// 예: "LightningRod:6:400:1"
    /// </summary>
    public class LightningRodPassive : TrainPassiveSkill
    {
        private float _interval;
        private float _damagePercent;
        private float _stunDuration;
        private float _elapsedTime;

        public static LightningRodPassive From(string[] parts)
        {
            if (parts.Length < 4) return null;
            if (!TryParseFloat(parts[1], out float interval) || interval <= 0f) return null;
            if (!TryParseFloat(parts[2], out float damagePercent) || damagePercent <= 0f) return null;
            if (!TryParseFloat(parts[3], out float stunDuration) || stunDuration < 0f) return null;

            return new LightningRodPassive { _interval = interval, _damagePercent = damagePercent, _stunDuration = stunDuration };
        }

        public override void Tick(float deltaTime)
        {
            if (Owner is not TurretTrain turret || turret.IsDead) return;

            if (_elapsedTime < _interval)
            {
                _elapsedTime += deltaTime;

                return;
            }

            Monster target = _FindToughestTarget(turret);
            if (target == null) return;

            _elapsedTime = 0f;
            _Strike(turret, target);
        }

        // 포탑이 이번 틱에 탐지한 사거리 안 적 중 남은 체력이 가장 많은 적.
        private static Monster _FindToughestTarget(TurretTrain turret)
        {
            Monster toughest = null;
            foreach (Monster monster in turret.TargetMonsters)
            {
                if (monster == null || !monster.IsActive || monster.IsDead) continue;

                if (toughest == null || monster.CurrentHp > toughest.CurrentHp)
                    toughest = monster;
            }

            return toughest;
        }

        private void _Strike(TurretTrain turret, Monster target)
        {
            // 연출은 방전과 같이 포탑 투사체(전기)를 대상 위치에 띄운다.
            turret.SpawnProjectileAtWorldPositionPublic(target, target.transform.position);
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundType.SFX_Game_LightningStrike);

            turret.DealDirectDamage(target, _damagePercent / 100f);
            if (_stunDuration > 0f)
                TrainCombatRecorder.ApplyStun(target, _stunDuration, turret);
        }
    }
}
