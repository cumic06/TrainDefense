using UnityEngine;

namespace TrainDefense.Game.Data
{
    public abstract class TrainUpgradeData : ScriptableObject
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private TrainStatusData trainStatusData;
        #endregion

        public string Id => id;
        public TrainStatusData TrainStatusData => trainStatusData;
    }
}