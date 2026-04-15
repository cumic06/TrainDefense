using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 엑셀 셀의 패시브 DSL 문자열을 TrainPassiveSkill 인스턴스 리스트로 파싱.
    /// 형식: "Type:p1:p2:..|Type:p1:p2|..."
    /// 예: "OverrideEveryN:5:elite_machinegun_area|FollowUpExplosion:0.2:elite_explosion_followup"
    /// </summary>
    public static class TrainPassiveSkillFactory
    {
        public static List<TrainPassiveSkill> ParseAll(string raw)
        {
            var result = new List<TrainPassiveSkill>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            var entries = raw.Split('|');
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                var skill = ParseOne(entry.Trim());
                if (skill != null) result.Add(skill);
            }
            return result;
        }

        private static TrainPassiveSkill ParseOne(string entry)
        {
            var parts = entry.Split(':');
            if (parts.Length == 0) return null;

            var typeKey = parts[0].Trim();
            switch (typeKey)
            {
                case nameof(OverrideEveryNAttacksPassive):
                case "OverrideEveryN":
                    return OverrideEveryNAttacksPassive.From(parts);

                case nameof(RepeatAfterAttackPassive):
                case "RepeatAfterAttack":
                    return RepeatAfterAttackPassive.From(parts);

                case nameof(ChainAttackChancePassive):
                case "ChainAttackChance":
                    return ChainAttackChancePassive.From(parts);

                case nameof(RandomPosBombardPassive):
                case "RandomPosBombard":
                    return RandomPosBombardPassive.From(parts);

                case nameof(PeriodicSpawnPassive):
                case "PeriodicSpawn":
                    return PeriodicSpawnPassive.From(parts);

                case nameof(FollowUpExplosionPassive):
                case "FollowUpExplosion":
                    return FollowUpExplosionPassive.From(parts);

                default:
                    Debug.LogWarning($"TrainPassiveSkillFactory: unknown passive type '{typeKey}' in '{entry}'");
                    return null;
            }
        }
    }
}
