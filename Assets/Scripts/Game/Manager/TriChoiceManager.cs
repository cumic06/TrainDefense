using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;
using Random = UnityEngine.Random;

namespace TrainDefense.Game
{
    /// <summary>
    /// 삼중택일 선택지 관리 매니저
    /// </summary>
    public class TriChoiceManager : Singleton<TriChoiceManager>
    {
        /// <summary>
        /// 사용 가능한 선택지 목록을 중복 없이 반환합니다.
        /// 요청한 개수만큼 반환하되, 사용 가능한 선택지가 부족하면 가능한 만큼만 반환합니다.
        /// </summary>
        public List<IChoiceOption> GetAvailableChoices(int count)
        {
            var db = DataBaseManager.Instance?.GetDB();
            if (db == null || db.TriChoiceDB == null)
            {
                Debug.LogError("DB or TriChoiceDB not found");
                return new List<IChoiceOption>();
            }

            List<IChoiceOption> availableChoices = new();

            // UserDataManager를 통해 선택된 AddTrainChoice가 있는지 확인
            HashSet<string> addedTrainIds = GetAddedTrainIdsFromUserData();

            // UpgradeTrainChoice 선택 (addedTrainIds 개수만큼)
            if (addedTrainIds.Count > 0)
            {
                List<ChoiceEntry> validUpgradeChoices = GetValidUpgradeChoicesForAddedTrains(db.TriChoiceDB, addedTrainIds);
                
                if (validUpgradeChoices.Count > 0)
                {
                    List<ChoiceEntry> remainingUpgradeChoices = new(validUpgradeChoices);
                    int upgradeCount = Mathf.Min(addedTrainIds.Count, count, validUpgradeChoices.Count);

                    for (int i = 0; i < upgradeCount && remainingUpgradeChoices.Count > 0; i++)
                    {
                        IChoiceOption selectedOption = SelectFromChoices(remainingUpgradeChoices);

                        if (selectedOption == null)
                        {
                            break;
                        }

                        // 선택지 초기화
                        selectedOption.Initialize(db);

                        // 선택된 선택지를 결과에 추가
                        availableChoices.Add(selectedOption);

                        // 선택된 선택지를 풀에서 제거하여 중복 방지
                        remainingUpgradeChoices.RemoveAll(entry => entry.Option?.Id == selectedOption.Id);
                    }
                }
            }

            // 나머지 개수만큼 AddTrainChoice 선택
            int remainingCount = count - availableChoices.Count;
            if (remainingCount > 0)
            {
                List<ChoiceEntry> validAddTrainChoices = GetValidChoices(db.TriChoiceDB.AddTrainChoices);

                if (validAddTrainChoices.Count > 0)
                {
                    List<ChoiceEntry> remainingAddTrainChoices = new(validAddTrainChoices);
                    int addTrainCount = Mathf.Min(remainingCount, validAddTrainChoices.Count);

                    for (int i = 0; i < addTrainCount && remainingAddTrainChoices.Count > 0; i++)
                    {
                        IChoiceOption selectedOption = SelectFromChoices(remainingAddTrainChoices);

                        if (selectedOption == null)
                        {
                            break;
                        }

                        // 선택지 초기화
                        selectedOption.Initialize(db);

                        // 선택된 선택지를 결과에 추가
                        availableChoices.Add(selectedOption);

                        // 선택된 선택지를 풀에서 제거하여 중복 방지
                        remainingAddTrainChoices.RemoveAll(entry => entry.Option?.Id == selectedOption.Id);
                    }
                }
            }

            if (availableChoices.Count == 0)
            {
                Debug.LogWarning("No valid choices available");
            }

            return availableChoices;
        }

        /// <summary>
        /// UserDataManager를 통해 선택된 AddTrainChoice의 TrainId 목록을 가져옵니다.
        /// </summary>
        private HashSet<string> GetAddedTrainIdsFromUserData()
        {
            HashSet<string> addedTrainIds = new();

            var userDataManager = UserDataManager.Instance;
            if (userDataManager == null) return addedTrainIds;

            var db = DataBaseManager.Instance?.GetDB();
            if (db == null) return addedTrainIds;

            // UserDataManager에서 선택된 모든 ChoiceOption ID를 가져옴
            var selectedChoiceIds = userDataManager.GetSelectedChoiceIds();

            // 선택된 ChoiceOption 중 AddTrainChoice인 것들의 trainDataId를 수집
            foreach (var choiceId in selectedChoiceIds)
            {
                var choiceEntry = db.TriChoiceDB.AddTrainChoices
                    .FirstOrDefault(entry => entry.Option?.Id == choiceId);

                if (choiceEntry?.Option is AddTrainChoice addTrainChoice)
                {
                    // AddTrainChoice의 trainDataId를 가져옴
                    var trainDataId = GetTrainDataIdFromAddTrainChoice(addTrainChoice);
                    if (!string.IsNullOrEmpty(trainDataId))
                    {
                        addedTrainIds.Add(trainDataId);
                    }
                }
            }

            return addedTrainIds;
        }

