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
        public const bool UNLIMITED_UPGRADE_LEVEL = true;

        // 발사 속도 상한(base 대비 배율). 밸런스가 아니라 프레임 천장(FixedUpdate 50Hz)에 닿기 전에
        // 공속 카드를 내리는 안전장치라 데이터가 아닌 상수로 둔다.
        public const float MAX_ATTACK_SPEED_MULTIPLIER = 10f;

        // 상점 공격 횟수 카드 상한(밸런스 컷). 물리 천장은 7(연타 간격이 주기의 15%라 8회부터 다음 주기에
        // 잘림)이고, 엘리트 패시브 가산(+2)을 합해도 천장 아래에 머물도록 카드는 3에서 멈춘다.
        public const int MAX_ATTACK_COUNT = 3;

        // 상점 사거리 카드 상한(base 대비 배율). 사거리가 화면을 넘으면 돈만 나가고, 분사형(화염)은 입자 수가
        // 길이의 제곱으로 늘어 성능 천장에 닿는다. 등급이 큰 카드 한 장이 상한을 넘겨 버리지 않도록
        // "이 카드를 산 뒤"의 배율로 판정한다 — 남은 여유에 맞는 낮은 등급 카드는 계속 뜬다.
        public const float MAX_ATTACK_RANGE_MULTIPLIER = 2f;

        // 복합 카드가 같은 티어 단일 카드 대비 얼마나 자주 뜨는가. 1이면 단일 카드가 밀려나므로 절반으로 둔다.
        public const float COMBO_WEIGHT_RATIO = 0.5f;

        // 복합 카드 가격 배율. 1이면 한 등급 아래 단일 카드 두 장 값 그대로다(2등급 복합 = 1등급 두 장).
        // 할인하면 원당 가격이 게임에서 가장 싼 카드가 된다. 한 칸에 효과 두 개가 담기는 것만으로 이득이 남는다.
        public const float COMBO_COST_RATIO = 1f;

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
        // 등급마다 등장 구간 안에서 가중치가 변한다(저등급은 뒤로 갈수록 덜, 고등급은 더) — 그래서 방문 수가 필요하다.
        public int TierWeight
        {
            get
            {
                var stageManager = StageManager.Instance;
                int shopVisitCount = stageManager != null ? stageManager.TotalInspectionPassedCount : 0;
                int finalShopVisit = stageManager != null ? stageManager.FinalStationCount : 0;
                int tierWeight = _displayTier.GetWeightAt(shopVisitCount, finalShopVisit);

                return IsCombo ? Mathf.Max(1, Mathf.RoundToInt(tierWeight * COMBO_WEIGHT_RATIO)) : tierWeight;
            }
        }

        // 가격 배수 = 등급 배수(고등급일수록 단가 할인 — 희귀 보상) × 스탯 프리미엄(+1 가치가 큰 정수 스탯).
        // 복합은 "카드 두 장 값"이라 두 스탯의 프리미엄을 더한 뒤 COMBO_COST_RATIO를 곱한다.
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

        // 둔화율이 이 값 이상이면 상점에 둔화 카드를 더 내지 않는다. 이미 산 카드는 넘겨도 그대로 적용된다(55%에서 +15% → 70%).
        private const float SLOW_RATE_OFFER_LIMIT = 60f;

        private static bool _IsOfferBlocked(Train train, TrainStatUpgradeRuleData rule)
        {
            return rule.StatType == StatType.SlowRate && train.GetCurrentStatValue(StatType.SlowRate) >= SLOW_RATE_OFFER_LIMIT;
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
            if (rule.StatType == StatType.AttackInterval && _GetAttackSpeedMultiplier() >= MAX_ATTACK_SPEED_MULTIPLIER)
                return false;

            // 공격 횟수는 연타가 주기를 넘치는 한계에 닿으면 멈춘다. 누적 = base + 구매 횟수(+1씩).
            if (rule.StatType == StatType.AttackCount && _train is TurretTrain countTrain
                && countTrain.BaseStatus.AttackCount + Mathf.RoundToInt(_train.GetStatUpgradeAmount(StatType.AttackCount)) >= MAX_ATTACK_COUNT)
                return false;

            // 사거리는 이 카드를 샀을 때 상한을 넘으면 내린다.
            if (rule.StatType == StatType.AttackRange && _IsOverAttackRangeLimit(rule))
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

        // 이 카드를 산 뒤의 사거리 배율(1 + rate × (누적 강화량 + 이번 등급 배수))이 상한을 넘는가.
        // 정확히 상한에 닿는 카드는 살 수 있어야 하므로 부동소수 오차만큼은 같다고 본다.
        private bool _IsOverAttackRangeLimit(TrainStatUpgradeRuleData rule)
        {
            float nextMultiplier = 1f + rule.IncreaseRate * (_train.GetStatUpgradeAmount(StatType.AttackRange) + _tier.ValueMultiplier);

            return nextMultiplier > MAX_ATTACK_RANGE_MULTIPLIER && !Mathf.Approximately(nextMultiplier, MAX_ATTACK_RANGE_MULTIPLIER);
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
                StatType.BurstDuration => "Upgrade_BurstDuration",
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
                StatType.BurstDuration => $"+{rule.IncreaseRate * _tier.ValueMultiplier:0.#}",
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
                // 둔화율은 카드에 적힌 %p 그대로 더한다(표기 = 실제). 상한은 RangeTrain이 적용한다.
                case StatType.SlowRate: delta.SlowRate += _GetSlowRateDelta(rule); break;
                default: return false;
            }

            return true;
        }
    }
}
