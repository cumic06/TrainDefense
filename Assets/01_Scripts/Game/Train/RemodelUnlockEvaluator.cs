using System.Collections.Generic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    /// <summary>
    /// 판이 끝날 때 편성 포탑의 전투 기록·스탯으로 개조 해금 조건(RemodelUnlockCondition)을 판정한다.
    /// 해금 목록의 저장은 UserDataManager가 맡는다 — 여기서는 "이번 판에 조건을 만족한 개조"만 돌려준다.
    /// </summary>
    public static class RemodelUnlockEvaluator
    {
        /// <summary>
        /// 조건을 전부 만족한 개조 스킬 ID 집합. 포탑은 원본이든 이미 개조했든 같은 포탑으로 본다
        /// (개조 시 전투 기록이 승계되므로 개조한 포탑으로 쌓은 기록도 그 포탑의 다른 개조를 연다).
        /// 스탯 조건도 개조한 포탑의 현재 스탯으로 본다 — 개조 패시브가 올린 만큼 쉬워지는 것은 의도(이미 등급 합 10을 투자한 포탑).
        /// </summary>
        public static HashSet<string> FindSatisfiedRemodels(IReadOnlyList<Train> trains)
        {
            var result = new HashSet<string>();

            if (trains == null) return result;

            foreach (var train in trains)
            {
                foreach (var remodel in GetRemodelsFor(train))
                {
                    if (remodel.IsUnlockedByDefault) continue;

                    if (_IsSatisfied(remodel, train))
                        result.Add(remodel.Id);
                }
            }

            return result;
        }

        /// <summary>이 포탑(원본이든 이미 개조했든)이 가질 수 있는 개조 스킬 전부. 포탑 상세창의 해금 진행도도 이 목록을 쓴다.</summary>
        public static List<TrainPassiveSkillData> GetRemodelsFor(Train train)
        {
            var result = new List<TrainPassiveSkillData>();
            var databaseManager = DatabaseManager.Instance;

            if (databaseManager == null || train == null || train.TrainData == null) return result;

            var skillDataDB = databaseManager.GetTrainSkillDataDB();
            // 포탑 ID(원본·엘리트 모두) → 그 포탑이 될 수 있는 엘리트 포탑 ID
            var eliteIdsByTrainId = new Dictionary<string, List<string>>();

            foreach (var option in databaseManager.GetEliteTrainChoices())
            {
                if (option is not EliteTrainChoice eliteChoice) continue;

                _AddEliteId(eliteIdsByTrainId, eliteChoice.BaseTrainId, eliteChoice.EliteTrainDataId);
                _AddEliteId(eliteIdsByTrainId, eliteChoice.EliteTrainDataId, eliteChoice.EliteTrainDataId);
            }

            if (!eliteIdsByTrainId.TryGetValue(train.TrainData.Id, out var eliteIds)) return result;

            foreach (string eliteId in eliteIds)
            {
                foreach (var (skillData, _) in skillDataDB.GetSkillsForTrain(eliteId))
                {
                    if (skillData is TrainPassiveSkillData remodel && !result.Contains(remodel))
                        result.Add(remodel);
                }
            }

            return result;
        }

        private static void _AddEliteId(Dictionary<string, List<string>> eliteIdsByTrainId, string trainId, string eliteId)
        {
            if (string.IsNullOrEmpty(trainId) || string.IsNullOrEmpty(eliteId)) return;

            if (!eliteIdsByTrainId.TryGetValue(trainId, out var eliteIds))
            {
                eliteIds = new List<string>();
                eliteIdsByTrainId[trainId] = eliteIds;
            }

            if (!eliteIds.Contains(eliteId))
                eliteIds.Add(eliteId);
        }

        private static bool _IsSatisfied(TrainPassiveSkillData remodel, Train train)
        {
            foreach (var condition in remodel.UnlockConditions)
            {
                if (!condition.IsSatisfiedBy(train)) return false;
            }

            return true;
        }
    }
}
