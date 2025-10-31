using UnityEngine;
using System;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// TurretTrain 전용 업그레이드 데이터
    /// </summary>
    [Serializable]
    public class TurretTrainUpgradeData : ITrainUpgradeData, IDescribableData, IIconData
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

        [Header("Turret Train Specific Upgrade")]
        [SerializeField]
        private TurretTrainStatus turretStatusUpgrade;
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
                        Debug.LogWarning($"TurretTrainUpgradeData [{id}]: Icon not found at 'Sprite/{iconId}'");
                    }
                }
                return icon;
            }
        }
        #endregion

        #region ITrainUpgradeData
        public TrainStatusData StatusUpgrade => statusUpgrade;
        #endregion

        public TurretTrainStatus TurretStatusUpgrade => turretStatusUpgrade;
    }
}
