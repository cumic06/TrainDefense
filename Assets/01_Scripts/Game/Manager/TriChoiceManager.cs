using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;
using System.Text;

namespace TrainDefense.Game
{
    /// <summary>
    /// 삼중택일 선택지 관리 매니저
    /// </summary>
    public class TriChoiceManager : Singleton<TriChoiceManager>
    {
        [SerializeField] private int upgradeProb = 2; // UpgradeTrain 선택 확률 분모 (예: 2 = 1/2 확률)
        [SerializeField] private int addProb = 2; // AddTrain 선택 확률 분모 (예: 2 = 1/2 확률)

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

            StringBuilder sb = new();

            for (int i = 0; i < count; i++)
            {
                if (TrainManager.Instance.IsMaxTrainCountReached())//무조건 UpgradeTrainChoice를 반환한다.
                {
                    result = GetUpgradeTrainChoices();
                }
                else //AddTrainChoice와 UpgradeTrainChoice 중 랜덤으로 반환한다.
                {
                    int trainCount = TrainManager.Instance.GetTrainCount();
                    int maxTrainCount = TrainManager.Instance.GetMaxTrainCount();
                    int maxUpgradeTrainCount = TrainManager.Instance.GetMaxUpgradeTrainCount();

                    var addChoices = GetAddTrainChoices().Where(x => !result.Contains(x)).ToList();
                    var upgradeChoices = GetUpgradeTrainChoices().Where(x => !result.Contains(x)).ToList();

                    // 조건 분리
                    bool canUpgrade = trainCount <= maxTrainCount && maxUpgradeTrainCount != trainCount && upgradeChoices.Count > 0;
                    bool canAdd = trainCount > 0 && trainCount < maxTrainCount && addChoices.Count > 0;
                    bool needFirst = trainCount <= 0 && addChoices.Count > 0;

                    // 두 조건이 모두 만족될 때 확률적으로 선택
                    if (canUpgrade && canAdd)
                    {
                        bool selectedUpgrade = SelectByProb(upgradeProb, addProb);
                        if (selectedUpgrade)
                        {
                            sb.AppendLine("UpgradeTrain (Probability)");
                            AddChoiceToResult(result, upgradeChoices);
                        }
                        else
                        {
                            sb.AppendLine("AddTrain (Probability)");
                            AddChoiceToResult(result, addChoices);
                        }
                    }
                    // 업그레이드만 가능
                    else if (canUpgrade)
                    {
                        sb.AppendLine("UpgradeTrain");
                        AddChoiceToResult(result, upgradeChoices);
                    }
                    // 기차 추가만 가능
                    else if (canAdd)
                    {
                        sb.AppendLine("AddTrain");
                        AddChoiceToResult(result, addChoices);
                    }
                    // 첫 기차 추가 필요
                    else if (needFirst)
                    {
                        sb.AppendLine("AddFirstTrain");
                        AddChoiceToResult(result, addChoices);
                    }
                }
            }

            Debug.Log($"ResultCount: {result.Count}\n {sb}");

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
        /// 선택지를 결과 목록에 추가합니다. 중복 체크 포함.
        /// </summary>
        private void AddChoiceToResult(List<ChoiceEntry> result, List<ChoiceEntry> choices)
        {
            if (choices == null || choices.Count == 0)
            {
                Debug.LogWarning("AddChoiceToResult: choices is null or empty");
                return;
            }

            ChoiceEntry randomChoice = GetRandomChoices(choices);
            if (randomChoice == null)
            {
                Debug.LogWarning("AddChoiceToResult: GetRandomChoices returned null");
                return;
            }

            int tryCount = 0;
            while (result.Contains(randomChoice))
            {
                randomChoice = GetRandomChoices(choices);
                if (randomChoice == null)
                {
                    Debug.LogWarning("AddChoiceToResult: GetRandomChoices returned null during retry");
                    return;
                }
                tryCount++;
                if (tryCount > 100)
                {
                    break;
                }
            }
            result.Add(randomChoice);
        }

