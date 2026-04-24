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
        private float baseInspectionTime;
        [SerializeField]
        private int stationCount;
        [SerializeField]
        private float stageEndTime;
        [SerializeField]
        private float spawnInterval;
        [SerializeField]
        private StageSpawnData[] spawnDatas;
        [SerializeField]
        private MapData mapData;
        [SerializeField]
        private Sprite stageImage;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        public float BaseInspectionTime => baseInspectionTime;
        public int StationCount => stationCount;
        public float StageEndTime => stageEndTime;
        public float SpawnInterval => spawnInterval;
        public StageSpawnData[] SpawnDatas => spawnDatas;
        public MapData MapData => mapData;
        public Sprite StageImage => stageImage;
    }

    [Serializable]
    public class StageSpawnData
    {
        public string MonsterId;
        public float Probability;
        public int SpawnLevel;

        public StageSpawnData(string monsterId, float probability, int spawnLevel)
        {
            MonsterId = monsterId;
            Probability = probability;
            SpawnLevel = spawnLevel;
        }
    }
}