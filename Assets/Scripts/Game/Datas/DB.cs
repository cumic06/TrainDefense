using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

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
        /// ID로 몬스터 데이터 검색
        /// </summary>
        public MonsterData GetMonsterData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Monster ID is null or empty");
                return null;
            }

            var result = monsterDataList.FirstOrDefault(m => m.Id == id);
            if (result == null)
            {
                Debug.LogWarning($"Monster data with ID '{id}' not found");
            }
            return result;
        }

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
        /// ID로 터렛 기차 업그레이드 데이터 검색
        /// </summary>
        public TurretTrainUpgradeData GetTurretTrainUpgradeData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Turret Train Upgrade ID is null or empty");
                return null;
            }

            var result = turretTrainUpgradeDataList.FirstOrDefault(u => u.Id == id);
            if (result == null)
            {
                Debug.LogWarning($"Turret train upgrade data with ID '{id}' not found");
            }
            return result;
        }

        /// <summary>
        /// ID로 원거리 기차 업그레이드 데이터 검색
        /// </summary>
        public RangeTrainUpgradeData GetRangeTrainUpgradeData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Range Train Upgrade ID is null or empty");
                return null;
            }

            var result = rangeTrainUpgradeDataList.FirstOrDefault(u => u.Id == id);
            if (result == null)
            {
                Debug.LogWarning($"Range train upgrade data with ID '{id}' not found");
            }
            return result;
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
        /// ID로 스테이지 데이터 검색
        /// </summary>
        public StageData GetStageData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Stage ID is null or empty");
                return null;
            }

            var result = stageDataList.FirstOrDefault(s => s.Id == id);
            if (result == null)
            {
                Debug.LogWarning($"Stage data with ID '{id}' not found");
            }
            return result;
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
        /// 메인 기차 데이터 검색 (모든 타입에서 검색)
        /// </summary>
        public TrainData GetMainTrainData()
        {
            var allTrainData = GetAllTrainData();
            var result = allTrainData.FirstOrDefault(t => t.IsMainTrain);
            if (result == null)
            {
                Debug.LogWarning("Main train data not found. Please ensure at least one train has IsMainTrain set to true.");
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

        /// <summary>
        /// 특정 타입의 기차 데이터 목록 검색
        /// </summary>
        public List<T> GetTrainDataByType<T>() where T : TrainData
        {
            var allTrainData = GetAllTrainData();
            return allTrainData.OfType<T>().ToList();
        }

        /// <summary>
        /// 모든 데이터가 로드되었는지 확인
        /// </summary>
        public bool IsDataLoaded()
        {
            return monsterDataList.Count > 0 || GetAllTrainData().Count > 0 ||
                   GetAllTrainUpgradeData().Count > 0 || stageDataList.Count > 0 ||
                   upgradeDataList.Count > 0;
        }

        /// <summary>
        /// 특정 데이터 타입의 개수 반환
        /// </summary>
        public int GetDataCount<T>() where T : class
        {
            if (typeof(T) == typeof(MonsterData)) return monsterDataList.Count;
            if (typeof(T) == typeof(TrainData)) return GetAllTrainData().Count;
            if (typeof(T) == typeof(RangeTrainData)) return rangeTrainDataList.Count;
            if (typeof(T) == typeof(TurretTrainData)) return turretTrainDataList.Count;
            if (typeof(T) == typeof(TrainUpgradeData)) return trainUpgradeDataList.Count;
            if (typeof(T) == typeof(TurretTrainUpgradeData)) return turretTrainUpgradeDataList.Count;
            if (typeof(T) == typeof(RangeTrainUpgradeData)) return rangeTrainUpgradeDataList.Count;
            if (typeof(T) == typeof(StageData)) return stageDataList.Count;
            if (typeof(T) == typeof(UpgradeData)) return upgradeDataList.Count;
            return 0;
        }

        /// <summary>
        /// TriChoiceDB에서 랜덤 선택지 가져오기
        /// </summary>
        public IChoiceOption GetRandomChoice()
        {
            if (triChoiceDB == null)
            {
                Debug.LogError("TriChoiceDB is not initialized");
                return null;
            }

            return triChoiceDB.RandomChoice();
        }

        #endregion

    }
}