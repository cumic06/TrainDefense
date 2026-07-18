using System.Collections.Generic;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.SkillTree
{
    /// <summary>
    /// DB(skillNodeDataList)가 비어 있을 때 사용하는 코드 기본 노드셋 (EliteData 기본 4종 선례).
    /// ⚠️ 샘플 규모(12노드) — 본 콘텐츠(30~50노드)는 노드 시트 확정 후 DB/임포터로 채운다. DB에 1개라도 있으면 무시된다.
    /// 배치: 레인 3개(화력/방어/유틸) × 세로 체인, row 0 = 최하단 출발역.
    /// 레인 종착역은 TurretUnlock — 습득 시 폭발(40001)/냉기(40002) 포탑이 삼중택일에 등장한다.
    /// </summary>
    public static class SkillTreeDefaultNodes
    {
        private static List<SkillNodeData> _nodes;

        public static IReadOnlyList<SkillNodeData> Nodes => _nodes ??= _Build();

        private static List<SkillNodeData> _Build()
        {
            return new List<SkillNodeData>
            {
                // ── 화력 레인 ──────────────────────────────────────────
                _Stat("skill_fire_damage", SkillTreeLane.Firepower, row: 0,
                    needPoint: 1, maxLevel: 5, prerequisites: null,
                    new SimpleStat { Type = StatType.AttackDamage, Value = 2f }),
                _Stat("skill_fire_speed", SkillTreeLane.Firepower, row: 1,
                    needPoint: 2, maxLevel: 3, prerequisites: new[] { "skill_fire_damage" },
                    new SimpleStat { Type = StatType.AttackInterval, Value = -0.03f }),
                _Stat("skill_fire_crit", SkillTreeLane.Firepower, row: 2,
                    needPoint: 3, maxLevel: 3, prerequisites: new[] { "skill_fire_speed" },
                    new SimpleStat { Type = StatType.CriticalChance, Value = 2f }),
                _Stat("skill_fire_critdmg", SkillTreeLane.Firepower, row: 3,
                    needPoint: 4, maxLevel: 3, prerequisites: new[] { "skill_fire_crit" },
                    new SimpleStat { Type = StatType.CriticalDamage, Value = 10f }),
                _Unlock("skill_fire_unlock", SkillTreeLane.Firepower, row: 4,
                    needPoint: 5, prerequisites: new[] { "skill_fire_critdmg" },
                    unlockTrainId: "40001"),   // 폭발 포탑 (ExplosionRangeTrain)

                // ── 방어 레인 ──────────────────────────────────────────
                _Passive("skill_def_hp", SkillTreeLane.Defense, row: 0,
                    needPoint: 1, maxLevel: 5, prerequisites: null,
                    SkillTreePassiveType.MaxHp, valuePerLevel: 5f),
                _Passive("skill_def_regen", SkillTreeLane.Defense, row: 1,
                    needPoint: 2, maxLevel: 3, prerequisites: new[] { "skill_def_hp" },
                    SkillTreePassiveType.HealthRegen, valuePerLevel: 1f),
                _Passive("skill_def_turret", SkillTreeLane.Defense, row: 2,
                    needPoint: 5, maxLevel: 2, prerequisites: new[] { "skill_def_regen" },
                    SkillTreePassiveType.MaxTurretCount, valuePerLevel: 1f),

                // ── 유틸 레인 ──────────────────────────────────────────
                _Stat("skill_util_range", SkillTreeLane.Utility, row: 0,
                    needPoint: 1, maxLevel: 3, prerequisites: null,
                    new SimpleStat { Type = StatType.AttackRange, Value = 0.5f }),
                _Passive("skill_util_reroll", SkillTreeLane.Utility, row: 1,
                    needPoint: 3, maxLevel: 2, prerequisites: new[] { "skill_util_range" },
                    SkillTreePassiveType.FreeReroll, valuePerLevel: 1f),
                _Stat("skill_util_area", SkillTreeLane.Utility, row: 2,
                    needPoint: 3, maxLevel: 3, prerequisites: new[] { "skill_util_reroll" },
                    new SimpleStat { Type = StatType.AttackArea, Value = 0.2f }),
                _Unlock("skill_util_unlock", SkillTreeLane.Utility, row: 3,
                    needPoint: 5, prerequisites: new[] { "skill_util_area" },
                    unlockTrainId: "40002"),   // 냉기 포탑 (ColdAirRangeTrain)
            };
        }

        private static SkillNodeData _Stat(string id, SkillTreeLane lane, int row,
            int needPoint, int maxLevel, string[] prerequisites, params SimpleStat[] stats)
        {
            return new SkillNodeData(id, iconId: "",
                name: $"SkillTree_{id}_Name", description: $"SkillTree_{id}_Desc",
                lane, row, col: 0, needPoint, maxLevel, growthRate: 1.5f, prerequisites,
                SkillNodeCategory.TurretStat, stats,
                SkillTreePassiveType.MaxHp, passiveValuePerLevel: 0f);
        }

        private static SkillNodeData _Passive(string id, SkillTreeLane lane, int row,
            int needPoint, int maxLevel, string[] prerequisites,
            SkillTreePassiveType passiveType, float valuePerLevel)
        {
            return new SkillNodeData(id, iconId: "",
                name: $"SkillTree_{id}_Name", description: $"SkillTree_{id}_Desc",
                lane, row, col: 0, needPoint, maxLevel, growthRate: 1.5f, prerequisites,
                SkillNodeCategory.Passive, stats: null,
                passiveType, valuePerLevel);
        }

        private static SkillNodeData _Unlock(string id, SkillTreeLane lane, int row,
            int needPoint, string[] prerequisites, string unlockTrainId)
        {
            return new SkillNodeData(id, iconId: "",
                name: $"SkillTree_{id}_Name", description: $"SkillTree_{id}_Desc",
                lane, row, col: 0, needPoint, maxLevel: 1, growthRate: 1.5f, prerequisites,
                SkillNodeCategory.TurretUnlock, stats: null,
                SkillTreePassiveType.MaxHp, passiveValuePerLevel: 0f,
                unlockTrainId);
        }
    }
}
