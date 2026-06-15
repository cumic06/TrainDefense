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
        private const float StunDuration = 2f;

        // 낙뢰 시각 이펙트 프리팹 경로(Resources). 수명은 프리팹의 AutoReleaseEffect가 관리.
        private const string LightningEffectPath = "Prefabs/LightningStrike";

        protected override bool OnUse()
        {
            var projectileSkillData = trainSkillData?.ProjectileData;
            if (projectileSkillData == null) return false;

            float strikeRadius = projectileSkillData.Range;
            if (strikeRadius <= 0f) return false;

            // 낙뢰 피해 = 포탑 공격력 × 4 (공격력 비례). 비-투사체형이면 스킬 고정값 폴백.
            float damage = projectileSkillData.Damage;
            if (owner is IProjectileAttacker attacker)
            {
                damage = attacker.CurrentAttackDamage * 4f;
            }

            // 낙뢰 중심 = 카메라 화면 정중앙 (적 위치 무관, 고정).
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector2 strikeCenter = cam.transform.position;

            // 낙뢰 이펙트·사운드는 적 유무와 무관하게 항상 발동 (스킬 쓰면 무조건 떨어짐).
            ResourceManager.Instance?.SpawnPath(LightningEffectPath, strikeCenter);
            SoundManager.Instance?.PlaySFX(SoundType.SFX_Game_LightningStrike);

            // 반경 내 적이 있으면 데미지 + 스턴.
            var targets = Physics2D.OverlapCircleAll(strikeCenter, strikeRadius)
                .Select(c => c.TryGetComponent(out Monster m) ? m : null)
                .Where(m => m != null && m.IsActive && m.gameObject.activeInHierarchy)
                .ToArray();

            foreach (var target in targets)
            {
                target.TakeDamage(damage);
                target.Stun(StunDuration);
            }

            return true;
        }
    }
}
