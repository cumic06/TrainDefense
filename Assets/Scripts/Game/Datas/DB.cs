using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using TrainDefense.Game;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 게임 내 모든 데이터를 통합 관리하는 중앙 데이터베이스
    /// </summary>
    [CreateAssetMenu(fileName = "DB", menuName = "Data/DB")]
    public class DB : ScriptableObject
    {
        #region Data Collections

        [TabGroup("Monster Data")]
        [InfoBox("몬스터 데이터 관리")]
        [SerializeField]
        public List<MonsterData> monsterDataList = new();

        #region Train Data    
        [TabGroup("Train Data")]
        [InfoBox("기차 데이터 관리")]
        [SerializeField]
        public List<TrainData> trainDataList = new();

        [TabGroup("Train Data")]
        [InfoBox("터렛 기차 데이터 관리")]
        [SerializeField]
        public List<TurretTrainData> turretTrainDataList = new();

        [TabGroup("Train Data")]
        [InfoBox("원거리 기차 데이터 관리")]
        [SerializeField]
        public List<RangeTrainData> rangeTrainDataList = new();
        #endregion

        [TabGroup("Train Upgrade Data")]
        [InfoBox("기차 업그레이드 데이터 관리 (기본 Train 및 MainTrain용)")]
        [SerializeField]
        public List<TrainUpgradeData> trainUpgradeDataList = new();

        [TabGroup("Train Upgrade Data")]
        [InfoBox("터렛 기차 업그레이드 데이터 관리")]
        [SerializeField]
        public List<TurretTrainUpgradeData> turretTrainUpgradeDataList = new();

        [TabGroup("Train Upgrade Data")]
        [InfoBox("원거리 기차 업그레이드 데이터 관리")]
        [SerializeField]
        public List<RangeTrainUpgradeData> rangeTrainUpgradeDataList = new();

        [TabGroup("Stage Data")]
        [InfoBox("스테이지 데이터 관리")]
        [SerializeField]
        public List<StageData> stageDataList = new();

        [TabGroup("Upgrade Data")]
        [InfoBox("일반 업그레이드 데이터 관리")]
        [SerializeField]
        public List<UpgradeData> upgradeDataList = new();

        [TabGroup("TriChoice Database")]
        [InfoBox("3지선다 데이터베이스 (선택지 관리)")]
        [SerializeField]
        private TriChoiceDB triChoiceDB = new();

        #endregion

        #region Public Properties

        public IReadOnlyList<MonsterData> MonsterDataList => monsterDataList;
        public IReadOnlyList<TrainData> TrainDataList => trainDataList;
        public IReadOnlyList<RangeTrainData> RangeTrainDataList => rangeTrainDataList;
        public IReadOnlyList<TurretTrainData> TurretTrainDataList => turretTrainDataList;
        public IReadOnlyList<TrainUpgradeData> TrainUpgradeDataList => trainUpgradeDataList;
        public IReadOnlyList<TurretTrainUpgradeData> TurretTrainUpgradeDataList => turretTrainUpgradeDataList;
        public IReadOnlyList<RangeTrainUpgradeData> RangeTrainUpgradeDataList => rangeTrainUpgradeDataList;
        public IReadOnlyList<StageData> StageDataList => stageDataList;
        public IReadOnlyList<UpgradeData> UpgradeDataList => upgradeDataList;
        public TriChoiceDB TriChoiceDB => triChoiceDB;

        #endregion

        #region Data Access Methods

        /// <summary>
        /// ID로 기차 데이터 검색 (모든 타입에서 검색)
        /// </summary>
        public TrainData GetTrainData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Train ID is null or empty");
                return null;
            }

            var allTrainData = GetAllTrainData();
            var result = allTrainData.FirstOrDefault(t => t.Id == id);
            if (result == null)
            {
                Debug.LogWarning($"Train data with ID '{id}' not found");
            }
            return result;
        }

        /// <summary>
        /// ID로 기차 업그레이드 데이터 검색 (모든 타입에서 통합 검색)
        /// </summary>
        public ITrainUpgradeData GetTrainUpgradeData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Train Upgrade ID is null or empty");
                return null;
            }

            var allUpgradeData = GetAllTrainUpgradeData();
            var result = allUpgradeData.FirstOrDefault(u => u.Id == id);
            if (result == null)
            {
                Debug.LogWarning($"Train upgrade data with ID '{id}' not found");
            }
            
            return result;
        }

        /// <summary>
        /// TrainDataId로 해당 기차의 현재 레벨에 맞는 업그레이드 데이터 목록을 반환
        /// MainTrain을 통해 현재 Train 인스턴스의 레벨을 확인하고, DB의 모든 업그레이드 데이터에서 해당 레벨에 맞는 데이터를 필터링하여 반환
        /// </summary>
        public List<ITrainUpgradeData> GetTrainUpgradeDataByTrainDataId(string trainDataId)
        {
            if (string.IsNullOrEmpty(trainDataId))
            {
                Debug.LogWarning("TrainDataId is null or empty");
                return new List<ITrainUpgradeData>();
            }

            // TrainManager를 통해 MainTrain 접근
            if (TrainManager.Instance == null)
            {
                Debug.LogWarning("TrainManager.Instance is null");
                return new List<ITrainUpgradeData>();
            }

            var mainTrain = TrainManager.Instance.MainTrain;
            if (mainTrain == null)
            {
                Debug.LogWarning("MainTrain is null");
                return new List<ITrainUpgradeData>();
            }

            // MainTrain.CurrentTrains에서 trainDataId로 Train 찾기
            var train = mainTrain.CurrentTrains.FirstOrDefault(t => t.TrainData.Id == trainDataId);
            if (train == null)
            {
                Debug.LogWarning($"Train with ID '{trainDataId}' not found in MainTrain.CurrentTrains");
                return new List<ITrainUpgradeData>();
            }

            // Train의 CurrentLevel 확인
            int currentLevel = train.CurrentLevel;

            // DB의 모든 업그레이드 데이터에서 Level == CurrentLevel 필터링
            var allUpgradeData = GetAllTrainUpgradeData();
            var filteredUpgrades = allUpgradeData
                .Where(upgrade => upgrade.Level == currentLevel)
                .ToList();

            return filteredUpgrades;
        }

        /// <summary>
        /// 모든 기차 업그레이드 데이터를 통합하여 반환 (기본 + 타입별)
        /// </summary>
        public List<ITrainUpgradeData> GetAllTrainUpgradeData()
        {
            var allUpgradeData = new List<ITrainUpgradeData>();
            allUpgradeData.AddRange(trainUpgradeDataList.Cast<ITrainUpgradeData>());
            allUpgradeData.AddRange(turretTrainUpgradeDataList.Cast<ITrainUpgradeData>());
            allUpgradeData.AddRange(rangeTrainUpgradeDataList.Cast<ITrainUpgradeData>());
            return allUpgradeData;
        }

        /// <summary>
        /// ID로 업그레이드 데이터 검색
        /// </summary>
        public UpgradeData GetUpgradeData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Upgrade ID is null or empty");
                return null;
            }

            var result = upgradeDataList.FirstOrDefault(u => u.Id == id);
            if (result == null)
            {
                Debug.LogWarning($"Upgrade data with ID '{id}' not found");
            }
            return result;
        }

        /// <summary>
        /// 모든 기차 데이터를 통합하여 반환 (기본 + 상속 타입들)
        /// </summary>
        public List<TrainData> GetAllTrainData()
        {
            var allTrainData = new List<TrainData>();
            allTrainData.AddRange(trainDataList);
            allTrainData.AddRange(rangeTrainDataList);
            allTrainData.AddRange(turretTrainDataList);
            return allTrainData;
        }
        #endregion
    }
}