        private ChoiceEntry GetRandomChoices(List<ChoiceEntry> choices)//랜덤으로 선택지를 반환한다.
        {
            if (choices == null || choices.Count == 0)
            {
                return null;
            }

            //이미 UserDataManager에 있는 선택지면 제외
            var userDataManager = UserDataManager.Instance;
            choices = choices.Where(x => !userDataManager.GetSelectedChoiceIds().Contains(x.Option.Id)).ToList();
            
            if (choices.Count == 0)
            {
                return null;
            }

            float totalWeight = choices.Sum(choice => choice.Weight);
            if (totalWeight <= 0)
            {
                return choices[0];
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            foreach (var choice in choices)
            {
                currentWeight += choice.Weight;
                if (randomValue <= currentWeight)
                {
                    return choice;
                }
            }
            return choices[^1];
        }

        private List<ChoiceEntry> GetAddTrainChoices()//AddTrainChoice 목록을 반환한다.
        {
            var addDatas = DatabaseManager.Instance.GetTriChoiceDB().AddTrainChoices;
            return addDatas.ToList();
        }

        private List<ChoiceEntry> GetUpgradeTrainChoices()//UpgradeTrainChoice 목록을 반환한다.
        {
            var upgradeDatas = DatabaseManager.Instance.GetTriChoiceDB().UpgradeTrainChoices;
            return upgradeDatas.ToList();
        }

        /// <summary>
        /// UpgradeTrainChoice에 대한 선택된 업그레이드 데이터를 반환합니다.
        /// 선택되지 않은 경우 랜덤으로 선택하여 저장합니다.
        /// </summary>
        public ITrainUpgradeData GetSelectedUpgrade(UpgradeTrainChoice choice)
        {
            if (choice == null) return null;

            if (!_selectedUpgrades.TryGetValue(choice.Id, out var selectedUpgrade))
            {
                selectedUpgrade = SelectRandomUpgrade(choice);
                if (selectedUpgrade != null)
                {
                    _selectedUpgrades[choice.Id] = selectedUpgrade;
                }
            }

            return selectedUpgrade;
        }

        /// <summary>
        /// UpgradeTrainChoice에서 랜덤으로 업그레이드를 선택합니다.
        /// </summary>
        private ITrainUpgradeData SelectRandomUpgrade(UpgradeTrainChoice choice)
        {
            if (choice == null) return null;
            if (choice.WeightedUpgrades == null || choice.WeightedUpgrades.Length == 0) return null;

            var train = GetTargetTrain(choice.TargetTrainId);
            if (train == null) return null;

            int currentLevel = train.CurrentLevel;

            // 현재 Train의 레벨에 맞는 업그레이드만 필터링
            var validUpgrades = choice.WeightedUpgrades
                .Select(w => new { Weight = w, UpgradeData = DatabaseManager.Instance.GetTrainUpgradeDataById(w?.UpgradeDataId) })
                .Where(x =>
                {
                    if (x.UpgradeData == null) return false;

                    // Train의 현재 레벨이 업그레이드 데이터의 최대 레벨보다 크거나 같으면 제외
                    if (currentLevel >= x.UpgradeData.MaxLevel) return false;

                    // 현재 레벨에 해당하는 업그레이드 데이터인지 확인
                    // upgradeStats 배열의 인덱스 = 레벨 (CurrentLevel은 0-based)
                    return currentLevel >= 0 && currentLevel < x.UpgradeData.MaxLevel;
                })
                .ToList();

            foreach (var item in validUpgrades)
            {
                Debug.Log($"UpgradeTrainChoice [{choice.Id}]: {item.UpgradeData.Id} CurrentLevel: {currentLevel} MaxLevel: {item.UpgradeData.MaxLevel}");
            }

            if (validUpgrades.Count == 0)
            {
                Debug.LogWarning($"UpgradeTrainChoice [{choice.Id}]: No upgrade data found for Train [{choice.TargetTrainId}] at level [{currentLevel}]");
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

        /// <summary>
        /// 선택된 업그레이드를 제거합니다 (선택지가 변경될 때 호출)
        /// </summary>
        public void ClearSelectedUpgrade(string choiceId)
        {
            _selectedUpgrades.Remove(choiceId);
        }

        /// <summary>
        /// 모든 선택된 업그레이드를 제거합니다.
        /// </summary>
        public void ClearAllSelectedUpgrades()
        {
            _selectedUpgrades.Clear();
        }

        private Train GetTargetTrain(string targetTrainId)
        {
            var trainManager = TrainManager.Instance;
            if (trainManager == null || trainManager.MainTrain == null) return null;

            return trainManager.MainTrain.CurrentTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);
        }
    }
}