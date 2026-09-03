using System.Collections.Generic;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public partial class TriChoiceManager : Singleton<TriChoiceManager>
    {
        #region Variables
        private Dictionary<string, (IData skillData, TrainChoiceSkillType skillType)> _selectedEliteSkills = new();
        private Dictionary<string, (IData skillData, TrainChoiceSkillType skillType)> _selectedAddSkills = new();
        #endregion

        // 게임 진입 첫 포탑 무료 선택 전용 — AddTrainChoice만 뽑는다.
        // (레벨업 카드 = GetStatUpgradeChoices, 역 상점 = GetShopChoices)
        public List<ChoiceEntry> GetChoices(int count)
        {
            List<ChoiceEntry> result = new();

            if (TrainManager.Instance == null)
            {
                Debug.LogError("TrainManager is null");

                return result;
            }

            var addChoices = _GetAddTrainChoices();

            for (int i = 0; i < count; i++)
            {
                _AddChoiceToResult(result, addChoices);
            }

            return result;
        }

        public void ClearSelectedChoiceData()
        {
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
            else if (choiceOption is RewardChoiceBase rewardChoice)
            {
                return new ChoiceUIInfo
                {
                    Icon = rewardChoice.Icon,
                    Name = rewardChoice.Name,
                    Description = rewardChoice.Description
                };
            }
            else if (choiceOption is StatUpgradeChoice statUpgradeChoice)
            {
                var upgradeData = statUpgradeChoice.UpgradeData;

                if (upgradeData == null)
                    return null;

                int currentLevel = UserDataManager.Instance != null
                    ? UserDataManager.Instance.GetUpgradeLevel(upgradeData.Id)
                    : 0;

                return new ChoiceUIInfo
                {
                    Icon = upgradeData.Icon,
                    Name = _SafeFormat(upgradeData.Name, upgradeData.GetTotalValueAtLevel(currentLevel)),
                    Description = _BuildStatUpgradeDescription(upgradeData, currentLevel)
                };
            }
            else if (choiceOption is TrainStatUpgradeChoice trainStatUpgradeChoice)
            {
                var trainData = trainStatUpgradeChoice.TargetTrain != null
                    ? trainStatUpgradeChoice.TargetTrain.TrainData
                    : null;

                if (trainData == null)
                    return null;

                return new ChoiceUIInfo
                {
                    Icon = trainData.Icon,
                    Name = trainData.Name,
                    Description = trainStatUpgradeChoice.BuildStatLineText()
                };
            }

            return null;
        }

        // 스탯 업그레이드 카드 설명. 포맷 {1}=레벨당 증가량(+표기), {2}=현재 누적값, {3}=최대 누적값.
        // {0}은 옛 표기(다음 누적 총값)의 잔여 자리 — 31개 언어 로컬라이즈 템플릿의 인덱스 호환을 위해 빈칸으로 채운다.
        // ★ {2}/{3}(현재/최대) 표기는 2026-07-21 사용자 지시로 템플릿에서 제거됨 — 인자는 템플릿 복구 대비 계속 전달한다.
        // 증가/감소 방향은 설명 문구가 표현하므로 값은 크기(양수)만 표시한다. (예: 공속 -1 → "+1")
        private static string _BuildStatUpgradeDescription(UpgradeData upgradeData, int currentLevel)
        {
            float perLevelAmount = Mathf.Abs(upgradeData.GetPerLevelValue());
            string increaseAmountText = perLevelAmount != 0 ? $"+{perLevelAmount}" : "";

            float currentValue = Mathf.Abs(upgradeData.GetTotalValueAtLevel(currentLevel));
            float maxValue = Mathf.Abs(upgradeData.GetTotalValueAtLevel(upgradeData.MaxUpgradeCount));

            if (currentLevel >= upgradeData.MaxUpgradeCount)
                return _SafeFormat(upgradeData.Description, string.Empty, increaseAmountText, maxValue, maxValue);

            return _SafeFormat(upgradeData.Description, string.Empty, increaseAmountText, currentValue, maxValue);
        }

        // 로컬라이즈 템플릿의 placeholder 개수/형식이 어긋나도 카드 생성이 죽지 않도록 하는 안전 포맷.
        // (FormatException이 카드 활성화 흐름까지 전파되면 일시정지 미해제 소프트락 위험)
        private static string _SafeFormat(string format, params object[] formatArguments)
        {
            if (string.IsNullOrEmpty(format))
                return format;

            try
            {
                return string.Format(format, formatArguments);
            }
            catch (System.FormatException)
            {
                return System.Text.RegularExpressions.Regex.Replace(format, @"\{[0-9]+\}", "-");
            }
        }

        private void _ApplySkillToInfo(ChoiceUIInfo info, IData skillData, TrainChoiceSkillType skillType)
        {
            if (skillData == null)
                return;

            if (skillType == TrainChoiceSkillType.Passive && skillData is TrainPassiveSkillData passiveSkill)
            {
                if (!string.IsNullOrEmpty(passiveSkill.Name))
                    info.PassiveName = passiveSkill.Name;

                if (!string.IsNullOrEmpty(passiveSkill.Description))
                    info.PassiveDescription = passiveSkill.Description;
            }
        }
    }
}
