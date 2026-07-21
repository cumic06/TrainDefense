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
        [ShowIf("upgradeDataType", UpgradeDataType.NonTrainUpgrade)]
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
            if (upgradeDataType == UpgradeDataType.NonTrainUpgrade)
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