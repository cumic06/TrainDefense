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

                    // 스킬트리 TurretUnlock 노드로 잠긴 포탑은 습득 전까지 선택지에 등장하지 않는다
                    var addChoice = (AddTrainChoice)entry.Option;

                    if (!_IsTrainUnlockedBySkillTree(addChoice.TrainDataId))
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

        // 스킬트리 매니저 부재(부트스트랩 전 등) 시 잠그지 않는다 — 스킬트리 문제로 선택지가 막히는 일 방지
        private bool _IsTrainUnlockedBySkillTree(string trainDataId)
        {
            var skillTreeManager = SkillTreeManager.Instance;

            if (skillTreeManager == null)
                return true;

            return skillTreeManager.IsTrainUnlocked(trainDataId);
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

        // 직전 GetChoices에서 화면에 나온 엘리트 선택지 Id. 리롤 시 직전에 떴던 엘리트를
        // 한 번 건너뛰어(ABAB) 같은 엘리트가 연속으로 뜨지 않게 한다.
        private readonly HashSet<string> _lastEliteChoiceIds = new();

        // 직전에 떴던 엘리트를 제외하고 셔플한 엘리트 후보를 반환한다.
        // 제외 후 후보가 비면(엘리트가 1종뿐인 경우 등) 엘리트가 아예 안 뜨는 걸 막기 위해
        // 원본 목록을 그대로 사용한다.
        private List<ChoiceEntry> _GetEliteTrainChoicesExcludingLast()
        {
            var eliteChoices = _GetEliteTrainChoices();

            if (_lastEliteChoiceIds.Count > 0)
            {
                var filtered = eliteChoices
                    .Where(entry => entry?.Option != null && !_lastEliteChoiceIds.Contains(entry.Option.Id))
                    .ToList();

                if (filtered.Count > 0)
                    eliteChoices = filtered;
            }

            // 후보가 여럿일 때 항상 같은 엘리트만 뜨지 않도록 셔플
            for (int i = eliteChoices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (eliteChoices[i], eliteChoices[j]) = (eliteChoices[j], eliteChoices[i]);
            }

            return eliteChoices;
        }

        // 이번 화면에 실제로 포함된 엘리트 선택지를 기억해 다음 리롤에서 제외한다.
        private void _RememberEliteChoices(List<ChoiceEntry> result)
        {
            _lastEliteChoiceIds.Clear();

            foreach (var entry in result)
            {
                if (entry?.Option is EliteTrainChoice elite)
                    _lastEliteChoiceIds.Add(elite.Id);
            }
        }

        private List<ChoiceEntry> _GetUpgradeTrainChoices()
        {
            var upgradeDatas = DatabaseManager.Instance.GetTriChoiceDB().UpgradeTrainChoices;

            return upgradeDatas
                .Where(entry => entry.Option != null && entry.Option.IsValid())
                .ToList();
        }

        // 만렙 보상 선택지(골드/엘리트 재화/긴급 수리) 후보를 반환한다.
        // 보유 기차가 전부 만렙이라 정상 선택지로 슬롯을 채우지 못할 때 빈 슬롯을 채우는 데 쓰인다.
        private List<ChoiceEntry> _GetRewardChoices()
        {
            var rewardDatas = DatabaseManager.Instance.GetTriChoiceDB().RewardChoices;

            return rewardDatas
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

                // 만렙 보상 선택지(골드/엘리트 재화/긴급 수리)는 만렙 후 매 레벨업마다 반복 획득 가능하므로 제외하지 않음
                if (x.Option is RewardChoiceBase)
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
