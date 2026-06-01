using System.Collections;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TrainSelfBuffSkillAction : TrainSkillAction
    {
        // 셀프버프 지속 동안 터렛에 표시되는 이펙트 프리팹 경로(Resources). 스킬 ID로 구분.
        // 예) 속사 모드(50001) → "Prefabs/SkillBuffEffects/50001". 프리팹 없으면 이펙트 없이 버프만 적용.
        private const string BuffEffectPathPrefix = "Prefabs/SkillBuffEffects/";

        protected override bool OnUse()
        {
            if (owner == null || trainSkillData == null)
                return false;

            if (trainSkillData.BuffDuration <= 0f)
                return false;

            var buffs = trainSkillData.Buffs;
            if (buffs == null || buffs.Count == 0)
                return false;

            for (int i = 0; i < buffs.Count; i++)
            {
                var entry = buffs[i];
                owner.ApplyTimedStat(entry.StatType, entry.Percent, trainSkillData.BuffDuration);
            }

            _TrySpawnBuffEffect();
            return true;
        }

        // 해당 스킬용 이펙트 프리팹이 있으면 터렛 위치에 스폰하고, 버프 시간(BuffDuration)이 끝나면 제거한다.
        private void _TrySpawnBuffEffect()
        {
            if (string.IsNullOrEmpty(trainSkillData.Id))
                return;

            var fx = ResourceManager.Instance?.SpawnPath(
                BuffEffectPathPrefix + trainSkillData.Id,
                owner.transform.position, Quaternion.identity, owner.transform);
            if (fx == null)
                return;

            owner.StartCoroutine(_ReleaseAfter(fx, trainSkillData.BuffDuration));
        }

        private static IEnumerator _ReleaseAfter(GameObject fx, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (fx == null)
                yield break;
            if (ResourceManager.Instance != null)
                ResourceManager.Instance.Destroy(fx);
            else
                Object.Destroy(fx);
        }
    }
}
