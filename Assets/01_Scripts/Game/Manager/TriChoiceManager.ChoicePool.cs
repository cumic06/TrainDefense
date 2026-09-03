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

            // 엘리트 조건: 업그레이드 7회(만렙) 진행된 기차가 존재하는지 확인
            return mainTrain.CurrentTrains.Any(train =>
            {
                bool isEliteEligible = train != null && train.IsEliteEligible;

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

        // 보상 선택지 후보를 반환한다. 리워크 후 실사용은 긴급 수리 카드뿐(DB rewardChoices에 수리만 등록 — 사용자 결정 2026-07-21).
        // 골드/엘리트 재화 클래스는 dev/merge 팀 코드라 잔존하지만 데이터 미등록으로 비활성.
        private List<ChoiceEntry> _GetRewardChoices()
        {
            var rewardDatas = DatabaseManager.Instance.GetTriChoiceDB().RewardChoices;

            return rewardDatas
                .Where(entry => entry.Option != null && entry.Option.IsValid())
                .ToList();
        }

        // 레벨업 삼중택일용: 스탯 업그레이드(110xxx) 중 만렙이 아닌 것에서 count개를 균등 랜덤으로 뽑는다.
        // 후보가 부족하면(전부 만렙) 남는 슬롯은 보상 선택지(현재 = 긴급 수리)로 채워보고, 없으면 그만큼 슬롯이 준다.
        public List<ChoiceEntry> GetStatUpgradeChoices(int count)
        {
            List<ChoiceEntry> result = new();

            List<ChoiceEntry> candidates = new();

            foreach (var upgradeData in DatabaseManager.Instance.GetUpgradeDatas())
            {
                var choice = new StatUpgradeChoice(upgradeData);

                if (choice.IsValid())
                    candidates.Add(new ChoiceEntry { Option = choice, Weight = 1, Tier = 0 });
            }

            for (int i = 0; i < count; i++)
            {
                if (!_AddChoiceToResult(result, candidates))
                    break;
            }

            if (result.Count < count)
            {
                var rewardChoices = _GetRewardChoices();

                for (int i = result.Count; i < count; i++)
                {
                    if (!_AddChoiceToResult(result, rewardChoices))
                        break;
                }
            }

            return result;
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

                // 반복 선택 가능한 카드(만렙 보상·스탯 업글·포탑 강화)는 "이미 선택됨" 제외를 적용하지 않음
                if (x.Option.IsRepeatable)
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
                // 같은 포탑이라도 "스탯 강화"끼리는 서로 다른 상품(다른 스탯)이라 동시 노출을 허용한다.
                // (같은 스탯 중복은 Id 비교가 이미 거른다 — 이 검사는 강화↔엘리트 승격 같은 이종 충돌만 막는다)
                .Where(entry => !(option is TrainStatUpgradeChoice && entry.Option is TrainStatUpgradeChoice))
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
            else if (option is TrainStatUpgradeChoice trainStatUpgradeChoice)
            {
                // 같은 포탑의 "스탯 강화"와 "엘리트 승격"이 한 화면에 동시에 노출되지 않게 한다.
                if (trainStatUpgradeChoice.TargetTrain != null && trainStatUpgradeChoice.TargetTrain.TrainData != null)
                    _AddReferenceId(refs, trainStatUpgradeChoice.TargetTrain.TrainData.Id);
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
