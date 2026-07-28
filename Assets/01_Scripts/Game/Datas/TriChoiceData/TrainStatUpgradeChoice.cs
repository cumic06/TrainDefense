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

        // 공격 횟수 상한. 연타 간격이 주기의 15%(REPEAT_ATTACK_DELAY_RATIO)라 8회부터는
        // 연타가 다음 주기에 잘려 낭비되므로, 주기 안에 다 들어가는 한계에서 카드를 내린다.
        public const int MAX_ATTACK_COUNT = 7;

        private readonly Train _train;
        private readonly StatType _statType;
        private readonly StatUpgradeTierData _tier;
        private readonly TrainStatUpgradeRuleData _rule;
        private readonly float _statCostMultiplier;

        public TrainStatUpgradeChoice(Train targetTrain, TrainStatUpgradeRuleData rule, StatUpgradeTierData tier)
        {
            _train = targetTrain;
            _rule = rule;
            _statType = rule.StatType;
            _tier = tier;
            _statCostMultiplier = DatabaseManager.Instance != null ? DatabaseManager.Instance.GetStatCostMultiplier(_statType) : 1f;
        }

        public Train TargetTrain => _train;
        public StatType StatType => _statType;
        public int Grade => _tier.Grade;

        // 상점 추첨 가중치 — 등급 데이터가 정한다(고등급 = 희귀).
        public int TierWeight => _tier.Weight;

        // 가격 배수 = 등급 배수(고등급일수록 단가 할인 — 희귀 보상) × 스탯 프리미엄(+1 가치가 큰 정수 스탯).
        // 상점(ShopOfferPricing)이 기본가에 곱해 최종 가격을 낸다.
        public float CostMultiplier => _tier.CostMultiplier * _statCostMultiplier;

        public string Id => $"StatUpgrade_{(_train != null && _train.TrainData != null ? _train.TrainData.Id : "?")}_{_statType}_T{_tier.Grade}";

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

            // 공속은 발사 속도 상한에 닿으면 더 사도 효과가 없으므로 카드를 내린다(돈 낭비 방지).
            if (_statType == StatType.AttackInterval && _GetAttackSpeedMultiplier() >= MAX_ATTACK_SPEED_MULTIPLIER)
                return false;

            // 공격 횟수는 연타가 주기를 넘치는 한계에 닿으면 카드를 내린다. 누적 = base + 구매 횟수(+1씩).
            if (_statType == StatType.AttackCount && _train is TurretTrain countTrain
                && countTrain.BaseStatus.AttackCount + Mathf.RoundToInt(_train.GetStatUpgradeAmount(StatType.AttackCount)) >= MAX_ATTACK_COUNT)
                return false;

            // 레벨 = 받은 업그레이드 횟수(획득 0). 만렙이면 더 못 산다.
            return UNLIMITED_UPGRADE_LEVEL || _train.CurrentLevel < Train.MAX_LEVEL;
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
            }
        }

        // 발사 속도 배율 = 1 + rate × 누적 강화량. (간격 = base ÷ 이 값)
        private float _GetAttackSpeedMultiplier()
        {
            return 1f + _rule.IncreaseRate * _train.GetStatUpgradeAmount(StatType.AttackInterval);
        }

        // 이번 구매로 줄어드는 간격(음수). 간격 = base ÷ (1 + rate×누적량) 곡선의 차분이라
        // 구매를 거듭할수록 감소폭이 작아지지만 발사 횟수(=DPS)는 등급 배수에 정확히 비례해 늘어난다.
        private float _GetAttackIntervalDelta(float baseInterval)
        {
            float currentMultiplier = _GetAttackSpeedMultiplier();
            float nextMultiplier = currentMultiplier + _rule.IncreaseRate * _tier.ValueMultiplier;

            return baseInterval / nextMultiplier - baseInterval / currentMultiplier;
        }

        // 이번 구매로 오르는 둔화율(%p). 둔화율 = 1 − 1/지연배율 곡선의 차분이라(공속과 동일 구조)
        // 구매를 거듭할수록 %p는 작아지지만 적 지연 시간은 등급 배수에 정확히 비례해 늘어난다.
        // 100%(완전 정지)에는 점근만 하므로 상한 가드가 필요 없다.
        private float _GetSlowRateDelta(float baseSlowRate)
        {
            float baseMultiplier = 1f / (1f - baseSlowRate / 100f);
            float currentMultiplier = baseMultiplier + _rule.IncreaseRate * _train.GetStatUpgradeAmount(StatType.SlowRate);
            float nextMultiplier = currentMultiplier + _rule.IncreaseRate * _tier.ValueMultiplier;

            return (1f / currentMultiplier - 1f / nextMultiplier) * 100f;
        }

        private float _GetBaseSlowRate()
        {
            return _train is RangeTrain rangeTrain ? rangeTrain.BaseStatus.SlowRate : 0f;
        }

        // 이번 구매로 늘어나는 범위(반경). 커버 면적 ∝ 반경²이라 반경을 그대로 가산하면 실효가 제곱으로 폭주한다.
        // 반경 = base × √(1 + rate×누적) 곡선의 차분 — 커버 면적이 등급 배수에 정확히 비례해 늘어난다(공속과 동일 구조).
        private float _GetAttackAreaDelta(float baseArea)
        {
            float currentMultiplier = 1f + _rule.IncreaseRate * _train.GetStatUpgradeAmount(StatType.AttackArea);
            float nextMultiplier = currentMultiplier + _rule.IncreaseRate * _tier.ValueMultiplier;

            return baseArea * (Mathf.Sqrt(nextMultiplier) - Mathf.Sqrt(currentMultiplier));
        }

        // 상점 슬롯 설명용 "레이블 +값" 한 줄 (Upgrade_* 로컬라이즈 템플릿 재사용, 리치 태그 제거)
        public string BuildStatLineText()
        {
            string localizeKey = _statType switch
            {
                StatType.AttackDamage => "Upgrade_AttackDamage",
                StatType.AttackInterval => "Upgrade_AttackSpeed",
                StatType.AttackArea => "Upgrade_AttackArea",
                StatType.TargetCount => "Upgrade_TargetCount",
                StatType.AttackCount => "Upgrade_AttackCount",
                StatType.BurstDuration => "Upgrade_BurstDuration",
                StatType.SlowRate => "Upgrade_Slow",
                _ => null,
            };

            string deltaText = _statType switch
            {
                StatType.AttackDamage => $"+{_GetBaseAttackDamage() * _rule.IncreaseRate * _tier.ValueMultiplier:0.#}",
                StatType.AttackInterval => $"+{_rule.IncreaseRate * _tier.ValueMultiplier * 100f:0}%",
                // √ 곡선의 실제 반경 증가분을 표시 (base 100 대입 = base 대비 %) — 살수록 %가 줄어드는 걸 정직하게 보여준다.
                StatType.AttackArea => $"+{_GetAttackAreaDelta(100f):0.#}%",
                // 절대초 가산 — 템플릿("지속시간 {0}초")이 단위를 붙이므로 숫자만 만든다.
                StatType.BurstDuration => $"+{_rule.IncreaseRate * _tier.ValueMultiplier:0.#}",
                // 점근 곡선의 실제 차분을 표시 — 살수록 %p가 줄어드는 걸 카드가 정직하게 보여준다.
                StatType.SlowRate => $"+{_GetSlowRateDelta(_GetBaseSlowRate()):0.#}%",
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
                return turretTrain.BaseStatus.AttackDamage;

            if (_train is RangeTrain rangeTrain)
                return rangeTrain.BaseStatus.AttackDamage;

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
                var baseStatus = turretTrain.BaseStatus;
                var delta = new TurretTrainStatus();

                switch (_statType)
                {
                    case StatType.AttackDamage: delta.AttackDamage = baseStatus.AttackDamage * _rule.IncreaseRate * _tier.ValueMultiplier; break;
                    case StatType.AttackInterval: delta.AttackInterval = _GetAttackIntervalDelta(baseStatus.AttackInterval); break;
                    case StatType.AttackArea: delta.AttackArea = _GetAttackAreaDelta(baseStatus.AttackArea); break;
                    case StatType.TargetCount: delta.TargetCount = 1; break;
                    case StatType.AttackCount: delta.AttackCount = 1; break;
                    // 분사 지속시간은 base 비율이 아니라 절대초 — rate가 1등급이 더할 초.
                    case StatType.BurstDuration: delta.BurstDuration = _rule.IncreaseRate * _tier.ValueMultiplier; break;
                    default: return null;
                }

                return TurretTrainUpgradeData.CreateRuntimeSingleStat(Id, _train.TrainData.Name, delta, _GetUpgradeLevelCount());
            }

            if (_train is RangeTrain rangeTrain)
            {
                var baseStatus = rangeTrain.BaseStatus;
                var delta = new RangeTrainStatus();

                switch (_statType)
                {
                    case StatType.AttackDamage: delta.AttackDamage = baseStatus.AttackDamage * _rule.IncreaseRate * _tier.ValueMultiplier; break;
                    case StatType.AttackInterval: delta.AttackInterval = _GetAttackIntervalDelta(baseStatus.AttackInterval); break;
                    case StatType.AttackArea: delta.AttackArea = _GetAttackAreaDelta(baseStatus.AttackArea); break;
                    // 분사 지속시간은 base 비율이 아니라 절대초 — rate가 1등급이 더할 초.
                    case StatType.BurstDuration: delta.BurstDuration = _rule.IncreaseRate * _tier.ValueMultiplier; break;
                    // 둔화율은 지연 배율 곡선의 차분 — 공속과 동일 구조(점근, 상한 불필요).
                    case StatType.SlowRate: delta.SlowRate = _GetSlowRateDelta(baseStatus.SlowRate); break;
                    default: return null;
                }

                return RangeTrainUpgradeData.CreateRuntimeSingleStat(Id, _train.TrainData.Name, delta, _GetUpgradeLevelCount());
            }

            return null;
        }
    }
}
