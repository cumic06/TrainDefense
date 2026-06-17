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
        [Tooltip("이 스테이지에서 해당 몬스터의 엘리트 등장 확률 배율. 1이면 기본, 0이면 엘리트로 등장하지 않음. (초반 곰/고블린 거인/미노타우로스 등 강한 몬스터는 낮춰서 난이도 조절)")]
        public float EliteChanceMultiplier;
        [Tooltip("체크 시 이 몬스터의 엘리트 배율이 한 판 동안 0에서 점점 증가(강한 몬스터용 - 초반엔 엘리트로 거의 안 나오고 후반에 증가). 해제 시 진행도와 무관하게 EliteChanceMultiplier가 바로 적용(약한 몬스터용).")]
        public bool EliteChanceRamp;

        public StageSpawnData(string monsterId, float probability, int spawnLevel, float eliteChanceMultiplier = 1f, bool eliteChanceRamp = false)
        {
            MonsterId = monsterId;
            Probability = probability;
            SpawnLevel = spawnLevel;
            EliteChanceMultiplier = eliteChanceMultiplier;
            EliteChanceRamp = eliteChanceRamp;
        }
    }
}