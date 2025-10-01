using System;
using UnityEngine;

namespace TrainDefense.Game.Data
{
    [CreateAssetMenu(fileName = "TrainData", menuName = "Data/TrainData/TrainData")]
    public class TrainData : ScriptableObject
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private Sprite icon;
        [SerializeField]
        private string description;
        [SerializeField]
        private TrainStatusData trainStatusData;
        [SerializeField]
        private Train trainPrefab;
        [SerializeField]
        private bool isMainTrain;
        #endregion

        public string Id => id;
        public Sprite Icon => icon;
        public string Description => description;
        public TrainStatusData TrainStatusData => trainStatusData;
        public Train TrainPrefab => trainPrefab;
        public bool IsMainTrain => isMainTrain;
    }

    [Serializable]
    public struct TrainStatusData
    {
        public int MaxHp;
    }
}