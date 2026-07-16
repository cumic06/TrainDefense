using System;
using UnityEngine;
using Sirenix.OdinInspector;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 스킬트리 노드 정의. 스킬 포인트로 습득하며 런 사이에 유지된다 (영구강화와 완전 별도 시스템).
    /// 배치는 데이터 주도 — (lane, row, col)로 런타임 그리드 생성, prerequisites 간선으로 레일이 이어진다.
    /// TurretStat: 포탑·레인지 스탯 강화 (SimpleStat[], 레벨당 가산 → GetBonus(StatType)).
    /// Passive: 게임 전반 상시 효과 (SkillTreePassiveType + 레벨당 값 → GetValue(type)).
    /// </summary>
    [Serializable]
    public class SkillNodeData : IDescribableData, IIconData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string iconId;
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
        [Tooltip("스킬 포인트 기준 기본 비용 (레벨 0 → 1)")]
        private int needPoint = 1;
        [SerializeField]
        private int maxLevel = 1;
        [SerializeField]
        private float growthRate = 1.5f;

        [SerializeField]
        [Tooltip("선행 노드 id 목록 — 전부 1레벨 이상이어야 습득 가능")]
        private string[] prerequisites;

        [SerializeField]
        private SkillNodeCategory category;

        [SerializeField]
        [ShowIf("category", SkillNodeCategory.TurretStat)]
        [Tooltip("포탑·레인지 스탯 강화 (레벨당 가산)")]
        private SimpleStat[] stats;

        [SerializeField]
        [ShowIf("category", SkillNodeCategory.Passive)]
        private SkillTreePassiveType passiveType;
        [SerializeField]
        [ShowIf("category", SkillNodeCategory.Passive)]
        [Tooltip("패시브 효과 레벨당 값 (예: +1 개수, +10%)")]
        private float passiveValuePerLevel;
        #endregion

        public SkillNodeData() { }

        /// <summary>코드 기본 노드셋 구성용 (DB가 비어 있을 때의 폴백 — EliteData 선례).</summary>
        public SkillNodeData(string id, string iconId, string name, string description,
            SkillTreeLane lane, int row, int col,
            int needPoint, int maxLevel, float growthRate, string[] prerequisites,
            SkillNodeCategory category, SimpleStat[] stats,
            SkillTreePassiveType passiveType, float passiveValuePerLevel)
        {
            this.id = id;
            this.iconId = iconId;
            this.name = name;
            this.description = description;
            this.lane = lane;
            this.row = row;
            this.col = col;
            this.needPoint = needPoint;
            this.maxLevel = maxLevel;
            this.growthRate = growthRate;
            this.prerequisites = prerequisites;
            this.category = category;
            this.stats = stats;
            this.passiveType = passiveType;
            this.passiveValuePerLevel = passiveValuePerLevel;
        }

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

        public SkillTreeLane Lane => lane;
        public int Row => row;
        public int Col => col;
        public int NeedPoint => needPoint;
        public int MaxLevel => maxLevel;
        public float GrowthRate => growthRate > 0f ? growthRate : 1.5f;
        public string[] Prerequisites => prerequisites;

        public SkillNodeCategory Category => category;
        public IStat[] Stats => stats;
        public SkillTreePassiveType PassiveType => passiveType;
        public float PassiveValuePerLevel => passiveValuePerLevel;

        /// <summary>
        /// 현재 레벨 기준 다음 습득 비용: needPoint * GrowthRate^currentLevel
        /// </summary>
        public int GetCostAtLevel(int currentLevel)
        {
            if (currentLevel <= 0) return needPoint;

            return Mathf.RoundToInt(needPoint * Mathf.Pow(GrowthRate, currentLevel));
        }
    }
}
