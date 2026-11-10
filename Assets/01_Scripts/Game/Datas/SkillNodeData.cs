using System;
using UnityEngine;
using Sirenix.OdinInspector;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 스킬트리 노드 정의. 스킬 포인트로 습득하며 런 사이에 유지된다 (영구강화와 완전 별도 시스템).
    /// 배치는 데이터 주도 — (lane, row, col)로 런타임 그리드 생성, prerequisites 간선으로 레일이 이어진다.
    /// 노드 하나는 티어 칸 사슬이다. 티어마다 레벨당 효과·최대 레벨·레벨당 비용을 따로 가진다 (tiers).
    /// 앞 티어를 만렙해야 다음 티어가 열리고, 효과는 전부 더해진다. 세이브는 노드 전체 레벨 하나 (티어 구분 없음).
    /// TurretStat: 포탑·레인지 스탯 강화 (stats는 스탯 종류만, 값은 tiers). 공격력·사거리·범위·간격은 기본값 대비 % → GetMultiplier(StatType),
    ///             치명타 확률·피해는 퍼센트 포인트 가산 → GetBonus(StatType).
    /// Passive: 게임 전반 상시 효과 (SkillTreePassiveType → GetValue(type)).
    /// TurretUnlock: 신규 포탑 해금 (unlockTrainId → IsTrainUnlocked, 삼중택일 Add 풀 게이트).
    /// </summary>
    [Serializable]
    public class SkillNodeTier
    {
        [SerializeField]
        [Tooltip("레벨당 효과. 공격력·사거리·공격 범위는 기본값 대비 %(5 = +5%), 공격 간격은 공격 속도 증가 %를 음수로(-3 = 공격 속도 +3%), 치명타 확률·피해는 퍼센트 포인트, 패시브는 그 패시브의 단위")]
        private float valuePerLevel;
        [SerializeField]
        private int maxLevel = 1;
        [SerializeField]
        [Tooltip("이 티어에서 1레벨 올리는 비용")]
        private int costPerLevel = 1;
        [SerializeField]
        [Tooltip("이 티어에서 레벨을 하나 올릴 때마다 비용이 첫 레벨 비용의 몇 %씩 늘어나는지 (25 = 둘째 레벨 ×1.25, 셋째 ×1.5). 0이면 전 레벨 같은 비용")]
        private float costGrowthPercent;

        public float ValuePerLevel => valuePerLevel;
        public int MaxLevel => maxLevel;
        public int CostPerLevel => costPerLevel;

        /// <summary>이 티어에서 levelInTier(0 = 첫 레벨)를 올리는 데 드는 비용. 첫 레벨 비용에 레벨마다 costGrowthPercent%를 선형으로 더한다.</summary>
        public int GetCost(int levelInTier)
            => Mathf.RoundToInt(costPerLevel * (1f + costGrowthPercent * 0.01f * Mathf.Max(0, levelInTier)));
    }

    [Serializable]
    public class SkillNodeData : IDescribableData, IIconData
    {
        // 번역문의 % 표기 — 언어마다 자리와 띄어쓰기가 다르다. 전후 값 양쪽에 같은 표기를 붙이려고 찾는다
        private static readonly string[] PercentValuePatterns = { "{0} %", "{0}%", "%{0}" };
        private const string NextValueArrow = " → ";   // 줄바꿈 안 되는 공백 — 전후 값이 두 줄로 갈라지지 않게

        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string iconId;
        [SerializeField]
        [Tooltip("칸 안 아이콘 크기 배율. 그림이 틀을 꽉 채워 다른 칸보다 커 보이는 노드만 줄인다")]
        private float iconScale = 1f;
        private Sprite icon;
        [SerializeField]
        private string name;
        [SerializeField]
        private string description;

        [SerializeField]
        [Tooltip("트리 레인(계열) — 세로 선로 한 줄")]
        private SkillTreeLane lane;
        [SerializeField]
        [Tooltip("행 (0 = 최하단 출발역, 위로 증가)")]
        private int row;
        [SerializeField]
        [Tooltip("열 (레인 안에서의 가로 위치 오프셋, 분기 노드용)")]
        private int col;

        [SerializeField]
        [Tooltip("티어 칸 사슬 (I부터 순서대로)")]
        private SkillNodeTier[] tiers;

        [SerializeField]
        [Tooltip("선행 노드 id 목록 — 전부 첫 티어를 만렙해야 습득 가능")]
        private string[] prerequisites;

        [SerializeField]
        private SkillNodeCategory category;

        [SerializeField]
        [ShowIf("category", SkillNodeCategory.TurretStat)]
        [Tooltip("강화할 포탑·레인지 스탯 종류. 값은 쓰지 않는다 — 효과는 tiers의 레벨당 값")]
        private SimpleStat[] stats;

        [SerializeField]
        [ShowIf("category", SkillNodeCategory.Passive)]
        private SkillTreePassiveType passiveType;

        [SerializeField]
        [ShowIf("category", SkillNodeCategory.TurretUnlock)]
        [Tooltip("습득 시 삼중택일에 등장하는 포탑(TrainData) id")]
        private string unlockTrainId;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        #region IDescribableData
        public string Name => TrainDefense.Localize.LocalizeHelper.GetByKey(name, name);
        public string Description => TrainDefense.Localize.LocalizeHelper.GetByKey(description, description).Replace("\\n", "\n");
        #endregion

        #region IIconData
        public string IconId => iconId;
        [ShowInInspector, ReadOnly]
        public Sprite Icon
        {
            get
            {
                if (icon == null && !string.IsNullOrEmpty(iconId))
                {
                    icon = Resources.Load<Sprite>($"Sprite/{iconId}");
                    if (icon == null)
                    {
                        Debug.LogWarning($"SkillNodeData [{id}]: Icon not found at 'Sprite/{iconId}'");
                    }
                }
                return icon;
            }
        }
        #endregion

        public float IconScale => iconScale;
        public SkillTreeLane Lane => lane;
        public int Row => row;
        public int Col => col;
        public int TierCount => tiers?.Length ?? 0;
        public string[] Prerequisites => prerequisites;

        public SkillNodeCategory Category => category;
        public IStat[] Stats => stats;
        public SkillTreePassiveType PassiveType => passiveType;
        public string UnlockTrainId => unlockTrainId;

        /// <summary>선행 노드와 같은 줄(같은 레인·같은 col)에 놓여 그 마지막 칸 뒤로 이어지는 노드인지. 이어지는 노드는 선행을 전부 만렙해야 열린다.</summary>
        public bool IsContinuationOf(SkillNodeData prerequisite)
            => prerequisite != null && lane == prerequisite.lane && col == prerequisite.col;

        /// <summary>노드 전체 최대 레벨 = 티어별 최대 레벨의 합.</summary>
        public int MaxLevel => GetLevelsBeforeTier(TierCount + 1);

        /// <summary>티어(1부터) 정의.</summary>
        public SkillNodeTier GetTier(int tier) => tiers[tier - 1];

        /// <summary>이 티어 앞 티어들의 최대 레벨 합 = 이 티어에 들어서는 노드 레벨.</summary>
        public int GetLevelsBeforeTier(int tier)
        {
            int levels = 0;
            for (int index = 0; index < tier - 1 && index < TierCount; index++)
                levels += tiers[index].MaxLevel;

            return levels;
        }

        /// <summary>노드 레벨 중 이 티어에 들어간 레벨 (0 ~ 티어 최대 레벨).</summary>
        public int GetLevelInTier(int nodeLevel, int tier)
            => Mathf.Clamp(nodeLevel - GetLevelsBeforeTier(tier), 0, GetTier(tier).MaxLevel);

        /// <summary>이 티어를 만렙했는지.</summary>
        public bool IsTierMastered(int nodeLevel, int tier)
            => nodeLevel >= GetLevelsBeforeTier(tier + 1);

        /// <summary>지금 레벨을 올릴 차례인 티어인지 (앞 티어 만렙 + 이 티어 미만렙).</summary>
        public bool IsTierInProgress(int nodeLevel, int tier)
            => nodeLevel >= GetLevelsBeforeTier(tier) && !IsTierMastered(nodeLevel, tier);

        /// <summary>지금 레벨을 올릴 차례인 티어. 전부 만렙이면 마지막 티어.</summary>
        public int GetCurrentTier(int nodeLevel)
        {
            for (int tier = 1; tier < TierCount; tier++)
            {
                if (!IsTierMastered(nodeLevel, tier)) return tier;
            }

            return Mathf.Max(TierCount, 1);
        }

        /// <summary>현재 노드 레벨에서 다음 1레벨의 비용. 만렙이면 0.</summary>
        public int GetCostAtLevel(int currentLevel)
        {
            int remaining = currentLevel;
            for (int index = 0; index < TierCount; index++)
            {
                if (remaining < tiers[index].MaxLevel) return tiers[index].GetCost(remaining);

                remaining -= tiers[index].MaxLevel;
            }

            return 0;
        }

        /// <summary>노드 레벨까지의 효과 합계 (티어별 레벨당 값 × 그 티어에 들어간 레벨).</summary>
        public float GetTotalValue(int nodeLevel)
        {
            float total = 0f;
            int remaining = nodeLevel;
            for (int index = 0; index < TierCount && remaining > 0; index++)
            {
                int levelInTier = Mathf.Min(remaining, tiers[index].MaxLevel);
                total += tiers[index].ValuePerLevel * levelInTier;
                remaining -= levelInTier;
            }

            return total;
        }

        /// <summary>
        /// 효과 문구. 설명 템플릿의 {0}에 이 칸이 주는 값의 크기를 넣는다 — 앞 칸에서 쌓인 값은 넣지 않는다 (합계로 쓰면 칸 효과로 오해한다, 11-03 사용자 결정).
        /// 부호·단위는 템플릿이 가진다 ("공격력 +{0}", "공격 간격 -{0}초").
        /// includesNextLevel이면 다음 레벨 값까지 "0.5% → 1%"로 넣는다. 아직 0레벨이면 첫 레벨 값만 넣는다 ("0.5%"). 템플릿에 {0}이 없으면(해금 노드 등) 원문 그대로.
        /// </summary>
        public string GetTierDescription(int nodeLevel, int tier, bool includesNextLevel)
        {
            string template = Description;
            if (!template.Contains("{0}")) return template;

            SkillNodeTier tierData = GetTier(tier);
            int levelInTier = GetLevelInTier(nodeLevel, tier);
            string current = _FormatValue(tierData.ValuePerLevel * levelInTier);
            if (!includesNextLevel) return template.Replace("{0}", current);

            string next = _FormatValue(tierData.ValuePerLevel * (levelInTier + 1));
            if (levelInTier == 0) return template.Replace("{0}", next);

            foreach (string valueWithUnit in PercentValuePatterns)
            {
                if (template.Contains(valueWithUnit))
                    return template.Replace(valueWithUnit, valueWithUnit.Replace("{0}", current) + NextValueArrow + valueWithUnit.Replace("{0}", next));
            }

            return template.Replace("{0}", current + NextValueArrow + next);
        }

        private static string _FormatValue(float value)
            => Mathf.Abs(value).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
