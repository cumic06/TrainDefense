using System;
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
        private static readonly Dictionary<string, Func<string[], TrainPassiveSkill>> _parsers = new(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(OverrideEveryNAttacksPassive), OverrideEveryNAttacksPassive.From },
            { "OverrideEveryN",                     OverrideEveryNAttacksPassive.From },
            { nameof(RepeatAfterAttackPassive),      RepeatAfterAttackPassive.From },
            { "RepeatAfterAttack",                   RepeatAfterAttackPassive.From },
            { nameof(ChainAttackChancePassive),      ChainAttackChancePassive.From },
            { "ChainAttackChance",                   ChainAttackChancePassive.From },
            { nameof(RandomPosBombardPassive),       RandomPosBombardPassive.From },
            { "RandomPosBombard",                    RandomPosBombardPassive.From },
            { nameof(PeriodicSpawnPassive),          PeriodicSpawnPassive.From },
            { "PeriodicSpawn",                       PeriodicSpawnPassive.From },
            { nameof(FollowUpExplosionPassive),      FollowUpExplosionPassive.From },
            { "FollowUpExplosion",                   FollowUpExplosionPassive.From },
            { nameof(StatModifierPassive),           StatModifierPassive.From },
            { "StatModifier",                        StatModifierPassive.From },
        };

        public static void Register(string typeKey, Func<string[], TrainPassiveSkill> parser)
            => _parsers[typeKey] = parser;

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
            if (!_parsers.TryGetValue(typeKey, out var parser))
            {
                Debug.LogWarning($"TrainPassiveSkillFactory: unknown passive type '{typeKey}' in '{entry}'");
                return null;
            }

            var skill = parser(parts);
            if (skill == null)
                Debug.LogWarning($"TrainPassiveSkillFactory: failed to parse '{entry}'");
            return skill;
        }
    }
}
