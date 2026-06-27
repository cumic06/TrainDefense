using System.Collections.Generic;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public partial class TriChoiceManager : Singleton<TriChoiceManager>
    {
        #region Fields
        [SerializeField]
        private int upgradeProb = 2;
        [SerializeField]
        private int addProb = 2;
        #endregion

        #region Variables
        private Dictionary<string, ITrainUpgradeData> _selectedUpgrades = new();
        private Dictionary<string, (IData skillData, TrainChoiceSkillType skillType)> _selectedEliteSkills = new();
        private Dictionary<string, (IData skillData, TrainChoiceSkillType skillType)> _selectedAddSkills = new();
        #endregion

        public List<ChoiceEntry> GetChoices(int count)
        {
            List<ChoiceEntry> result = new();

            if (TrainManager.Instance == null)
            {
                Debug.LogError("TrainManager is null");

                return result;
            }

            int trainCount = TrainManager.Instance.GetTrainCount();

            // Train 미보유: 무조건 AddTrainChoice만
            if (trainCount == 0)
            {
                var addChoices = _GetAddTrainChoices();

                for (int i = 0; i < count; i++)
                {
                    _AddChoiceToResult(result, addChoices);
                }

                return result;
            }

            // EliteTrain: 가능한 EliteTrain Choice를 모두 추가 (count 초과 방지)
            // 직전 리롤에서 떴던 엘리트는 제외해 같은 엘리트가 연속으로 뜨지 않게 한다.
            if (_CanUpgradeToEliteTrain())
            {
                var eliteChoices = _GetEliteTrainChoicesExcludingLast();

                foreach (var eliteChoice in eliteChoices)
                {
                    if (result.Count >= count)
                        break;

                    if (!_HasChoiceReferenceConflict(eliteChoice.Option, result))
                        result.Add(eliteChoice);
                }
            }

            // 나머지 슬롯 채우기
            for (int i = result.Count; i < count; i++)
            {
                // MaxTrainCount 도달: UpgradeChoice만
                if (TrainManager.Instance.IsMaxTrainCountReached())
                {
                    var upgradeChoices = _GetUpgradeTrainChoices();
                    _AddChoiceToResult(result, upgradeChoices);
                }
                else
                {
                    var addChoices = _GetAddTrainChoices();
                    var upgradeChoices = _GetUpgradeTrainChoices();

                    bool hasAddChoices = addChoices.Count > 0;
                    bool hasUpgradeChoices = upgradeChoices.Count > 0;

                    if (hasAddChoices && hasUpgradeChoices)
                    {
                        bool selectUpgrade = _SelectByProb(upgradeProb, addProb);

                        if (selectUpgrade)
                        {
                            if (!_AddChoiceToResult(result, upgradeChoices))
                            {
                                _AddChoiceToResult(result, addChoices);
                            }
                        }
                        else
                        {
                            if (!_AddChoiceToResult(result, addChoices))
                            {
                                _AddChoiceToResult(result, upgradeChoices);
                            }
                        }
                    }
                    else if (hasUpgradeChoices)
                    {
                        _AddChoiceToResult(result, upgradeChoices);
                    }
                    else if (hasAddChoices)
                    {
                        _AddChoiceToResult(result, addChoices);
                    }
                }
            }

            // 정상 선택지(추가/강화/엘리트)로 모든 슬롯을 채우지 못했다면(보유 기차가 전부 만렙 등)
            // 부족분을 만렙 보상 선택지(골드/엘리트 재화/긴급 수리)로 채운다.
            if (result.Count < count)
            {
                var rewardChoices = _GetRewardChoices();

                for (int i = result.Count; i < count; i++)
                {
                    if (!_AddChoiceToResult(result, rewardChoices))
                        break; // 더 채울 보상 선택지가 없으면 중단
                }
            }

            // 엘리트 트레인이 항상 0번 슬롯에 고정되지 않도록 셔플
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            // 이번 화면의 엘리트를 기억해 다음 리롤에서 직전 엘리트를 제외한다.
            _RememberEliteChoices(result);

            return result;
        }

        public void ClearSelectedChoiceData()
        {
            _selectedUpgrades.Clear();
            _selectedEliteSkills.Clear();
            _selectedAddSkills.Clear();
        }

        public void ClearSelectedUpgrades() => ClearSelectedChoiceData();

        public ChoiceUIInfo GetChoiceUIInfo(IChoiceOption choiceOption)
        {
            if (choiceOption is AddTrainChoice addTrainChoice)
            {
                var trainData = DatabaseManager.Instance.GetTrainData(addTrainChoice.TrainDataId);

                if (trainData == null)
                    return null;

                var info = new ChoiceUIInfo
                {
                    Icon = trainData.Icon,
                    Name = trainData.Name,
                    Description = trainData.Description
                };

                var (skillData, skillType) = GetSelectedAddSkill(addTrainChoice);
                _ApplySkillToInfo(info, skillData, skillType);

                return info;
            }
            else if (choiceOption is EliteTrainChoice eliteTrainChoice)
            {
                var trainData = DatabaseManager.Instance.GetTrainData(eliteTrainChoice.EliteTrainDataId);

                if (trainData == null)
                    return null;

                var info = new ChoiceUIInfo
                {
                    Icon = trainData.Icon,
                    Name = trainData.Name,
                    Description = trainData.Description
                };

                var (skillData, skillType) = GetSelectedEliteSkill(eliteTrainChoice);
                _ApplySkillToInfo(info, skillData, skillType);

                return info;
            }
            else if (choiceOption is UpgradeTrainChoice upgradeTrainChoice)
            {
                var upgradeData = GetSelectedUpgrade(upgradeTrainChoice);

                if (upgradeData == null)
                    return null;

                return new ChoiceUIInfo
                {
                    Icon = upgradeData.Icon,
                    Name = upgradeData.Name,
                    Description = upgradeData.Description
                };
            }
            else if (choiceOption is RewardChoiceBase rewardChoice)
            {
                return new ChoiceUIInfo
                {
                    Icon = rewardChoice.Icon,
                    Name = rewardChoice.Name,
                    Description = rewardChoice.Description
                };
            }

            return null;
        }

        private void _ApplySkillToInfo(ChoiceUIInfo info, IData skillData, TrainChoiceSkillType skillType)
        {
            if (skillData == null)
                return;

            if (skillType == TrainChoiceSkillType.Active && skillData is TrainSkillData activeSkill)
            {
                if (!string.IsNullOrEmpty(activeSkill.Name))
                    info.ActiveSkillName = activeSkill.Name;

                if (!string.IsNullOrEmpty(activeSkill.Description))
                    info.ActiveSkillDescription = activeSkill.Description;
            }
            else if (skillType == TrainChoiceSkillType.Passive && skillData is TrainPassiveSkillData passiveSkill)
            {
                if (!string.IsNullOrEmpty(passiveSkill.Name))
                    info.PassiveName = passiveSkill.Name;

                if (!string.IsNullOrEmpty(passiveSkill.Description))
                    info.PassiveDescription = passiveSkill.Description;
            }
        }
    }
}
