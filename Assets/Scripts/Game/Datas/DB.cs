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
        [InfoBox("기차 업그레이드 데이터 관리")]
        [SerializeField]
        public List<TrainUpgradeData> trainUpgradeDataList = new();

        [TabGroup("Stage Data")]
        [InfoBox("스테이지 데이터 관리")]
        [SerializeField]
        public List<StageData> stageDataList = new();

        [TabGroup("Upgrade Data")]
        [InfoBox("일반 업그레이드 데이터 관리")]
        [SerializeField]
        public List<UpgradeData> upgradeDataList = new();

        [TabGroup("Choice Data")]
        [InfoBox("선택지 데이터 관리")]
        [SerializeField]
        public List<ChoiceOption> choiceOptionList = new();

        [TabGroup("TriChoice Database")]
        [InfoBox("3지선다 데이터베이스")]
        [SerializeField]
        [Required("TriChoiceDB는 필수입니다. TriChoiceDB 에셋을 할당해주세요.")]
        private TriChoiceDB triChoiceDB;

        #endregion

        #region Public Properties

        public IReadOnlyList<MonsterData> MonsterDataList => monsterDataList;
        public IReadOnlyList<TrainData> TrainDataList => trainDataList;
        public IReadOnlyList<RangeTrainData> RangeTrainDataList => rangeTrainDataList;
        public IReadOnlyList<TurretTrainData> TurretTrainDataList => turretTrainDataList;
        public IReadOnlyList<TrainUpgradeData> TrainUpgradeDataList => trainUpgradeDataList;
        public IReadOnlyList<StageData> StageDataList => stageDataList;
        public IReadOnlyList<UpgradeData> UpgradeDataList => upgradeDataList;
        public IReadOnlyList<ChoiceOption> ChoiceOptionList => choiceOptionList;
        public TriChoiceDB TriChoiceDB => triChoiceDB;

        /// <summary>
        /// TriChoiceDB가 설정되었는지 확인
        /// </summary>
        public bool HasTriChoiceDB => triChoiceDB != null;

        /// <summary>
        /// TriChoiceDB 검색 (에러 처리 포함)
        /// </summary>
        public TriChoiceDB GetTriChoiceDB()
        {
            if (triChoiceDB == null)
            {
                Debug.LogWarning("TriChoiceDB is not assigned. Please assign a TriChoiceDB asset.");
            }
            return triChoiceDB;
        }

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
        /// ID로 기차 업그레이드 데이터 검색
        /// </summary>
        public TrainUpgradeData GetTrainUpgradeData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Train Upgrade ID is null or empty");
                return null;
            }

            var result = trainUpgradeDataList.FirstOrDefault(u => u.UpgradeName == id);
            if (result == null)
            {
                Debug.LogWarning($"Train upgrade data with ID '{id}' not found");
            }
            return result;
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
        /// ID로 선택지 데이터 검색
        /// </summary>
        public ChoiceOption GetChoiceOption(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("Choice ID is null or empty");
                return null;
            }

            var result = choiceOptionList.FirstOrDefault(c => c.Id == id);
            if (result == null)
            {
                Debug.LogWarning($"Choice option with ID '{id}' not found");
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
                   trainUpgradeDataList.Count > 0 || stageDataList.Count > 0 ||
                   upgradeDataList.Count > 0 || choiceOptionList.Count > 0;
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
            if (typeof(T) == typeof(StageData)) return stageDataList.Count;
            if (typeof(T) == typeof(UpgradeData)) return upgradeDataList.Count;
            if (typeof(T) == typeof(ChoiceOption)) return choiceOptionList.Count;
            return 0;
        }

        #endregion

    }
}