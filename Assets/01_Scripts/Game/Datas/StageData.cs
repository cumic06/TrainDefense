using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class StageData : IData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private float[] stageInspectionTime;
        [SerializeField]
        private float stageEndTime;
        [SerializeField]
        private StageSpawnData[] spawnDatas;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        public float[] StageInspectionTime => stageInspectionTime;
        public float StageEndTime => stageEndTime;

        public StageSpawnData[] SpawnDatas => spawnDatas;
    }

    [Serializable]
    public class StageSpawnData
    {
        public string MonsterId;
        public float Probability;

        public StageSpawnData(string monsterId, float probability)
        {
            MonsterId = monsterId;
            Probability = probability;
        }
    }
}