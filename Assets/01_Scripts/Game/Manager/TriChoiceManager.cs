using System.Collections.Generic;
using System.Linq;
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

                // 승격 = 패시브 추가 + 외형 변경이라 플레이어에게는 "개조"로 보인다 (예: 박격포 포탑 개조). 엘리트도 이름은 원본과 같다.
                var info = new ChoiceUIInfo
                {
                    Icon = trainData.Icon,
                    Name = _SafeFormat(TrainDefense.Localize.LocalizeHelper.GetByKey("UI_Shop_TurretRemodel_Name", "{0} 개조"), trainData.Name),
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

                // 상점 아이템은 문장 템플릿에 효과 값을 채운다(_BuildShopItemDescription). 레벨 표기가 없어 레벨을 안 읽는다.
                if (upgradeData.UpgradePool == UpgradePool.ShopItem)
                {
                    return new ChoiceUIInfo
                    {
                        Icon = upgradeData.Icon,
                        Name = upgradeData.Name,
                        Description = _BuildShopItemDescription(upgradeData),
                        Grade = upgradeData.Grade
                    };
                }

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

                // 새 포탑 구매 카드와 구분되도록 제목에 "강화"를 붙인다 (예: 기관총 포탑 강화)
                return new ChoiceUIInfo
                {
                    Icon = trainData.Icon,
                    Name = _SafeFormat(TrainDefense.Localize.LocalizeHelper.GetByKey("UI_Shop_TurretUpgrade_Name", "{0} 강화"), trainData.Name),
                    Description = trainStatUpgradeChoice.BuildStatLineText(),
                    Grade = trainStatUpgradeChoice.Grade
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

        // 상점 아이템 설명 — 아이템마다 문장 템플릿(Upgrade_1100xx_Desc, "모든 포탑의 공격력이 {0}% 증가하고 사거리가 {1}% 감소합니다").
        // {n}은 stats 배열 n번째의 크기(부호 없음). 공격 간격은 값이 줄어야 빨라지므로 문장은 "공격 속도"로 쓰고 부호만 뒤집어 본다.
        // 손해(나빠지는 쪽) 자리는 DESIGN.md danger-text 색 — 그 줄에 손해 자리만 있으면 줄 전체를, 섞여 있으면 숫자만 물들인다.
        private const string SHOP_ITEM_PENALTY_COLOR_HEX = "#E06666";
        private static readonly System.Text.RegularExpressions.Regex ShopItemPlaceholderRegex = new(@"\{(\d+)\}");

        private static string _BuildShopItemDescription(UpgradeData upgradeData)
        {
            var arguments = new List<object>();
            var penaltyIndexes = new HashSet<int>();

            if (upgradeData.UpgradeDataType == UpgradeDataType.GoldGain)
            {
                arguments.Add($"{Mathf.Abs(upgradeData.UpgradeValue):0.#}");
            }
            else if (upgradeData.Stats != null)
            {
                foreach (var stat in upgradeData.Stats)
                {
                    float shownValue = stat == null ? 0f : (stat.Type == Stats.StatType.AttackInterval ? -stat.Value : stat.Value);

                    if (shownValue < 0f)
                        penaltyIndexes.Add(arguments.Count);

                    arguments.Add($"{Mathf.Abs(shownValue):0.#}");
                }
            }

            var lines = new List<string>();

            foreach (string templateLine in upgradeData.Description.Split('\n'))
            {
                var placeholders = ShopItemPlaceholderRegex.Matches(templateLine);
                bool hasPenalty = false;
                bool hasBenefit = false;

                foreach (System.Text.RegularExpressions.Match match in placeholders)
                {
                    if (penaltyIndexes.Contains(int.Parse(match.Groups[1].Value)))
                        hasPenalty = true;
                    else
                        hasBenefit = true;
                }

                string line = templateLine;

                if (hasPenalty && hasBenefit)
                {
                    // 숫자만(앞뒤 % 포함) 물들인다 — 터키어처럼 %가 앞에 오는 언어도 같이 잡는다.
                    foreach (int index in penaltyIndexes)
                        line = System.Text.RegularExpressions.Regex.Replace(line, @"%?\{" + index + @"\}%?",
                            match => $"<color={SHOP_ITEM_PENALTY_COLOR_HEX}>{match.Value}</color>");
                }

                line = _SafeFormat(line, arguments.ToArray()).Trim();

                if (hasPenalty && !hasBenefit)
                    line = $"<color={SHOP_ITEM_PENALTY_COLOR_HEX}>{line}</color>";

                lines.Add(line);
            }

            return string.Join("\n", lines);
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
