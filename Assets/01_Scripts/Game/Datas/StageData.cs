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
        [SerializeField]
        [Tooltip("이 맵에서 엘리트 처치 시 주는 재화 배율. 빠른(약한 몹) 맵은 낮게, 느린(강한 몹) 맵은 높게 — 맵별 재화 획득률 균등화용. 0이면 1로 처리.")]
        private float eliteRewardMultiplier = 1f;
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
        public float EliteRewardMultiplier => eliteRewardMultiplier > 0f ? eliteRewardMultiplier : 1f;
    }

    [Serializable]
    public class StageSpawnData
    {
        public string MonsterId;
        public float Probability;
        public int SpawnLevel;
        [Tooltip("0보다 크면 이 몬스터가 엘리트로 등장할 수 있고, 0이면 엘리트로 등장하지 않는다. (초반 곰/고블린 거인/미노타우로스 등 강한 몬스터를 엘리트 대상에서 제외할 때 0)")]
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