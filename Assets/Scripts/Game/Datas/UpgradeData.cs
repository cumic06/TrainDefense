using System;
using UnityEngine;
using Sirenix.OdinInspector;

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
        private float upgradeValue;
        [SerializeField]
        private int maxUpgradeCount;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        #region IDescribableData
        public string Name => name;
        public string Description => description;
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
        public string UpgradeName => name;
        public float UpgradeValue => upgradeValue;
        public int MaxUpgradeCount => maxUpgradeCount;
    }
}
