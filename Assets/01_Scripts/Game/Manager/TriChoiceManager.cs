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
        /// 삼중택일 선택지를 반환합니다.
        /// </summary>
        /// <param name="count">요청하는 선택지 개수</param>
        /// <returns>선택된 선택지 목록</returns>
        public List<IChoiceOption> GetChoices(int count) //TODO: 전체 로직 수정.
        {
            var db = GetDB();
            if (db == null || db.TriChoiceDB == null)
            {
                Debug.LogError("DB or TriChoiceDB not found");
                return new List<IChoiceOption>();
            }

            List<IChoiceOption> result = new();

            // 1. TrainManager에서 IsMaxTrainCountReached면 UpgradeData만 가져온다.
            if (TrainManager.Instance.IsMaxTrainCountReached())
            {
                List<ChoiceEntry> validUpgradeChoices = GetValidUpgradeChoicesForTrains(db.TriChoiceDB);

                // 모든 Train이 최대 업그레이드면 빈 리스트 반환
                if (validUpgradeChoices.Count == 0)
                {
                    return result;
                }

                // 가중치 기반으로 count개만큼 선택
                List<ChoiceEntry> remainingChoices = new(validUpgradeChoices);
                int selectCount = Mathf.Min(count, remainingChoices.Count);

                for (int i = 0; i < selectCount && remainingChoices.Count > 0; i++)
                {
                    IChoiceOption selectedOption = SelectFromChoices(remainingChoices);
                    if (selectedOption == null)
                    {
                        break;
                    }

                    selectedOption.Initialize(db);
                    result.Add(selectedOption);
                    remainingChoices.RemoveAll(entry => entry.Option?.Id == selectedOption.Id);
                }
            }
            else
            {
                // 2-2. IsMaxTrainCountReached가 아니면 AddTrainData과 UpgradeData를 가져와 이건 랜덤으로 정해지는거야.
                CalculateChoiceRatio(count, out int addTrainCount, out int upgradeCount);

                Debug.Log($"addTrainCount: {addTrainCount}, upgradeCount: {upgradeCount}");

                // AddTrain 선택지 가져오기
                if (addTrainCount > 0)
                {
                    List<ChoiceEntry> validAddTrainChoices = GetValidAddTrainChoices(db.TriChoiceDB);
                    List<ChoiceEntry> remainingAddChoices = new(validAddTrainChoices);
                    int selectAddCount = Mathf.Min(addTrainCount, remainingAddChoices.Count);

                    for (int i = 0; i < selectAddCount && remainingAddChoices.Count > 0; i++)
                    {
                        IChoiceOption selectedOption = SelectFromChoices(remainingAddChoices);
                        if (selectedOption == null)
                        {
                            break;
                        }

                        selectedOption.Initialize(db);
                        result.Add(selectedOption);
                        remainingAddChoices.RemoveAll(entry => entry.Option?.Id == selectedOption.Id);
                    }
                }

                // Upgrade 선택지 가져오기
                if (upgradeCount > 0)
                {
                    List<ChoiceEntry> validUpgradeChoices = GetValidUpgradeChoicesForTrains(db.TriChoiceDB);
                    List<ChoiceEntry> remainingUpgradeChoices = new(validUpgradeChoices);
                    int selectUpgradeCount = Mathf.Min(upgradeCount, remainingUpgradeChoices.Count);

                    for (int i = 0; i < selectUpgradeCount && remainingUpgradeChoices.Count > 0; i++)
                    {
                        IChoiceOption selectedOption = SelectFromChoices(remainingUpgradeChoices);
                        if (selectedOption == null)
                        {
                            break;
                        }

                        selectedOption.Initialize(db);
                        result.Add(selectedOption);
                        remainingUpgradeChoices.RemoveAll(entry => entry.Option?.Id == selectedOption.Id);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 선택지 비율을 계산합니다. 현재 Train 개수와 업그레이드 가능 여부를 기준으로 AddTrain과 Upgrade의 개수를 결정합니다.
        /// Train이 없거나 모든 Train이 최대 레벨이면 Upgrade 선택지를 제외하고 모두 AddTrain으로 배분합니다.
        /// </summary>
        /// <param name="totalCount">전체 선택지 개수</param>
        /// <param name="addTrainCount">AddTrain 선택지 개수</param>
        /// <param name="upgradeCount">Upgrade 선택지 개수</param>
        private void CalculateChoiceRatio(int totalCount, out int addTrainCount, out int upgradeCount)
        {
            var trainManager = TrainManager.Instance;
            var mainTrain = trainManager?.MainTrain;
            
            // Train이 없으면 Upgrade 선택지 제외
            if (mainTrain == null || mainTrain.CurrentTrainCount == 0)
            {
                addTrainCount = totalCount;
                upgradeCount = 0;
                return;
            }

            // 업그레이드 가능한 Train 개수 확인
            int upgradeableTrainCount = 0;
            int maxLevelTrainCount = 0;

            foreach (var train in mainTrain.CurrentTrains)
            {
                int trainMaxLevel = GetTrainMaxLevel(train);
                
                if (train.CurrentLevel >= trainMaxLevel)
                {
                    maxLevelTrainCount++;
                }
                else
                {
                    upgradeableTrainCount++;
                }
            }

            // 모든 Train이 최대 레벨이면 Upgrade 선택지 제외
            if (upgradeableTrainCount == 0)
            {
                addTrainCount = totalCount;
                upgradeCount = 0;
                return;
            }

            // 업그레이드 가능한 Train의 비율에 따라 선택지 배분
            float upgradeRatio = (float)upgradeableTrainCount / mainTrain.CurrentTrainCount;
            
            // 업그레이드 가능한 Train이 50% 이상이면 균등 분할
            if (upgradeRatio >= 0.5f)
            {
                if (totalCount % 2 == 0)
                {
                    addTrainCount = totalCount / 2;
                    upgradeCount = totalCount / 2;
                }
                else
                {
                    addTrainCount = (totalCount / 2) + 1;
                    upgradeCount = totalCount / 2;
                }
            }
            else
            {
                // 업그레이드 가능한 Train이 50% 미만이면 AddTrain 비중 증가
                upgradeCount = Mathf.Max(1, Mathf.FloorToInt(totalCount * upgradeRatio));
                addTrainCount = totalCount - upgradeCount;
            }
        }

        /// <summary>
        /// 현재 Train들에 대한 유효한 UpgradeTrainChoice 목록을 반환합니다.
        /// Train이 MaxLevel에 도달한 경우 제외하고, 레벨에 맞는 UpgradeData만 포함합니다.
        /// </summary>
        private List<ChoiceEntry> GetValidUpgradeChoicesForTrains(TriChoiceDB triChoiceDB)
        {
            List<ChoiceEntry> validChoices = new();
            var db = GetDB();
            if (db == null) return validChoices;

            var trainManager = TrainManager.Instance;
            if (trainManager == null || trainManager.MainTrain == null) return validChoices;

            foreach (var entry in triChoiceDB.UpgradeTrainChoices)
            {
                if (entry.Option is UpgradeTrainChoice upgradeChoice)
                {
                    // MainTrain.CurrentTrains에서 targetTrainId와 일치하는 Train 찾기
                    var targetTrain = trainManager.MainTrain.CurrentTrains
                        .FirstOrDefault(train => train.TrainData.Id == upgradeChoice.TargetTrainId);

                    if (targetTrain == null) continue;

                    // Train의 최대 레벨 계산
                    int trainMaxLevel = GetTrainMaxLevel(targetTrain);

                    // Train이 MaxLevel에 도달했으면 제외
                    if (targetTrain.CurrentLevel >= trainMaxLevel) continue;

                    // 레벨에 맞는 UpgradeData가 있는지 확인
                    bool hasValidUpgrade = false;
                    if (upgradeChoice.WeightedUpgrades != null && upgradeChoice.WeightedUpgrades.Length > 0)
                    {
                        foreach (var weightedUpgrade in upgradeChoice.WeightedUpgrades)
                        {
                            var upgradeData = db.GetTrainUpgradeData(weightedUpgrade?.UpgradeDataId);
                            if (upgradeData == null) continue;

                            // Train의 현재 레벨이 업그레이드 데이터의 최대 레벨보다 작아야 함
                            if (targetTrain.CurrentLevel >= upgradeData.MaxLevel) continue;

                            // 현재 레벨에 해당하는 업그레이드 데이터인지 확인
                            if (upgradeData.Level == targetTrain.CurrentLevel)
                            {
                                hasValidUpgrade = true;
                                break;
                            }
                        }
                    }

                    // 유효한 업그레이드가 있고 IsValid()도 통과하면 선택지에 추가
                    if (hasValidUpgrade && entry.Option.IsValid())
                    {
                        validChoices.Add(entry);
                    }
                }
            }

            return validChoices;
        }

        /// <summary>
        /// 유효한 AddTrainChoice 목록을 반환합니다.
        /// </summary>
        private List<ChoiceEntry> GetValidAddTrainChoices(TriChoiceDB triChoiceDB)
        {
            List<ChoiceEntry> validChoices = new();

            foreach (var entry in triChoiceDB.AddTrainChoices)
            {
                if (entry.Option != null && entry.Option.IsValid())
                {
                    validChoices.Add(entry);
                }
            }

            return validChoices;
        }

        /// <summary>
        /// Train의 최대 레벨을 반환합니다. TriChoiceDB의 UpgradeTrainChoice에서 targetTrainId로 찾아서 WeightedUpgradeData의 UpgradeDataId로 업그레이드 데이터를 조회하고 MaxLevel의 최대값을 반환합니다.
        /// </summary>
        private int GetTrainMaxLevel(Train train)
        {
            var db = GetDB();
            if (db == null || db.TriChoiceDB == null) return 0;

            string trainDataId = train.TrainData.Id;
            var maxLevels = new List<int>();

            // TriChoiceDB.UpgradeTrainChoices에서 targetTrainId == trainDataId인 항목 찾기
            foreach (var entry in db.TriChoiceDB.UpgradeTrainChoices)
            {
                if (entry.Option is UpgradeTrainChoice upgradeChoice && upgradeChoice.TargetTrainId == trainDataId)
                {
                    // 해당 UpgradeTrainChoice의 WeightedUpgradeData 배열 순회
                    if (upgradeChoice.WeightedUpgrades != null)
                    {
                        foreach (var weightedUpgrade in upgradeChoice.WeightedUpgrades)
                        {
                            // 각 UpgradeDataId로 db.GetTrainUpgradeData() 호출
                            var upgradeData = db.GetTrainUpgradeData(weightedUpgrade?.UpgradeDataId);
                            if (upgradeData != null)
                            {
                                // 모든 업그레이드 데이터의 MaxLevel 수집
                                maxLevels.Add(upgradeData.MaxLevel);
                            }
                        }
                    }
                }
            }

            // MaxLevel 중 최대값 반환
            return maxLevels.Count > 0 ? maxLevels.Max() : 0;
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

        private DB GetDB()
        {
            return DataBaseManager.Instance?.GetDB();
        }
    }
}