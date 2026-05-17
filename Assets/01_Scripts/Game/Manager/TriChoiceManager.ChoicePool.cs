using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public partial class TriChoiceManager
    {
        private bool _CanUpgradeToEliteTrain()
        {
            var mainTrain = TrainManager.Instance?.MainTrain;

            if (mainTrain == null)
                return false;

            // 엘리트 조건: 업그레이드 3회 이상 진행된 기차가 존재하는지 확인
            return mainTrain.CurrentTrains.Any(train =>
            {
                bool isEliteEligible = train != null && train.CurrentLevel >= 2;

                return isEliteEligible;
            });
        }

        private List<ChoiceEntry> _GetAddTrainChoices()
        {
            var addDatas = DatabaseManager.Instance.GetTriChoiceDB().TrainChoiceEntries;
            bool hasUpgradedTrain = _CanUpgradeToEliteTrain();

            return addDatas
                .Where(entry =>
                {
                    bool isInvalidEntry = entry.Option is not AddTrainChoice || !entry.Option.IsValid();

                    if (isInvalidEntry)
                        return false;

                    // Tier 0은 항상 포함
                    if (entry.Tier == 0)
                        return true;

                    // Tier 1은 Upgrade 3번 이상 한 Train이 있을 때만 포함
                    if (entry.Tier == 1)
                        return hasUpgradedTrain;

                    return false;
                })
                .ToList();
        }

        private List<ChoiceEntry> _GetEliteTrainChoices()
        {
            var addDatas = DatabaseManager.Instance.GetTriChoiceDB().TrainChoiceEntries;

            return addDatas
                .Where(entry =>
                {
                    bool isValidElite = entry.Option is EliteTrainChoice && entry.Option.IsValid() && entry.Tier == 1;

                    return isValidElite;
                })
                .ToList();
        }

        private List<ChoiceEntry> _GetUpgradeTrainChoices()
        {
            var upgradeDatas = DatabaseManager.Instance.GetTriChoiceDB().UpgradeTrainChoices;

            return upgradeDatas
                .Where(entry => entry.Option != null && entry.Option.IsValid())
                .ToList();
        }

        private bool _SelectByProb(int prob1, int prob2)
        {
            float weight1 = 1f / prob1;
            float weight2 = 1f / prob2;
            float totalWeight = weight1 + weight2;
            float randomValue = Random.Range(0f, totalWeight);

            return randomValue <= weight1;
        }

        private bool _AddChoiceToResult(List<ChoiceEntry> result, List<ChoiceEntry> choices)
        {
            bool isInvalidChoices = choices == null || choices.Count == 0;

            if (isInvalidChoices)
            {
                Debug.LogWarning("AddChoiceToResult: choices is null or empty");

                return false;
            }

            ChoiceEntry randomChoice = _GetRandomChoice(choices, result);

            if (randomChoice == null)
            {
                Debug.LogWarning("AddChoiceToResult: GetRandomChoice returned null (all choices may be duplicates)");

                return false;
            }

            result.Add(randomChoice);

            return true;
        }

        private ChoiceEntry _GetRandomChoice(List<ChoiceEntry> choices, List<ChoiceEntry> excludeResult)
        {
            bool hasNoChoices = choices == null || choices.Count == 0;

            if (hasNoChoices)
                return null;

            var userDataManager = UserDataManager.Instance;

            var filteredChoices = choices.Where(x =>
            {
                if (x?.Option == null)
                    return false;

                bool isDuplicate = excludeResult != null && excludeResult.Any(r => r?.Option?.Id == x.Option.Id);

                if (isDuplicate)
                    return false;

                if (_HasChoiceReferenceConflict(x.Option, excludeResult))
                    return false;

                // UpgradeTrainChoice는 레벨업 후 다시 선택 가능하므로 제외하지 않음
                if (x.Option is UpgradeTrainChoice)
                    return true;

                // Add/EliteTrainChoice는 한 번만 선택 가능하므로 제외
                bool isNewChoice = userDataManager == null || !userDataManager.HasSelectedChoice(x.Option.Id);

                return isNewChoice;
            }).ToList();

            if (filteredChoices.Count == 0)
                return null;

            var positiveWeightChoices = filteredChoices.Where(choice => choice.Weight > 0).ToList();

            if (positiveWeightChoices.Count > 0)
            {
                filteredChoices = positiveWeightChoices;
            }

            float totalWeight = filteredChoices.Sum(choice => choice.Weight);

            if (totalWeight <= 0)
            {
                int randomIndex = Random.Range(0, filteredChoices.Count);

                return filteredChoices[randomIndex];
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

        private bool _HasChoiceReferenceConflict(IChoiceOption option, List<ChoiceEntry> excludeResult)
        {
            bool hasInvalidArgs = option == null || excludeResult == null || excludeResult.Count == 0;

            if (hasInvalidArgs)
                return false;

            var optionRefs = _GetChoiceReferenceIds(option);

            if (optionRefs.Count == 0)
                return false;

            return excludeResult
                .Where(entry => entry?.Option != null)
                .SelectMany(entry => _GetChoiceReferenceIds(entry.Option))
                .Any(optionRefs.Contains);
        }

        private HashSet<string> _GetChoiceReferenceIds(IChoiceOption option)
        {
            var refs = new HashSet<string>();

            if (option is AddTrainChoice addChoice)
            {
                _AddReferenceId(refs, addChoice.TrainDataId);
                _AddReferenceId(refs, addChoice.ReplaceTrainId);
            }
            else if (option is EliteTrainChoice eliteChoice)
            {
                _AddReferenceId(refs, eliteChoice.BaseTrainId);
                _AddReferenceId(refs, eliteChoice.EliteTrainDataId);
            }
            else if (option is UpgradeTrainChoice upgradeChoice)
            {
                _AddReferenceId(refs, upgradeChoice.TargetTrainId);
            }

            return refs;
        }

        private void _AddReferenceId(HashSet<string> refs, string trainId)
        {
            if (!string.IsNullOrEmpty(trainId) && trainId != "0")
                refs.Add(trainId);
        }
    }
}
