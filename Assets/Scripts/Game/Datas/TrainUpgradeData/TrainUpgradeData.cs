using UnityEngine;
using System;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Train 업그레이드 데이터 (기본 Train 및 MainTrain용)
    /// </summary>
    [Serializable]
    public class TrainUpgradeData : ITrainUpgradeData, IDescribableData, IIconData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string iconId;
        [Header("UI Info")]
        private Sprite icon;

        [SerializeField]
        private string name;

        [SerializeField]
        [TextArea(2, 4)]
        private string description;

        [Header("Upgrade Stats")]
        [SerializeField]
        private TrainStatusData statusUpgrade;
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
                        Debug.LogWarning($"TrainUpgradeData [{id}]: Icon not found at 'Sprite/{iconId}'");
                    }
                }
                return icon;
            }
        }
        #endregion

        #region ITrainUpgradeData
        public string UpgradeName => name;
        public TrainStatusData StatusUpgrade => statusUpgrade;
        #endregion
    }
}
