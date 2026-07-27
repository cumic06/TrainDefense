using System.Linq;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
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

            if (_db != null)
            {
                var allTrains = GetAllTrainData();
                _db.TrainSkillDataDB.BuildIndex(allTrains);
            }
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

        public IReadOnlyList<PermanentUpgradeData> GetPermanentUpgradeDatas() => GetDB().PermanentUpgradeDataList;
        public PermanentUpgradeData GetPermanentUpgradeData(string id) => GetDB().PermanentUpgradeDataList.FirstOrDefault(u => u != null && u.Id == id);

        public IReadOnlyList<SkillNodeData> GetSkillNodeDatas() => GetDB().SkillNodeDataList;
        public SkillNodeData GetSkillNodeData(string id) => GetDB().SkillNodeDataList.FirstOrDefault(n => n != null && n.Id == id);

        public EliteData GetEliteData() => GetDB().EliteData;

        public ScoreData GetScoreData() => GetDB().ScoreData;
        #region TriChoiceDB

        public TriChoiceDB GetTriChoiceDB() => GetDB().TriChoiceDB;
        public IChoiceOption[] GetAddTrainChoices() => GetDB().TriChoiceDB.TrainChoiceEntries.Select(x => x.Option).OfType<AddTrainChoice>().Cast<IChoiceOption>().ToArray();
        public IChoiceOption[] GetEliteTrainChoices() => GetDB().TriChoiceDB.TrainChoiceEntries.Select(x => x.Option).OfType<EliteTrainChoice>().Cast<IChoiceOption>().ToArray();
        public IChoiceOption[] GetUpgradeTrainChoices() => GetDB().TriChoiceDB.UpgradeTrainChoices.Select(x => x.Option).ToArray();

        public IReadOnlyList<StatUpgradeTierData> GetStatUpgradeTiers() => GetDB().StatUpgradeTierDataList;

        // 이 스탯이 나오기 시작하는 등급. 설정이 없으면 1등급부터.
        public int GetStatMinGrade(StatType statType)
        {
            var setting = GetDB().StatUpgradeStatDataList.FirstOrDefault(s => s != null && s.StatType == statType);

            return setting != null ? setting.MinGrade : 1;
        }

        // 이 포탑이 상점에서 강화할 수 있는 스탯 규칙들. 목록에 없는 스탯은 카드로 뜨지 않는다.
        // 엘리트(31xxx·41xxx)는 시트에 행을 두지 않고 base 포탑(30xxx·40xxx) 규칙을 그대로 쓴다.
        public IEnumerable<TrainStatUpgradeRuleData> GetTrainStatUpgradeRules(string trainDataId)
        {
            var rules = GetDB().TrainStatUpgradeRuleDataList.Where(rule => rule != null && rule.TrainDataId == trainDataId);

            if (rules.Any())
                return rules;

            string baseTrainDataId = GetBaseTrainDataId(trainDataId);

            if (baseTrainDataId == trainDataId)
                return rules;

            return GetDB().TrainStatUpgradeRuleDataList.Where(rule => rule != null && rule.TrainDataId == baseTrainDataId);
        }

        // 엘리트 ID → base ID (31001 → 30001, 41002 → 40002). 규격이 다르면 원본을 그대로 돌려준다.
        private static string GetBaseTrainDataId(string trainDataId)
        {
            if (string.IsNullOrEmpty(trainDataId) || trainDataId.Length < 2 || trainDataId[1] != '1')
                return trainDataId;

            return trainDataId[0] + "0" + trainDataId.Substring(2);
        }
        #endregion

        public TrainData[] GetTrainDatas() => GetDB().TrainDataList.ToArray();

        public TurretTrainData[] GetTurretTrainDatas() => GetDB().TurretTrainDataList.ToArray();

        public RangeTrainData[] GetRangeTrainDatas() => GetDB().RangeTrainDataList.ToArray();
        public TrainSkillDataDB GetTrainSkillDataDB() => GetDB().TrainSkillDataDB;
        public TrainSkillData[] GetTrainSkillDatas() => GetDB().TrainSkillDataDB.TrainActiveSkillDataList.ToArray();
        public TrainSkillData GetTrainSkillData(string id) => string.IsNullOrEmpty(id) ? null : GetDB().TrainSkillDataDB.trainActiveSkillDataList.Find(s => s != null && s.Id == id);


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
