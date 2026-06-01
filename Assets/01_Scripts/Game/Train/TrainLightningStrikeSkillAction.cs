using System.Linq;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 낙뢰 액티브 스킬. 사정거리 내 최근접 몬스터 위치를 중심으로
    /// AoE 피해 + 기절(3초)을 즉발로 적용한다.
    /// </summary>
    public class TrainLightningStrikeSkillAction : TrainSkillAction
    {
        private const float StunDuration = 3f;

        // 낙뢰 시각 이펙트 프리팹 경로(Resources). 수명은 프리팹의 AutoReleaseEffect가 관리.
        private const string LightningEffectPath = "Prefabs/LightningStrike";

        protected override bool OnUse()
        {
            var projectileSkillData = trainSkillData?.ProjectileData;
            if (projectileSkillData == null) return false;

            float strikeRadius = projectileSkillData.Range;
            if (strikeRadius <= 0f) return false;

            float damage = projectileSkillData.Damage;

            var nearest = Physics2D.OverlapCircleAll(owner.transform.position, strikeRadius)
                .Select(c => c.TryGetComponent(out Monster m) ? m : null)
                .Where(m => m != null && m.IsActive && m.gameObject.activeInHierarchy)
                .OrderBy(m => (owner.transform.position - m.transform.position).sqrMagnitude)
                .FirstOrDefault();

            if (nearest == null) return false;

            Vector2 strikeCenter = nearest.transform.position;

            // 낙뢰가 떨어진 지점에 번개 이펙트 스폰 (AutoReleaseEffect가 자동 반환).
            ResourceManager.Instance?.SpawnPath(LightningEffectPath, strikeCenter);
            SoundManager.Instance?.PlaySFX(SoundType.SFX_Game_LightningStrike);

            var targets = Physics2D.OverlapCircleAll(strikeCenter, strikeRadius)
                .Select(c => c.TryGetComponent(out Monster m) ? m : null)
                .Where(m => m != null && m.IsActive && m.gameObject.activeInHierarchy)
                .ToArray();

            foreach (var target in targets)
            {
                target.TakeDamage(damage);
                target.Stun(StunDuration);
            }

            return targets.Length > 0;
        }
    }
}
