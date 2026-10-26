using System;
using UnityEngine;
using Sirenix.OdinInspector;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class UpgradeData : IDescribableData, IIconData
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
        private int needMoney;
        [SerializeField]
        private int maxUpgradeCount;
        [SerializeField]
        private UpgradeDataType upgradeDataType;
        [SerializeField]
        private UpgradePool upgradePool;
        // 상점 아이템의 등급(1~5). 상점 강화 카드 등급표(stat_upgrade_tier_data)의 가격 배수·등장 시점·테두리 색을 그대로 따른다. 0 = 등급 없음.
        [SerializeField]
        private int grade;

        [SerializeField]
        [HideIf("upgradeDataType", UpgradeDataType.TrainUpgrade)]
        private float upgradeValue;
        [SerializeField]
        [ShowIf("upgradeDataType", UpgradeDataType.TrainUpgrade)]
        private SimpleStat[] stats;
        [SerializeField]
        private float growthRate = 1.065f;
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
                        Debug.LogWarning($"UpgradeData [{id}]: Icon not found at 'Sprite/{iconId}'");
                    }
                }
                return icon;
            }
        }
        #endregion

        public int NeedMoney => needMoney;
        public string UpgradeName => Name;
        public float UpgradeValue => upgradeValue;
        public int MaxUpgradeCount => maxUpgradeCount;
        public UpgradeDataType UpgradeDataType => upgradeDataType;
        public UpgradePool UpgradePool => upgradePool;
        public int Grade => grade;
        public float GrowthRate => growthRate > 0f ? growthRate : 1.065f;

        /// <summary>
        /// 현재 레벨 기준 실제 구매 비용: baseCost * GrowthRate^currentLevel
        /// </summary>
        public int GetCostAtLevel(int currentLevel)
        {
            if (currentLevel <= 0) return needMoney;
            return Mathf.RoundToInt(needMoney * Mathf.Pow(GrowthRate, currentLevel));
        }

        /// <summary>
        /// 레벨당 증가량(부호 유지, 스탯 합). UI 포맷용 숫자 계산 전용 — 문자열 조립은 호출자 책임.
        /// </summary>
        public float GetPerLevelValue()
        {
            if (upgradeDataType != UpgradeDataType.TrainUpgrade)
                return upgradeValue;

            float perLevelValue = 0f;

            if (stats != null)
            {
                foreach (var stat in stats)
                {
                    if (stat.Value == 0)
                        continue;

                    perLevelValue += stat.Value;
                }
            }

            return perLevelValue;
        }

        /// <summary>
        /// 레벨 기준 누적 총 증가량(부호 유지). 이름 포맷 {0}에 쓰인다.
        /// </summary>
        public float GetTotalValueAtLevel(int level)
        {
            return level * GetPerLevelValue();
        }

        /// <summary>
        /// SimpleStat 기반 스탯 업그레이드
        /// </summary>
        public IStat[] Stats => stats;
    }
}