using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    /// <summary>
    /// 삼중택일 선택지 관리 매니저
    /// </summary>
    public class TriChoiceManager : Singleton<TriChoiceManager>
    {
        [SerializeField]
        private int upgradeProb = 2; // UpgradeTrain 선택 확률 분모 (예: 2 = 1/2 확률)
        [SerializeField]
        private int addProb = 2; // AddTrain 선택 확률 분모 (예: 2 = 1/2 확률)

        // 선택된 업그레이드를 런타임 상태로 관리
        private Dictionary<string, ITrainUpgradeData> _selectedUpgrades = new();

        /// <summary>
        /// 삼중택일 선택지를 반환합니다.
        /// </summary>
        /// <param name="count">요청하는 선택지 개수</param>
        /// <returns>선택된 선택지 목록</returns>
        public List<ChoiceEntry> GetChoices(int count)
        {
            List<ChoiceEntry> result = new();

            if (TrainManager.Instance == null)
            {
                Debug.LogError("TrainManager is null");
                return result;
            }

            int trainCount = TrainManager.Instance.GetTrainCount();

            // 2. Train 미보유 (trainCount == 0): 무조건 AddTrainChoice만
            if (trainCount == 0)
            {
                var addChoices = GetAddTrainChoices();
                for (int i = 0; i < count; i++)
                {
                    AddChoiceToResult(result, addChoices);
                }
                return result;
            }

            // 3. EliteTrain 보장: CanUpgradeToEliteTrain() true면 삼중택일 중 하나는 무조건 EliteTrain Choice
            if (CanUpgradeToEliteTrain())
            {
                var eliteChoices = GetEliteTrainChoices();
                if (eliteChoices.Count > 0)
                {
                    AddChoiceToResult(result, eliteChoices);
                }
            }

            // 4. 나머지 슬롯 채우기
            for (int i = result.Count; i < count; i++)
            {
                // MaxTrainCount 도달: UpgradeChoice만
                if (TrainManager.Instance.IsMaxTrainCountReached())
                {
                    var upgradeChoices = GetUpgradeTrainChoices();
                    AddChoiceToResult(result, upgradeChoices);
                }
                else
                {
                    // 가중치로 Add/Upgrade 선택
                    var addChoices = GetAddTrainChoices();
                    var upgradeChoices = GetUpgradeTrainChoices();

                    bool hasAddChoices = addChoices.Count > 0;
                    bool hasUpgradeChoices = upgradeChoices.Count > 0;

                    if (hasAddChoices && hasUpgradeChoices)
                    {
                        // 가중치로 선택
                        bool selectUpgrade = SelectByProb(upgradeProb, addProb);
                        if (selectUpgrade)
                        {
                            // 실패하면 다른 타입으로 재시도
                            if (!AddChoiceToResult(result, upgradeChoices))
                            {
                                AddChoiceToResult(result, addChoices);
                            }
                        }
                        else
                        {
                            // 실패하면 다른 타입으로 재시도
                            if (!AddChoiceToResult(result, addChoices))
                            {
                                AddChoiceToResult(result, upgradeChoices);
                            }
                        }
                    }
                    else if (hasUpgradeChoices)
                    {
                        AddChoiceToResult(result, upgradeChoices);
                    }
                    else if (hasAddChoices)
                    {
                        AddChoiceToResult(result, addChoices);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 확률에 따라 첫 번째 선택지를 선택합니다.
        /// </summary>
        /// <param name="prob1">첫 번째 선택지 확률 분모</param>
        /// <param name="prob2">두 번째 선택지 확률 분모</param>
        /// <returns>true면 첫 번째 선택지, false면 두 번째 선택지</returns>
        private bool SelectByProb(int prob1, int prob2)
        {
            float weight1 = 1f / prob1;
            float weight2 = 1f / prob2;
            float totalWeight = weight1 + weight2;
            float randomValue = Random.Range(0f, totalWeight);
            return randomValue <= weight1;
        }

        /// <summary>
        /// 선택지를 결과 목록에 추가합니다. ID 기반 중복 체크 포함.
        /// </summary>
        /// <returns>추가 성공 여부</returns>
        private bool AddChoiceToResult(List<ChoiceEntry> result, List<ChoiceEntry> choices)
        {
            if (choices == null || choices.Count == 0)
            {
                Debug.LogWarning("AddChoiceToResult: choices is null or empty");
                return false;
            }

            ChoiceEntry randomChoice = GetRandomChoice(choices, result);
            if (randomChoice == null)
            {
                Debug.LogWarning("AddChoiceToResult: GetRandomChoice returned null (all choices may be duplicates)");
                return false;
            }

            result.Add(randomChoice);
            return true;
        }

        /// <summary>
        /// 랜덤으로 선택지를 반환합니다. 동일한 ID를 가진 선택지는 제외합니다.
        /// </summary>
        /// <param name="choices">선택 가능한 선택지 목록</param>
        /// <param name="excludeResult">제외할 선택지 목록 (이미 선택된 결과)</param>
        private ChoiceEntry GetRandomChoice(List<ChoiceEntry> choices, List<ChoiceEntry> excludeResult)
        {
            if (choices == null || choices.Count == 0)
            {
                return null;
            }

            var userDataManager = UserDataManager.Instance;

            // 1. 이미 결과에 포함된 ID 제외
            // 2. UserDataManager에 있는 선택지 제외 (UpgradeTrainChoice는 제외하지 않음)
            var filteredChoices = choices.Where(x =>
            {
                if (x?.Option == null) return false;

                // 이미 결과에 동일한 ID가 있으면 제외
                if (excludeResult != null && excludeResult.Any(r => r?.Option?.Id == x.Option.Id))
                    return false;

                // UpgradeTrainChoice는 레벨업 후 다시 선택 가능하므로 제외하지 않음
                if (x.Option is UpgradeTrainChoice)
                    return true;

                // AddTrainChoice는 한 번만 선택 가능하므로 제외
                return !userDataManager.GetSelectedChoiceIds().Contains(x.Option.Id);
            }).ToList();

            if (filteredChoices.Count == 0)
            {
                return null;
            }

            float totalWeight = filteredChoices.Sum(choice => choice.Weight);
            if (totalWeight <= 0)
            {
                return filteredChoices[0];
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            foreach (var choice in filteredChoices)
            {
                currentWeight += choice.Weight;
                if (randomValue <= currentWeight)
                {
                    return choice;
                }
            }
            return filteredChoices[^1];
        }

        private List<ChoiceEntry> GetAddTrainChoices()//AddTrainChoice 목록을 반환한다.
        {
            var addDatas = DatabaseManager.Instance.GetTriChoiceDB().AddTrainChoices;
            var userDataManager = UserDataManager.Instance;

            // 엘리트 트레인으로 업그레이드가 가능한지 확인
            bool hasUpgradedTrain = CanUpgradeToEliteTrain();

            // 아직 획득하지 않은 train에 대한 choice만 필터링
            return addDatas
                .Where(entry =>
                {
                    if (entry.Option == null || !entry.Option.IsValid())
                        return false;

                    // Tier 0은 항상 포함
                    if (entry.Tier == 0)
                        return true;

                    // Tier 1은 Upgrade 3번 이상 한 Train이 있을 때만 포함
                    if (entry.Tier == 1)
                        return hasUpgradedTrain;

                    // 기타 Tier는 제외 (확장성을 위해)
                    return false;
                })
                .ToList();
        }

        /// <summary>
        /// Tier 1 (엘리트 트레인) 선택지만 반환합니다.
        /// </summary>
        private List<ChoiceEntry> GetEliteTrainChoices()
        {
            var addDatas = DatabaseManager.Instance.GetTriChoiceDB().AddTrainChoices;

            return addDatas
                .Where(entry => entry.Option != null && entry.Option.IsValid() && entry.Tier == 1)
                .ToList();
        }

        /// <summary>
        /// 엘리트 트레인으로 업그레이드가 가능한지 확인합니다.
        /// 업그레이드 선택 횟수 기록 대신 실제 기차 레벨을 기준으로 판단합니다.
        /// (기본 레벨 -1에서 3번 업그레이드 시 CurrentLevel >= 2)
        /// </summary>
        private bool CanUpgradeToEliteTrain()
        {
            var mainTrain = TrainManager.Instance?.MainTrain;
            if (mainTrain == null) return false;

            // 엘리트 조건: 업그레이드 3회 이상 진행된 기차가 존재하는지 확인
            return mainTrain.CurrentTrains.Any(train => train != null && train.CurrentLevel >= 2);
        }

        private List<ChoiceEntry> GetUpgradeTrainChoices()//UpgradeTrainChoice 목록을 반환한다.
        {
            var upgradeDatas = DatabaseManager.Instance.GetTriChoiceDB().UpgradeTrainChoices;
            // 획득한 train에 대한 upgrade choice만 필터링
            return upgradeDatas
                .Where(entry => entry.Option != null && entry.Option.IsValid())
                .ToList();
        }

        /// <summary>
        /// UpgradeTrainChoice에 대한 선택된 업그레이드 데이터를 반환합니다.
        /// 선택되지 않은 경우 랜덤으로 선택하여 저장합니다.
        /// 저장된 업그레이드가 현재 Train 레벨에 맞지 않으면 재선택합니다.
        /// </summary>
        public ITrainUpgradeData GetSelectedUpgrade(UpgradeTrainChoice choice)
        {
            if (choice == null) return null;

            // 저장된 업그레이드가 있는 경우, 현재 Train 레벨에 맞는지 확인
            if (_selectedUpgrades.TryGetValue(choice.Id, out var selectedUpgrade))
            {
                if (IsUpgradeValidForCurrentLevel(choice, selectedUpgrade))
                {
                    return selectedUpgrade;
                }
                else
                {
                    // 저장된 업그레이드가 현재 레벨에 맞지 않으면 캐시 제거
                    _selectedUpgrades.Remove(choice.Id);
                }
            }

            // 새로 선택
            selectedUpgrade = SelectRandomUpgrade(choice);
            if (selectedUpgrade != null)
            {
                _selectedUpgrades[choice.Id] = selectedUpgrade;
            }

            return selectedUpgrade;
        }

        /// <summary>
        /// 저장된 업그레이드가 현재 Train 레벨에 유효한지 확인합니다.
        /// </summary>
        private bool IsUpgradeValidForCurrentLevel(UpgradeTrainChoice choice, ITrainUpgradeData upgradeData)
        {
            if (choice == null || upgradeData == null) return false;

            var train = GetTargetTrain(choice.TargetTrainId);
            if (train == null) return false;

            int currentLevel = train.CurrentLevel;
            // Train 초기 레벨은 -1, upgradeStats 배열은 0부터 시작
            // View 표시 및 업그레이드 적용 시: 레벨 + 1 인덱스 사용
            int currentLevelIndex = currentLevel + 1;

            // Train의 현재 레벨 인덱스가 업그레이드 데이터의 최대 레벨보다 크거나 같으면 유효하지 않음
            if (currentLevelIndex >= upgradeData.MaxLevel) return false;

            // 현재 레벨 인덱스에 해당하는 업그레이드 데이터인지 확인
            return currentLevelIndex >= 0 && currentLevelIndex < upgradeData.MaxLevel;
        }

        /// <summary>
        /// UpgradeTrainChoice에서 랜덤으로 업그레이드를 선택합니다.
        /// Train의 현재 레벨에 맞는 다음 레벨의 업그레이드를 선택합니다.
        /// </summary>
        private ITrainUpgradeData SelectRandomUpgrade(UpgradeTrainChoice choice)
        {
            if (choice == null) return null;
            if (choice.WeightedUpgrades == null || choice.WeightedUpgrades.Length == 0) return null;

            var train = GetTargetTrain(choice.TargetTrainId);
            if (train == null) return null;

            int currentLevel = train.CurrentLevel;
            // Train 초기 레벨은 -1, upgradeStats 배열은 0부터 시작
            // View 표시 및 업그레이드 적용 시: 레벨 + 1 인덱스 사용
            // 레벨 -1이면 인덱스 0, 레벨 0이면 인덱스 1
            int currentLevelIndex = currentLevel + 1;

            // 현재 Train의 레벨에 맞는 업그레이드만 필터링
            var validUpgrades = choice.WeightedUpgrades
                .Select(w => new { Weight = w, UpgradeData = DatabaseManager.Instance.GetTrainUpgradeDataById(w?.UpgradeDataId) })
                .Where(x =>
                {
                    if (x.UpgradeData == null) return false;

                    // Train의 현재 레벨 인덱스가 업그레이드 데이터의 최대 레벨보다 크거나 같으면 제외
                    if (currentLevelIndex >= x.UpgradeData.MaxLevel) return false;

                    // 현재 레벨 인덱스에 해당하는 업그레이드 데이터가 있는지 확인
                    return currentLevelIndex >= 0 && currentLevelIndex < x.UpgradeData.MaxLevel;
                })
                .ToList();

            if (validUpgrades.Count == 0)
            {
                Debug.LogWarning($"UpgradeTrainChoice [{choice.Id}]: No upgrade data found for Train [{choice.TargetTrainId}] at level [{currentLevel}] (Index: {currentLevelIndex})");
                return null;
            }

            if (validUpgrades.Count == 1)
            {
                return validUpgrades[0].UpgradeData;
            }

            float total = validUpgrades.Sum(x => x.Weight.UpgradeDataWeight);
            if (total <= 0)
            {
                return validUpgrades[0].UpgradeData;
            }

            float r = Random.Range(0f, total);
            float acc = 0f;
            foreach (var item in validUpgrades)
            {
                acc += item.Weight.UpgradeDataWeight;
                if (r <= acc)
                {
                    return item.UpgradeData;
                }
            }

            return validUpgrades[^1].UpgradeData;
        }

        private Train GetTargetTrain(string targetTrainId)
        {
            var trainManager = TrainManager.Instance;
            if (trainManager == null || trainManager.MainTrain == null) return null;

            return trainManager.MainTrain.CurrentTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);
        }
    }
}