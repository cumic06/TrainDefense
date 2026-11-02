using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 이 포탑의 피해로 죽은 적 자리에서 폭발 투사체가 터진다. 폭발로 죽은 적도 다시 터져 연쇄될 수 있다. (연쇄 발화·집속탄)
    /// DSL: "OnKillExplode:projectile_prefab_id:radius:damage_mul[:shove_scale]"
    /// 예: "OnKillExplode:ExplosionProjectile:2:0.5:0.3" → 반경 2, 공격력 50% 폭발, 밀쳐내기 30%
    /// </summary>
    public class OnKillExplodePassive : TrainPassiveSkill
    {
        private string _projectilePrefabId;
        private float _radius;
        private float _damageMultiplier = 1f;
        private float _shoveScale = 1f;

        public static OnKillExplodePassive From(string[] parts)
        {
            if (parts.Length < 4) return null;
            if (string.IsNullOrWhiteSpace(parts[1])) return null;
            if (!TryParseFloat(parts[2], out float radius) || radius <= 0f) return null;
            if (!TryParseFloat(parts[3], out float damageMultiplier) || damageMultiplier <= 0f) return null;

            float shoveScale = 1f;
            if (parts.Length >= 5 && (!TryParseFloat(parts[4], out shoveScale) || shoveScale < 0f)) return null;

            return new OnKillExplodePassive
            {
                _projectilePrefabId = parts[1].Trim(),
                _radius = radius,
                _damageMultiplier = damageMultiplier,
                _shoveScale = shoveScale,
            };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t) t.OnKilledMonster += _OnKilledMonster;
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.OnKilledMonster -= _OnKilledMonster;
        }

        private void _OnKilledMonster(Monster monster, Vector3 position)
        {
            if (Owner is not TurretTrain turret || turret.IsDead) return;

            var prefab = LoadProjectilePrefab(_projectilePrefabId);
            if (prefab == null) return;

            turret.SpawnExternalProjectileAtPosition(prefab, position, _radius, _damageMultiplier, _shoveScale);
        }
    }
}
