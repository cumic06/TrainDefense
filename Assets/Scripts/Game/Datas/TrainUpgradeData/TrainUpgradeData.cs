using System;
using UnityEngine;

namespace TrainDefense.Game.Data
{
    [CreateAssetMenu(fileName = "TrainUpgradeData", menuName = "Data/TrainUpgradeData/TrainUpgradeData")]
    public class TrainUpgradeData : ScriptableObject
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