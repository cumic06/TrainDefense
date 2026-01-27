using System.Linq;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;
using System.Collections.Generic;

namespace TrainDefense.Game
{
    public class DatabaseManager : Singleton<DatabaseManager>
    {
        private DB _db;

        protected override void Awake()
        {
            base.Awake();

            _db = Resources.Load<DB>("Data/DB");
        }

        public DB GetDB()
        {
            if (_db == null)
            {
                Debug.LogError("DB is null");
                _db = Resources.Load<DB>("Data/DB");
            }

            return _db;
        }

        public StageData[] GetStageDatas() => GetDB().StageDataList.ToArray();

        public MapData GetMapData(StageData stageData) => stageData.MapData;

        public MonsterData[] GetMonsterDatas() => GetDB().MonsterDataList.ToArray();

        public MonsterData GetMonsterData(string id)
        {
            return GetMonsterDatas().FirstOrDefault(m => m.Id == id);
        }

        public UpgradeData[] GetUpgradeDatas() => GetDB().UpgradeDataList.ToArray();
        #region TriChoiceDB

        public TriChoiceDB GetTriChoiceDB() => GetDB().TriChoiceDB;
        public IChoiceOption[] GetAddTrainChoices() => GetDB().TriChoiceDB.AddTrainChoices.Select(x => x.Option).ToArray();
        public IChoiceOption[] GetUpgradeTrainChoices() => GetDB().TriChoiceDB.UpgradeTrainChoices.Select(x => x.Option).ToArray();
        #endregion

        public TrainData[] GetTrainDatas() => GetDB().TrainDataList.ToArray();

        public TurretTrainData[] GetTurretTrainDatas() => GetDB().TurretTrainDataList.ToArray();

        public RangeTrainData[] GetRangeTrainDatas() => GetDB().RangeTrainDataList.ToArray();

        public TrainUpgradeData[] GetTrainUpgradeDatas() => GetDB().TrainUpgradeDataList.ToArray();

        public TurretTrainUpgradeData[] GetTurretTrainUpgradeDatas() => GetDB().TurretTrainUpgradeDataList.ToArray();

        public RangeTrainUpgradeData[] GetRangeTrainUpgradeDatas() => GetDB().RangeTrainUpgradeDataList.ToArray();


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
        /// 1. TriChoiceDB에서 ID로 UpgradeTrainChoice를 찾으면, MainTrain.CurrentTrains에 있는 Train의 레벨과 같은 업그레이드 데이터를 반환
        /// 2. 그렇지 않으면 업그레이드 데이터 ID로 직접 검색하여 반환
        /// </summary>
        public ITrainUpgradeData GetTrainUpgradeData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Train Upgrade ID is null or empty");
                return null;
            }

            // 먼저 TriChoiceDB에서 UpgradeTrainChoice를 찾아봄
            var choiceEntry = GetTriChoiceDB().UpgradeTrainChoices.FirstOrDefault(x => x.Option.Id == id);
            if (choiceEntry != null)
            {
                var upgradeChoice = choiceEntry.Option as UpgradeTrainChoice;
                if (upgradeChoice != null)
                {
                    // TrainManager를 통해 MainTrain 접근
                    if (TrainManager.Instance == null)
                    {
                        Debug.LogWarning("TrainManager.Instance is null");
                        return null;
                    }

                    var mainTrain = TrainManager.Instance.MainTrain;
                    if (mainTrain == null)
                    {
                        Debug.LogWarning("MainTrain is null");
                        return null;
                    }

                    // MainTrain.CurrentTrains에서 targetTrainId로 Train 찾기
                    var train = mainTrain.CurrentTrains.FirstOrDefault(t => t.TrainData.Id == upgradeChoice.TargetTrainId);
                    if (train == null)
                    {
                        Debug.LogWarning($"Train with ID '{upgradeChoice.TargetTrainId}' not found in MainTrain.CurrentTrains");
                        return null;
                    }

                    // Train의 CurrentLevel 확인
                    int currentLevel = train.CurrentLevel;

                    // weightedUpgrades에서 현재 레벨에 해당하는 업그레이드 데이터 찾기
                    if (upgradeChoice.WeightedUpgrades == null || upgradeChoice.WeightedUpgrades.Length == 0)
                    {
                        Debug.LogWarning($"UpgradeTrainChoice [{id}]: WeightedUpgrades is null or empty");
                        return null;
                    }

                    // 현재 레벨에 해당하는 업그레이드 데이터 찾기
                    // upgradeStats 배열의 인덱스 = 레벨 (CurrentLevel은 0-based)
                    foreach (var weightedUpgrade in upgradeChoice.WeightedUpgrades)
                    {
                        var upgradeData = GetTrainUpgradeDataById(weightedUpgrade.UpgradeDataId);
                        if (upgradeData != null && currentLevel >= 0 && currentLevel < upgradeData.MaxLevel)
                        {
                            return upgradeData;
                        }
                    }

                    Debug.LogWarning($"Train upgrade data with ID '{id}' and level '{currentLevel}' not found");
                    return null;
                }
            }

            // TriChoiceDB에서 찾지 못했으면 업그레이드 데이터 ID로 직접 검색
            return GetTrainUpgradeDataById(id);
        }

        /// <summary>
        /// UpgradeDataId로 업그레이드 데이터를 검색
        /// </summary>
        public ITrainUpgradeData GetTrainUpgradeDataById(string upgradeDataId)
        {
            if (string.IsNullOrEmpty(upgradeDataId))
            {
                return null;
            }

            var allUpgradeData = GetAllTrainUpgradeData();
            return allUpgradeData.FirstOrDefault(u => u.Id == upgradeDataId);
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

            // DB의 모든 업그레이드 데이터에서 현재 레벨에 해당하는 업그레이드 필터링
            // upgradeStats 배열의 인덱스 = 레벨 (CurrentLevel은 0-based)
            var allUpgradeData = GetAllTrainUpgradeData();
            var filteredUpgrades = allUpgradeData
                .Where(upgrade => currentLevel >= 0 && currentLevel < upgrade.MaxLevel)
                .ToList();

            return filteredUpgrades;
        }

        /// <summary>
        /// 모든 기차 업그레이드 데이터를 통합하여 반환 (기본 + 타입별)
        /// </summary>
        public List<ITrainUpgradeData> GetAllTrainUpgradeData()
        {
            var allUpgradeData = new List<ITrainUpgradeData>();
            allUpgradeData.AddRange(GetTrainUpgradeDatas().Cast<ITrainUpgradeData>());
            allUpgradeData.AddRange(GetTurretTrainUpgradeDatas().Cast<ITrainUpgradeData>());
            allUpgradeData.AddRange(GetRangeTrainUpgradeDatas().Cast<ITrainUpgradeData>());
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

            var result = GetUpgradeDatas().FirstOrDefault(u => u.Id == id);
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
            allTrainData.AddRange(GetTrainDatas());
            allTrainData.AddRange(GetRangeTrainDatas());
            allTrainData.AddRange(GetTurretTrainDatas());
            return allTrainData;
        }
        #endregion
    }
}