        /// <summary>
        /// AddTrainChoice에서 trainDataId를 가져옵니다.
        /// </summary>
        private string GetTrainDataIdFromAddTrainChoice(AddTrainChoice addTrainChoice)
        {
            return addTrainChoice?.TrainDataId;
        }

        /// <summary>
        /// 유효한 선택지 목록을 반환합니다.
        /// </summary>
        private List<ChoiceEntry> GetValidChoices(IReadOnlyList<ChoiceEntry> entries)
        {
            List<ChoiceEntry> validChoices = new();

            foreach (var entry in entries)
            {
                if (entry.Option != null && entry.Option.IsValid())
                {
                    validChoices.Add(entry);
                }
            }

            return validChoices;
        }

        /// <summary>
        /// 추가된 Train들에 대한 유효한 UpgradeTrainChoice 목록을 반환합니다.
        /// </summary>
        private List<ChoiceEntry> GetValidUpgradeChoicesForAddedTrains(TriChoiceDB triChoiceDB, HashSet<string> addedTrainIds)
        {
            List<ChoiceEntry> validChoices = new();

            foreach (var entry in triChoiceDB.UpgradeTrainChoices)
            {
                if (entry.Option is UpgradeTrainChoice upgradeChoice)
                {
                    // UpgradeTrainChoice의 targetTrainId가 추가된 Train 목록에 있는지 확인
                    if (addedTrainIds.Contains(upgradeChoice.TargetTrainId) && entry.Option.IsValid())
                    {
                        validChoices.Add(entry);
                    }
                }
            }

            return validChoices;
        }

        /// <summary>
        /// 가중치 기반으로 선택지를 선택합니다.
        /// 가중치가 0인 항목들도 최소 가중치 1을 부여하여 선택될 수 있도록 합니다.
        /// </summary>
        private IChoiceOption SelectFromChoices(List<ChoiceEntry> choices)
        {
            if (choices.Count == 0) return null;
            if (choices.Count == 1) return choices[0].Option;

            int totalWeight = 0;
            bool hasZeroWeight = false;
            
            foreach (var entry in choices)
            {
                totalWeight += entry.Weight;
                if (entry.Weight == 0)
                {
                    hasZeroWeight = true;
                }
            }

            // 가중치가 모두 0이거나 합이 0 이하면 균등 확률로 선택 (가중치 0인 항목도 포함)
            if (totalWeight <= 0)
            {
                return choices[Random.Range(0, choices.Count)].Option;
            }

            // 가중치 0인 항목이 있으면, 가중치 0인 항목들도 최소 가중치 1을 부여
            if (hasZeroWeight)
            {
                // 가중치 0인 항목들도 선택될 수 있도록 최소 가중치 적용
                int adjustedTotalWeight = totalWeight;
                foreach (var entry in choices)
                {
                    if (entry.Weight == 0)
                    {
                        adjustedTotalWeight += 1; // 가중치 0인 항목도 최소 1의 가중치 부여
                    }
                }

                int randomValue = Random.Range(0, adjustedTotalWeight);
                int currentWeight = 0;

                foreach (var entry in choices)
                {
                    int weight = entry.Weight == 0 ? 1 : entry.Weight;
                    currentWeight += weight;

                    if (randomValue < currentWeight)
                    {
                        return entry.Option;
                    }
                }
            }
            else
            {
                // 가중치 기반 랜덤 선택
                int randomValue = Random.Range(0, totalWeight);
                int currentWeight = 0;

                foreach (var entry in choices)
                {
                    currentWeight += entry.Weight;

                    if (randomValue < currentWeight)
                    {
                        return entry.Option;
                    }
                }
            }

            return choices[^1].Option;
        }
    }
}

