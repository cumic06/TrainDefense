using System;
using UnityEngine;
using Sirenix.OdinInspector;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 영구(메타) 업그레이드 정의. 한 판이 끝나도 유지되며, 엘리트 몬스터 처치 재화로 구매한다.
    /// TurretStat: 포탑·레인지 스탯 강화 (SimpleStat[], 레벨당 가산 → GetBonus(StatType)).
    /// Passive: 게임 전반 상시 효과 (PermanentUpgradeType + 레벨당 값 → GetValue(type)). 각 시스템이 조회한다.
    /// </summary>
    [Serializable]
    public class PermanentUpgradeData : IDescribableData, IIconData
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
        [Tooltip("영구 재화 기준 기본 비용 (레벨 0 → 1)")]
        private int needMoney;
        [SerializeField]
        private int maxUpgradeCount;
        [SerializeField]
        private float growthRate = 1.065f;

        [SerializeField]
        private PermanentUpgradeCategory category;

        [SerializeField]
        [ShowIf("category", PermanentUpgradeCategory.TurretStat)]
        [Tooltip("포탑·레인지 스탯 강화 (레벨당 가산)")]
        private SimpleStat[] stats;

        [SerializeField]
        [ShowIf("category", PermanentUpgradeCategory.Passive)]
        private PermanentUpgradeType passiveType;
        [SerializeField]
        [ShowIf("category", PermanentUpgradeCategory.Passive)]
        [Tooltip("패시브 효과 레벨당 값 (예: +1 개수, +10%)")]
        private float passiveValuePerLevel;
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
                        Debug.LogWarning($"PermanentUpgradeData [{id}]: Icon not found at 'Sprite/{iconId}'");
                    }
                }
                return icon;
            }
        }
        #endregion

        public int NeedMoney => needMoney;
        public string UpgradeName => Name;
        public int MaxUpgradeCount => maxUpgradeCount;
        public float GrowthRate => growthRate > 0f ? growthRate : 1.065f;

        public PermanentUpgradeCategory Category => category;
        public IStat[] Stats => stats;
        public PermanentUpgradeType PassiveType => passiveType;
        public float PassiveValuePerLevel => passiveValuePerLevel;

        /// <summary>
        /// 현재 레벨 기준 다음 구매 비용: needMoney * GrowthRate^currentLevel
        /// </summary>
        public int GetCostAtLevel(int currentLevel)
        {
            if (currentLevel <= 0) return needMoney;
            return Mathf.RoundToInt(needMoney * Mathf.Pow(GrowthRate, currentLevel));
        }
    }
}
