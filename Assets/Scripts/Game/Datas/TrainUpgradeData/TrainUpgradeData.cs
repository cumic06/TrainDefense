using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Train 업그레이드 데이터 (독립적인 클래스)
    /// </summary>
    [System.Serializable]
    public class TrainUpgradeData
    {
        #region Fields
        [Header("UI Info")]
        [SerializeField]
        private Sprite icon;
        
        [SerializeField]
        private string upgradeName;
        
        [SerializeField]
        [TextArea(2, 4)]
        private string description;

        [Header("Upgrade Stats")]
        [SerializeField]
        private TrainStatusData statusUpgrade;

        [Header("Extended Upgrade Data (Optional)")]
        [Tooltip("TurretTrain 등 추가 업그레이드 데이터가 필요한 경우 사용")]
        [SerializeReference]
        private TrainUpgradeExtension extensionData;
        #endregion

        public Sprite Icon => icon;
        public string UpgradeName => upgradeName;
        public string Description => description;
        public TrainStatusData StatusUpgrade => statusUpgrade;
        public TrainUpgradeExtension ExtensionData => extensionData;
    }
}

