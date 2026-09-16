using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TrainDefense.Game.Manager;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 상점 "원하는 스탯 강화" 선택지 — 대상 포탑의 스탯 하나를 base 대비 일정량(정수 스탯은 +1) 올린다.
    /// 모든 레벨 인덱스에 동일 델타를 채운 런타임 UpgradeData로 기존 Train.Upgrade 경로
    /// (레벨 카운트·HP 회복·상점 배율·풀 사이징)를 그대로 재사용한다. DB에 직렬화되지 않는 런타임 전용.
    /// </summary>
    public class TrainStatUpgradeChoice : IChoiceOption
    {
        // ★ 테스트 스위치 — true면 포탑 만렙(Train.MAX_LEVEL)을 무시하고 무한히 강화할 수 있다.
        // 엘리트 승격 조건(Train.ELITE_PROMOTION_LEVEL)은 이 값과 무관하게 그대로 적용된다.
        public const bool UNLIMITED_UPGRADE_LEVEL = true;

        // 발사 속도 상한(base 대비 배율). 밸런스가 아니라 프레임 천장(FixedUpdate 50Hz)에 닿기 전에
        // 공속 카드를 내리는 안전장치라 데이터가 아닌 상수로 둔다.
        public const float MAX_ATTACK_SPEED_MULTIPLIER = 10f;

        // 상점 공격 횟수 카드 상한(밸런스 컷). 물리 천장은 7(연타 간격이 주기의 15%라 8회부터 다음 주기에
        // 잘림)이고, 엘리트 패시브 가산(+2)을 합해도 천장 아래에 머물도록 카드는 3에서 멈춘다.
        public const int MAX_ATTACK_COUNT = 3;

        // 복합 카드가 같은 티어 단일 카드 대비 얼마나 자주 뜨는가. 1이면 단일 카드가 밀려나므로 절반으로 둔다.
        public const float COMBO_WEIGHT_RATIO = 0.5f;

        // 복합 카드 가격 할인. 두 장 값을 그대로 받으면 원당 효율이 한 등급 아래 단일과 같아져,
        // 등장 확률이 절반인 카드에 희귀 보상이 없어진다. 조합이 무작위라 반쪽이 노는 경우도 함께 친다.
        public const float COMBO_COST_RATIO = 0.8f;

        private readonly Train _train;
        private readonly StatType _statType;
        private readonly StatUpgradeTierData _tier;
        private readonly TrainStatUpgradeRuleData _rule;
        private readonly float _statCostMultiplier;

        // 복합 카드 전용 — 한 장에 같이 담긴 두 번째 스탯. 단일 카드면 null이다.
        private readonly TrainStatUpgradeRuleData _secondRule;
        private readonly float _secondStatCostMultiplier;

        // 복합 카드의 표시 등급·등장 구간·추첨 가중치. 효과와 가격은 한 단계 아래인 _tier가 정한다.
        // (2티어 복합 = 1등급 효과 두 개) 단일 카드에서는 _tier와 같은 것을 가리킨다.
        private readonly StatUpgradeTierData _displayTier;

        public TrainStatUpgradeChoice(Train targetTrain, TrainStatUpgradeRuleData rule, StatUpgradeTierData tier)
            : this(targetTrain, rule, tier, null, tier)
        {
        }

        public TrainStatUpgradeChoice(Train targetTrain, TrainStatUpgradeRuleData rule, StatUpgradeTierData effectTier,
            TrainStatUpgradeRuleData secondRule, StatUpgradeTierData displayTier)
        {
            _train = targetTrain;
            _rule = rule;
            _statType = rule.StatType;
            _tier = effectTier;
            _secondRule = secondRule;
            _displayTier = displayTier;

            var databaseManager = DatabaseManager.Instance;
            _statCostMultiplier = databaseManager != null ? databaseManager.GetStatCostMultiplier(_statType) : 1f;
            _secondStatCostMultiplier = secondRule != null && databaseManager != null
                ? databaseManager.GetStatCostMultiplier(secondRule.StatType)
                : 0f;
        }

        public Train TargetTrain => _train;
        public StatType StatType => _statType;
        public int Grade => _displayTier.Grade;

        // 한 장에 스탯 두 개가 담긴 카드인가.
        public bool IsCombo => _secondRule != null;
        public StatType SecondStatType => _secondRule != null ? _secondRule.StatType : _statType;

        // 상점 추첨 가중치 — 등급 데이터가 정한다(고등급 = 희귀). 복합은 같은 티어 단일의 절반.
        public int TierWeight => IsCombo
            ? Mathf.Max(1, Mathf.RoundToInt(_displayTier.Weight * COMBO_WEIGHT_RATIO))
            : _displayTier.Weight;

        // 가격 배수 = 등급 배수(고등급일수록 단가 할인 — 희귀 보상) × 스탯 프리미엄(+1 가치가 큰 정수 스탯).
        // 복합은 "카드 두 장 값"이라 두 스탯의 프리미엄을 더한 뒤 COMBO_COST_RATIO로 깎는다 — 두 장 값을
        // 그대로 받으면 한 등급 아래 단일과 원당 효율이 같아져, 확률이 절반인 카드를 살 이유가 남지 않는다.
        // 상점(ShopOfferPricing)이 기본가에 곱해 최종 가격을 낸다.
        public float CostMultiplier => _tier.CostMultiplier * (_statCostMultiplier + _secondStatCostMultiplier)
            * (IsCombo ? COMBO_COST_RATIO : 1f);

        public string Id => IsCombo
            ? $"StatUpgrade_{(_train != null && _train.TrainData != null ? _train.TrainData.Id : "?")}_{_statType}+{_secondRule.StatType}_T{_displayTier.Grade}"
            : $"StatUpgrade_{(_train != null && _train.TrainData != null ? _train.TrainData.Id : "?")}_{_statType}_T{_displayTier.Grade}";

        // 만렙 전까지 반복 구매 가능.
        public bool IsRepeatable => true;

        // 보유 포탑 하나가 상점에 낼 수 있는 스탯 강화 선택지 — DB의 포탑별 스탯 규칙 × 지금 등장 가능한 등급.
        // 어떤 스탯을 강화할 수 있는지, 증가율이 얼마인지는 전부 train_stat_upgrade_data 시트가 정한다.
        public static List<TrainStatUpgradeChoice> CreateOptionsFor(Train train)
        {
            var options = new List<TrainStatUpgradeChoice>();

            if (train == null || train.TrainData == null)
                return options;

            var databaseManager = DatabaseManager.Instance;

            if (databaseManager == null)
                return options;

            var tiers = databaseManager.GetStatUpgradeTiers();

            if (tiers == null)
                return options;

            var stageManager = StageManager.Instance;
            int shopVisitCount = stageManager != null ? stageManager.TotalInspectionPassedCount : 0;

            foreach (var tier in tiers)
            {
                if (tier == null || !tier.IsAvailableAt(shopVisitCount))
                    continue;

                foreach (var rule in databaseManager.GetTrainStatUpgradeRules(train.TrainData.Id))
                {
                    if (rule == null)
                        continue;

                    int minGrade = databaseManager.GetStatMinGrade(rule.StatType);

                    if (tier.Grade < minGrade)
                        continue;

                    // 정수 스탯은 증가량이 +1 고정이라 상위 등급은 비싸기만 하다 → 최소 등급에서만 낸다.
                    if (IsCountStat(rule.StatType) && tier.Grade != minGrade)
                        continue;

                    options.Add(new TrainStatUpgradeChoice(train, rule, tier));
                }
            }

            return options;
        }

        // 복합 카드 후보 — 이 포탑이 올릴 수 있는 스탯 중 무작위 두 개를 한 장에 담는다.
        // N티어 복합 = (N-1)등급 효과 두 개. 총량은 단일 2(N-1)등급과 같지만 서로 다른 축이라 곱해져 조금 더 세고,
        // 그만큼 가격도 "카드 두 장 값"이다. 등장 구간과 가중치는 표시 등급(N)을 따른다.
        // 조합은 상점을 열 때마다(=이 메서드 호출마다) 다시 뽑히므로 리롤하면 다른 조합이 나온다.
        public static List<TrainStatUpgradeChoice> CreateComboOptionsFor(Train train)
        {
            var options = new List<TrainStatUpgradeChoice>();

            if (train == null || train.TrainData == null)
                return options;

            var databaseManager = DatabaseManager.Instance;

            if (databaseManager == null)
                return options;

            var tiers = databaseManager.GetStatUpgradeTiers();

            if (tiers == null || tiers.Count < 2)
                return options;

            var stageManager = StageManager.Instance;
            int shopVisitCount = stageManager != null ? stageManager.TotalInspectionPassedCount : 0;

            // 1등급짜리 복합은 없다(효과 등급이 0이 되므로) — 표시 등급은 2부터 센다.
            for (int i = 1; i < tiers.Count; i++)
            {
                var displayTier = tiers[i];
                var effectTier = tiers[i - 1];

                if (displayTier == null || effectTier == null || !displayTier.IsAvailableAt(shopVisitCount))
                    continue;

                var candidates = new List<TrainStatUpgradeRuleData>();

                foreach (var rule in databaseManager.GetTrainStatUpgradeRules(train.TrainData.Id))
                {
                    if (rule == null)
                        continue;

                    int minGrade = databaseManager.GetStatMinGrade(rule.StatType);

                    if (effectTier.Grade < minGrade)
                        continue;

                    // 단일 카드와 같은 규칙 — 정수 스탯은 +1 고정이라 상위 등급이 비싸기만 하다.
                    if (IsCountStat(rule.StatType) && effectTier.Grade != minGrade)
                        continue;

                    candidates.Add(rule);
                }

                if (candidates.Count < 2)
                    continue;

                int firstIndex = Random.Range(0, candidates.Count);
                int secondIndex = Random.Range(0, candidates.Count - 1);

                if (secondIndex >= firstIndex)
                    secondIndex++;

                options.Add(new TrainStatUpgradeChoice(train, candidates[firstIndex], effectTier,
                    candidates[secondIndex], displayTier));
            }

            return options;
        }

        // 개수로 오르는 스탯 — 등급 배수를 개수로 쓰면 폭발하므로(포격 연타 1→6) 항상 +1이다.
        public static bool IsCountStat(StatType statType)
        {
            return statType == StatType.TargetCount || statType == StatType.AttackCount;
        }

        public bool IsValid()
        {
            if (_train == null || _train.TrainData == null)
                return false;

            var trainManager = TrainManager.Instance;

            if (trainManager == null)
                return false;

            if (trainManager.IsTrainIdReplaced(_train.TrainData.Id))
                return false;

            if (!trainManager.CheckHasTrainById(_train.TrainData.Id))
                return false;

            // 복합은 두 스탯 중 하나라도 상한이면 반쪽 카드가 되므로 통째로 내린다.
            if (!_IsStatBuyable(_rule))
                return false;

            if (_secondRule != null && !_IsStatBuyable(_secondRule))
                return false;

            // 레벨 = 받은 업그레이드 횟수(획득 0). 만렙이면 더 못 산다.
            return UNLIMITED_UPGRADE_LEVEL || _train.CurrentLevel < Train.MAX_LEVEL;
        }

        // 이 스탯을 지금 더 살 수 있는가 — 효과가 상한에 닿은 스탯은 돈만 나가므로 카드를 내린다.
        private bool _IsStatBuyable(TrainStatUpgradeRuleData rule)
        {
            // 공속은 발사 속도 상한에 닿으면 더 사도 효과가 없다.
            if (rule.StatType == StatType.AttackInterval && _GetAttackSpeedMultiplier(rule) >= MAX_ATTACK_SPEED_MULTIPLIER)
                return false;

            // 공격 횟수는 연타가 주기를 넘치는 한계에 닿으면 멈춘다. 누적 = base + 구매 횟수(+1씩).
            if (rule.StatType == StatType.AttackCount && _train is TurretTrain countTrain
                && countTrain.BaseStatus.AttackCount + Mathf.RoundToInt(_train.GetStatUpgradeAmount(StatType.AttackCount)) >= MAX_ATTACK_COUNT)
                return false;

            return true;
        }

        public void Execute()
        {
            var mainTrain = TrainManager.Instance?.MainTrain;

            if (mainTrain == null || !IsValid())
                return;

            var upgradeData = _BuildUpgradeData();

            if (upgradeData != null)
            {
                mainTrain.UpgradeTrain(_train.TrainData.Id, upgradeData);
                // 공속 증가분이 누적량에서 나오므로 적용 후 반드시 등급 배수만큼 더한다.
                _train.AddStatUpgradeAmount(_statType, IsCountStat(_statType) ? 1f : _tier.ValueMultiplier);

                if (_secondRule != null)
                    _train.AddStatUpgradeAmount(_secondRule.StatType,
                        IsCountStat(_secondRule.StatType) ? 1f : _tier.ValueMultiplier);
            }
        }

        // 발사 속도 배율 = 1 + rate × 누적 강화량. (간격 = base ÷ 이 값)
        private float _GetAttackSpeedMultiplier(TrainStatUpgradeRuleData rule)
        {
            return 1f + rule.IncreaseRate * _train.GetStatUpgradeAmount(StatType.AttackInterval);
        }

        // 이번 구매로 줄어드는 간격(음수). 간격 = base ÷ (1 + rate×누적량) 곡선의 차분이라
        // 구매를 거듭할수록 감소폭이 작아지지만 발사 횟수(=DPS)는 등급 배수에 정확히 비례해 늘어난다.
        private float _GetAttackIntervalDelta(float baseInterval, TrainStatUpgradeRuleData rule)
        {
            float currentMultiplier = _GetAttackSpeedMultiplier(rule);
            float nextMultiplier = currentMultiplier + rule.IncreaseRate * _tier.ValueMultiplier;

            return baseInterval / nextMultiplier - baseInterval / currentMultiplier;
        }

        // 이번 구매로 오르는 둔화율(%p). 둔화율 = 1 − 1/지연배율 곡선의 차분이라(공속과 동일 구조)
        // 구매를 거듭할수록 %p는 작아지지만 적 지연 시간은 등급 배수에 정확히 비례해 늘어난다.
        // 100%(완전 정지)에는 점근만 하므로 상한 가드가 필요 없다.
        private float _GetSlowRateDelta(float baseSlowRate, TrainStatUpgradeRuleData rule)
        {
            float baseMultiplier = 1f / (1f - baseSlowRate / 100f);
            float currentMultiplier = baseMultiplier + rule.IncreaseRate * _train.GetStatUpgradeAmount(StatType.SlowRate);
            float nextMultiplier = currentMultiplier + rule.IncreaseRate * _tier.ValueMultiplier;

            return (1f / currentMultiplier - 1f / nextMultiplier) * 100f;
        }

        // 이번 구매로 늘어나는 범위(반경). 커버 면적 ∝ 반경²이라 반경을 그대로 가산하면 실효가 제곱으로 폭주한다.
        // 반경 = base × √(1 + rate×누적) 곡선의 차분 — 커버 면적이 등급 배수에 정확히 비례해 늘어난다(공속과 동일 구조).
        private float _GetAttackAreaDelta(float baseArea, TrainStatUpgradeRuleData rule)
        {
            float currentMultiplier = 1f + rule.IncreaseRate * _train.GetStatUpgradeAmount(StatType.AttackArea);
            float nextMultiplier = currentMultiplier + rule.IncreaseRate * _tier.ValueMultiplier;

            return baseArea * (Mathf.Sqrt(nextMultiplier) - Mathf.Sqrt(currentMultiplier));
        }

        // 상점 슬롯 설명용 "레이블 +값" 줄 (Upgrade_* 로컬라이즈 템플릿 재사용, 리치 태그 제거).
        // 복합 카드는 두 줄로 낸다.
        public string BuildStatLineText()
        {
            string line = _BuildOneStatLine(_rule);

            if (_secondRule == null)
                return line;

            return line + "\n" + _BuildOneStatLine(_secondRule);
        }

        private string _BuildOneStatLine(TrainStatUpgradeRuleData rule)
        {
            string localizeKey = rule.StatType switch
            {
                StatType.AttackDamage => "Upgrade_AttackDamage",
                StatType.AttackInterval => "Upgrade_AttackSpeed",
                StatType.AttackArea => "Upgrade_AttackArea",
                StatType.AttackRange => "Upgrade_AttackRange",
                StatType.TargetCount => "Upgrade_TargetCount",
                StatType.AttackCount => "Upgrade_AttackCount",
                StatType.BurstDuration => "Upgrade_BurstDuration",
                StatType.SlowRate => "Upgrade_Slow",
                _ => null,
            };

            string deltaText = rule.StatType switch
            {
                StatType.AttackDamage => $"+{_GetBaseAttackDamage() * rule.IncreaseRate * _tier.ValueMultiplier:0.#}",
                StatType.AttackInterval => $"+{rule.IncreaseRate * _tier.ValueMultiplier * 100f:0}%",
                // 커버 면적(실효) 기준 고정 표시 — 이 축에선 매 장 정확히 rate×등급배수만큼 는다(공속과 동일 철학).
                StatType.AttackArea => $"+{rule.IncreaseRate * _tier.ValueMultiplier * 100f:0}%",
                // 사거리는 범위와 달리 제곱 보정을 하지 않는다 — 실효(적이 사정권에 머무는 시간)가
                // 반경에 비례하므로 표시값이 곧 실제 증가율이다.
                StatType.AttackRange => $"+{rule.IncreaseRate * _tier.ValueMultiplier * 100f:0}%",
                // 절대초 가산 — 템플릿("지속시간 {0}초")이 단위를 붙이므로 숫자만 만든다.
                StatType.BurstDuration => $"+{rule.IncreaseRate * _tier.ValueMultiplier:0.#}",
                // 적 지연 시간(실효) 기준 고정 표시 — 이 축에선 매 장 정확히 rate×등급배수만큼 는다(공속과 동일 철학).
                StatType.SlowRate => $"+{rule.IncreaseRate * _tier.ValueMultiplier * 100f:0}%",
                _ => "+1",
            };

            if (localizeKey == null)
                return deltaText;

            string template = TrainDefense.Localize.LocalizeHelper.GetByKey(localizeKey, localizeKey);
            string line = string.Format(template, deltaText);

            return Regex.Replace(line, "<[^>]+>", "").Trim();
        }

        private float _GetBaseAttackDamage()
        {
            if (_train is TurretTrain turretTrain)
                return turretTrain.MetaBaseStatus.AttackDamage;

            if (_train is RangeTrain rangeTrain)
                return rangeTrain.MetaBaseStatus.AttackDamage;

            return 0f;
        }

        // 런타임 강화 데이터에서 실제로 읽히는 인덱스는 "이번에 적용할 레벨"(= 현재 레벨) 하나뿐이다.
        // 고정 길이(MAX_LEVEL)로 만들면 만렙을 넘어선 레벨에서 인덱스가 배열 밖이 되어
        // 증가량이 0으로 조용히 사라지므로(코인만 빠짐), 항상 그 인덱스까지 채운다.
        private int _GetUpgradeLevelCount() => _train.CurrentLevel + 1;

        private ITrainUpgradeData _BuildUpgradeData()
        {
            if (_train is TurretTrain turretTrain)
            {
                // 메타 적용 후 값을 기준으로 잡아야 카드 성장 전체에 메타 %가 곱해진다 (원본 기준이면 메타가 후반에 희석됨).
                var baseStatus = turretTrain.MetaBaseStatus;
                var delta = new TurretTrainStatus();

                if (!_ApplyTurretDelta(ref delta, baseStatus, _rule))
                    return null;

                if (_secondRule != null && !_ApplyTurretDelta(ref delta, baseStatus, _secondRule))
                    return null;

                return TurretTrainUpgradeData.CreateRuntimeSingleStat(Id, _train.TrainData.Name, delta, _GetUpgradeLevelCount());
            }

            if (_train is RangeTrain rangeTrain)
            {
                var baseStatus = rangeTrain.MetaBaseStatus;
                var delta = new RangeTrainStatus();

                if (!_ApplyRangeDelta(ref delta, baseStatus, _rule))
                    return null;

                if (_secondRule != null && !_ApplyRangeDelta(ref delta, baseStatus, _secondRule))
                    return null;

                return RangeTrainUpgradeData.CreateRuntimeSingleStat(Id, _train.TrainData.Name, delta, _GetUpgradeLevelCount());
            }

            return null;
        }

        // 스탯 하나가 이번 구매로 더할 양을 delta에 얹는다. 복합 카드는 두 rule로 두 번 호출한다.
        // 지원하지 않는 스탯이면 false — 카드 자체를 무효로 만든다.
        // ★ TurretTrainStatus는 struct라 ref 없이 넘기면 복사본만 바뀌고 증가량이 조용히 사라진다.
        private bool _ApplyTurretDelta(ref TurretTrainStatus delta, TurretTrainStatus baseStatus, TrainStatUpgradeRuleData rule)
        {
            {
                switch (rule.StatType)
                {
                    case StatType.AttackDamage: delta.AttackDamage += baseStatus.AttackDamage * rule.IncreaseRate * _tier.ValueMultiplier; break;
                    case StatType.AttackInterval: delta.AttackInterval += _GetAttackIntervalDelta(baseStatus.AttackInterval, rule); break;
                    case StatType.AttackArea: delta.AttackArea += _GetAttackAreaDelta(baseStatus.AttackArea, rule); break;
                    // 사거리는 base 대비 선형 가산(공격력과 동일) — 범위처럼 면적이 아니라
                    // 사정권 체류 시간이 반경에 비례해 늘어나므로 제곱 보정이 필요 없다.
                    case StatType.AttackRange: delta.AttackRange += baseStatus.AttackRange * rule.IncreaseRate * _tier.ValueMultiplier; break;
                    case StatType.TargetCount: delta.TargetCount += 1; break;
                    case StatType.AttackCount: delta.AttackCount += 1; break;
                    // 분사 지속시간은 base 비율이 아니라 절대초 — rate가 1등급이 더할 초.
                    case StatType.BurstDuration: delta.BurstDuration += rule.IncreaseRate * _tier.ValueMultiplier; break;
                    default: return false;
                }
            }

            return true;
        }

        // RangeTrainStatus도 struct — ref 필수.
        private bool _ApplyRangeDelta(ref RangeTrainStatus delta, RangeTrainStatus baseStatus, TrainStatUpgradeRuleData rule)
        {
            switch (rule.StatType)
            {
                case StatType.AttackDamage: delta.AttackDamage += baseStatus.AttackDamage * rule.IncreaseRate * _tier.ValueMultiplier; break;
                case StatType.AttackInterval: delta.AttackInterval += _GetAttackIntervalDelta(baseStatus.AttackInterval, rule); break;
                case StatType.AttackArea: delta.AttackArea += _GetAttackAreaDelta(baseStatus.AttackArea, rule); break;
                // 분사 지속시간은 base 비율이 아니라 절대초 — rate가 1등급이 더할 초.
                case StatType.BurstDuration: delta.BurstDuration += rule.IncreaseRate * _tier.ValueMultiplier; break;
                // 둔화율은 지연 배율 곡선의 차분 — 공속과 동일 구조(점근, 상한 불필요).
                case StatType.SlowRate: delta.SlowRate += _GetSlowRateDelta(baseStatus.SlowRate, rule); break;
                default: return false;
            }

            return true;
        }
    }
}
