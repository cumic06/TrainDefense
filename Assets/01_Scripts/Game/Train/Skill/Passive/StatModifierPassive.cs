using System;
using System.Collections.Generic;
using System.Globalization;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 선택 즉시 owner 스탯을 영구 변경하는 패시브.
    /// DSL: "StatModifier:StatType1:percent1:StatType2:percent2:..."
    /// 예: "StatModifier:AttackRange:50:AttackArea:50"
    /// </summary>
    public class StatModifierPassive : TrainPassiveSkill
    {
        private IStat[] _stats;

        public static StatModifierPassive From(string[] parts)
        {
            if (parts.Length < 3 || (parts.Length - 1) % 2 != 0) return null;

            var stats = new List<IStat>();
            for (int i = 1; i + 1 < parts.Length; i += 2)
            {
                if (!Enum.TryParse<StatType>(parts[i].Trim(), true, out var statType))
                {
                    Debug.LogWarning($"StatModifierPassive: unknown StatType '{parts[i]}'");
                    return null;
                }
                if (!float.TryParse(parts[i + 1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float percent))
                {
                    Debug.LogWarning($"StatModifierPassive: invalid percent '{parts[i + 1]}'");
                    return null;
                }
                stats.Add(new SimpleStat { Type = statType, Value = percent });
            }

            if (stats.Count == 0) return null;
            return new StatModifierPassive { _stats = stats.ToArray() };
        }

        public override void Subscribe()
        {
            if (Owner == null || _stats == null || _stats.Length == 0) return;
            Owner.ApplyStatsByBaseValue(_stats);
        }
    }
}
