using System.Collections.Generic;
using System.Linq;
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
        // 발사 속도 상한(base 대비 배율). 밸런스가 아니라 프레임 천장(FixedUpdate 50Hz)에 닿기 전에
        // 공속 카드를 내리는 안전장치라 데이터가 아닌 상수로 둔다.
        public const float MAX_ATTACK_SPEED_MULTIPLIER = 10f;

        // 사거리·공격 횟수 카드엔 상한이 없다(10-20 사용자 결정). 사거리는 증가율 0.08/등급과 카드 등장 수가 자연스러운 천장이고,
        // 공격 횟수는 상점 카드 규칙 자체가 없다(연타는 개조 패시브로만 오른다).


        private readonly Train _train;
        private readonly StatType _statType;
        private readonly StatUpgradeTierData _tier;
        private readonly TrainStatUpgradeRuleData _rule;

        // 복합 카드 전용 — 한 장에 같이 담긴 두 번째 스탯. 단일 카드면 null이다.
        private readonly TrainStatUpgradeRuleData _secondRule;

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
        }

        public Train TargetTrain => _train;
        public StatType StatType => _statType;
        public int Grade => _displayTier.Grade;

        // 한 장에 스탯 두 개가 담긴 카드인가.
        public bool IsCombo => _secondRule != null;
        // 복합 카드의 두 번째 스탯. 단일 카드면 null.
        public StatType? SecondStatType => _secondRule?.StatType;

        // 효과·가격을 정하는 등급(복합은 표시 등급보다 한 단계 아래). 가격은 상점(ShopOfferPricing)이 이 값으로 매긴다.
        public StatUpgradeTierData EffectTier => _tier;
        // 표시 등급·등장 구간·추첨 가중치를 정하는 등급. 추첨 가중치는 상점 추첨(TriChoiceManager.Shop)이 이 값으로 매긴다.
        public StatUpgradeTierData DisplayTier => _displayTier;

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
                    if (rule == null || _IsOfferBlocked(train, rule))
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
                    if (rule == null || _IsOfferBlocked(train, rule))
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

        // 런 세이브가 남긴 id("StatUpgrade_{포탑}_{스탯}[+{스탯}]_T{표시 등급}")로 카드를 다시 만든다.
        // 복합 카드는 상점을 열 때마다 조합을 무작위로 뽑아(CreateComboOptionsFor) 후보 풀에서 id로 되찾을 수 없으므로,
        // 이어하기 때 저장 당시와 같은 조합을 여기서 직접 만든다. 유효성(상한·보유 여부)은 호출자가 IsValid로 판정한다.
        public static TrainStatUpgradeChoice TryCreateFromId(string id)
        {
            var match = Regex.Match(id ?? string.Empty, @"^StatUpgrade_(\d+)_(\w+?)(?:\+(\w+))?_T(\d+)$");

            if (!match.Success)
                return null;

            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;
            var databaseManager = DatabaseManager.Instance;

            if (mainTrain == null || databaseManager == null)
                return null;

            string trainId = match.Groups[1].Value;
            var train = mainTrain.CurrentTrains.FirstOrDefault(t => t.TrainData != null && t.TrainData.Id == trainId);

            if (train == null || !System.Enum.TryParse(match.Groups[2].Value, out StatType firstStat))
                return null;

            bool isCombo = match.Groups[3].Success;
            StatType secondStat = default;

            if (isCombo && !System.Enum.TryParse(match.Groups[3].Value, out secondStat))
                return null;

            var tiers = databaseManager.GetStatUpgradeTiers();

            if (tiers == null)
                return null;

            int displayGrade = int.Parse(match.Groups[4].Value);
            int displayIndex = -1;

            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i] != null && tiers[i].Grade == displayGrade)
                    displayIndex = i;
            }

            // 복합은 효과 등급이 표시 등급 한 단계 아래라 표시 등급이 첫 티어면 만들 수 없다.
            if (displayIndex < 0 || (isCombo && displayIndex < 1))
                return null;

            var rules = databaseManager.GetTrainStatUpgradeRules(train.TrainData.Id);
            var firstRule = rules.FirstOrDefault(rule => rule != null && rule.StatType == firstStat);

            if (firstRule == null)
                return null;

            if (!isCombo)
                return new TrainStatUpgradeChoice(train, firstRule, tiers[displayIndex]);

            var secondRule = rules.FirstOrDefault(rule => rule != null && rule.StatType == secondStat);

            if (secondRule == null)
                return null;

            return new TrainStatUpgradeChoice(train, firstRule, tiers[displayIndex - 1], secondRule, tiers[displayIndex]);
        }

        // 둔화율이 포탑의 최대값(RangeTrain.MaxSlowRate, 개조로 오를 수 있음) 이상이면 상점에 둔화 카드를 더 내지 않는다.
        // 최대값 직전에 산 카드는 최대값까지만 오른다(RangeTrain이 잘라 낸다).
        private static bool _IsOfferBlocked(Train train, TrainStatUpgradeRuleData rule)
        {
            return rule.StatType == StatType.SlowRate && train is RangeTrain rangeTrain
                && train.GetCurrentStatValue(StatType.SlowRate) >= rangeTrain.MaxSlowRate;
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

            return true;
        }

        // 이 스탯을 지금 더 살 수 있는가 — 효과가 상한에 닿은 스탯은 돈만 나가므로 카드를 내린다.
        private bool _IsStatBuyable(TrainStatUpgradeRuleData rule)
        {
            // 공속은 발사 속도 상한에 닿으면 더 사도 효과가 없다.
            if (rule.StatType == StatType.AttackInterval && _GetAttackSpeedMultiplier() >= MAX_ATTACK_SPEED_MULTIPLIER)
                return false;

            // 분사형(화염)은 분사 각도가 상한에 닿으면 범위를 더 사도 모양과 판정이 안 바뀐다. 사거리를 올리면 다시 열린다.
            if (rule.StatType == StatType.AttackArea && _train is TurretTrain areaTrain && areaTrain.IsAttackAreaAtLimit)
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
                // 업그레이드 이벤트를 받는 배지가 새 합계를 읽도록 먼저 더한다. 복합 카드는 표시 등급으로 센다.
                _train.AddUpgradeGradeSum(Grade);
                mainTrain.UpgradeTrain(_train.TrainData.Id, upgradeData);
                // 사거리 상한·범위 곡선·공격 횟수 상한이 누적량으로 판정되므로 적용 후 반드시 등급 배수만큼 더한다.
                _train.AddStatUpgradeAmount(_statType, IsCountStat(_statType) ? 1f : _tier.ValueMultiplier);

                if (_secondRule != null)
                    _train.AddStatUpgradeAmount(_secondRule.StatType,
                        IsCountStat(_secondRule.StatType) ? 1f : _tier.ValueMultiplier);
            }
        }

        // 지금 발사 속도 배율 = 메타 적용 기본 간격 ÷ 지금 간격 (상점·레벨업 공속이 모두 담긴 값).
        private float _GetAttackSpeedMultiplier()
        {
            float baseInterval = _train switch
            {
                TurretTrain turretTrain => turretTrain.MetaBaseStatus.AttackInterval,
                RangeTrain rangeTrain => rangeTrain.MetaBaseStatus.AttackInterval,
                _ => 0f,
            };
            float currentInterval = _train.GetCurrentStatValue(StatType.AttackInterval);

            return baseInterval > 0f && currentInterval > 0f ? baseInterval / currentInterval : 1f;
        }

        // 이번 구매로 줄어드는 간격(음수). 간격 = base ÷ (1 + 상점% + 레벨업%) 곡선의 차분이라
        // 구매를 거듭할수록 감소폭이 작아지지만 발사 횟수(=DPS)는 등급 배수에 정확히 비례해 늘어난다.
        // 지금 배율은 현재 간격에서 역산한다 — 레벨업 공속도 같은 배율에 더해지기 때문(Train.GetAttackIntervalAfterSpeedBonus).
        private float _GetAttackIntervalDelta(float baseInterval, TrainStatUpgradeRuleData rule)
        {
            float currentInterval = _train.GetCurrentStatValue(StatType.AttackInterval);

            return Train.GetAttackIntervalAfterSpeedBonus(baseInterval, currentInterval, rule.IncreaseRate * _tier.ValueMultiplier) - currentInterval;
        }

        // 이번 구매로 오르는 둔화율(%p). 카드 표기("+5%")를 유저가 30%→35%로 읽으므로 곡선 없이 rate×등급배수를 그대로 더한다.
        private float _GetSlowRateDelta(TrainStatUpgradeRuleData rule)
        {
            return rule.IncreaseRate * _tier.ValueMultiplier * 100f;
        }

        // 이번 구매로 늘어나는 범위(반경). 사거리·공격력처럼 매번 기본값 × rate×등급배수만큼 같은 양을 더한다 —
        // 면적은 반경²이라 갈수록 빨리 커지므로 범위 rate는 사거리보다 작게 잡는다(StatUpgradeData attack_area).
        private float _GetAttackAreaDelta(float baseArea, TrainStatUpgradeRuleData rule)
        {
            return baseArea * rule.IncreaseRate * _tier.ValueMultiplier;
        }

        // 상점 카드 롱프레스용 — 이 카드를 사면 될 대상 포탑의 스탯(Execute와 같은 강화 데이터로 계산).
        public (string label, string value)[] GetUpgradedStatDetails()
        {
            var upgradeData = _BuildUpgradeData();
            return upgradeData != null ? _train.GetUpgradePreviewStatDetails(upgradeData) : _train.GetStatDetails();
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
                StatType.SlowRate => "Upgrade_Slow",
                _ => null,
            };

            string deltaText = rule.StatType switch
            {
                // 공격력·사거리는 매번 같은 양이 올라 실제 값으로 적는다. 기준은 스킬 트리를 뺀 데이터 기본값 —
                // 스킬 트리는 맨 마지막에 곱하는 것으로 보므로 카드 숫자는 스킬 트리 레벨과 상관없이 고정이다.
                StatType.AttackDamage => $"+{_GetDataBaseValue(StatType.AttackDamage) * rule.IncreaseRate * _tier.ValueMultiplier:0.#}",
                StatType.AttackInterval => $"+{rule.IncreaseRate * _tier.ValueMultiplier * 100f:0}%",
                // 범위는 포탑마다 기본값(1.2~3.8)이 달라 실제 값이 0.06처럼 작게 나오므로 %로 둔다. 매번 기본값 × rate×등급배수만큼 는다.
                StatType.AttackArea => $"+{rule.IncreaseRate * _tier.ValueMultiplier * 100f:0}%",
                StatType.AttackRange => $"+{_GetDataBaseValue(StatType.AttackRange) * rule.IncreaseRate * _tier.ValueMultiplier:0.#}",
                // 절대초 가산 — 템플릿("지속시간 {0}초")이 단위를 붙이므로 숫자만 만든다.
                // 실제로 더해지는 둔화율(%p) 그대로 표시.
                StatType.SlowRate => $"+{rule.IncreaseRate * _tier.ValueMultiplier * 100f:0}%",
                _ => "+1",
            };

            if (localizeKey == null)
                return deltaText;

            string template = TrainDefense.Localize.LocalizeHelper.GetByKey(localizeKey, localizeKey);
            string line = string.Format(template, deltaText);

            return Regex.Replace(line, "<[^>]+>", "").Trim();
        }

        // 카드 숫자의 기준 — 스킬 트리를 뺀 데이터 기본값(공격력·사거리).
        private float _GetDataBaseValue(StatType statType)
        {
            return _train switch
            {
                TurretTrain turretTrain => statType == StatType.AttackDamage ? turretTrain.BaseStatus.AttackDamage : turretTrain.BaseStatus.AttackRange,
                RangeTrain rangeTrain => statType == StatType.AttackDamage ? rangeTrain.BaseStatus.AttackDamage : rangeTrain.BaseStatus.AttackRange,
                _ => 0f,
            };
        }

        // 런타임 강화 데이터에서 실제로 읽히는 인덱스는 "이번에 적용할 레벨"(= 현재 레벨) 하나뿐이다.
        // 고정 길이로 만들면 그 길이를 넘어선 레벨에서 인덱스가 배열 밖이 되어
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
                // 폭발·냉기는 사거리가 곧 공격 반경이다 — 포탑과 같은 선형 가산.
                case StatType.AttackRange: delta.AttackRange += baseStatus.AttackRange * rule.IncreaseRate * _tier.ValueMultiplier; break;
                // 둔화율은 카드에 적힌 %p 그대로 더한다(표기 = 실제). 상한은 RangeTrain이 적용한다.
                case StatType.SlowRate: delta.SlowRate += _GetSlowRateDelta(rule); break;
                default: return false;
            }

            return true;
        }
    }
}
