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
        [Tooltip("이 스테이지에서 해당 몬스터의 엘리트 등장 확률 배율. 1이면 기본, 0이면 엘리트로 등장하지 않음. (초반 곰/고블린 거인/미노타우로스 등 강한 몬스터는 낮춰서 난이도 조절)")]
        public float EliteChanceMultiplier;

        public StageSpawnData(string monsterId, float probability, int spawnLevel, float eliteChanceMultiplier = 1f)
        {
            MonsterId = monsterId;
            Probability = probability;
            SpawnLevel = spawnLevel;
            EliteChanceMultiplier = eliteChanceMultiplier;
        }
    }
}