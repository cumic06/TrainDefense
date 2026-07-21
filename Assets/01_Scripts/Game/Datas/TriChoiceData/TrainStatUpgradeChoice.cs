using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        // ★ 플레이스홀더 밸런스 — 1회 구매당 base 대비 증가율. 인게임 테스트 후 사용자가 조정.
        public const float DAMAGE_UPGRADE_RATE = 0.15f;
        public const float ATTACK_INTERVAL_REDUCE_RATE = 0.05f; // base 간격 -5%/회 (만렙 몰빵 시 -40%)
        public const float ATTACK_AREA_UPGRADE_RATE = 0.10f;

        // ★ 포탑별 스탯 풀 규칙이 코드에 하드코딩된 상태(플레이스홀더 단계). 신규 포탑 추가 시 여기도 갱신 필요 —
        // 규칙이 굳으면 TrainData의 데이터 필드로 옮기는 게 정석.
        private const string MACHINE_GUN_TRAIN_ID = "30001";
        private const string ELITE_MACHINE_GUN_TRAIN_ID = "31001";
        private const string ELECTRIC_TRAIN_ID = "30005";
        private const string ELITE_ELECTRIC_TRAIN_ID = "31005";
        private const string CANNON_TRAIN_ID = "30006";
        private const string ELITE_CANNON_TRAIN_ID = "31006";
        private const string SNIPER_TRAIN_ID = "30007";
        private const string ELITE_SNIPER_TRAIN_ID = "31007";

        // 범위 스탯이 실효가 없는 포탑(기관총·전기·저격 계열 — 일반/엘리트)
        private static readonly HashSet<string> NoAreaTrainIds = new()
        {
            MACHINE_GUN_TRAIN_ID, ELITE_MACHINE_GUN_TRAIN_ID,
            ELECTRIC_TRAIN_ID, ELITE_ELECTRIC_TRAIN_ID,
            SNIPER_TRAIN_ID, ELITE_SNIPER_TRAIN_ID,
        };

        private readonly Train _train;
        private readonly StatType _statType;

        public TrainStatUpgradeChoice(Train targetTrain, StatType statType)
        {
            _train = targetTrain;
            _statType = statType;
        }

        public Train TargetTrain => _train;
        public StatType StatType => _statType;

        public string Id => $"StatUpgrade_{(_train != null && _train.TrainData != null ? _train.TrainData.Id : "?")}_{_statType}";

        // 만렙 전까지 반복 구매 가능.
        public bool IsRepeatable => true;

        // 보유 포탑 하나가 상점에 낼 수 있는 스탯 강화 선택지 목록.
        // 공격력은 공통, 포격은 공속 대신 공격 횟수(묵직한 발사 리듬 유지 — 포탑 리워크 플랜), 전기는 타겟 수 추가.
        public static List<TrainStatUpgradeChoice> CreateOptionsFor(Train train)
        {
            var options = new List<TrainStatUpgradeChoice>();

            if (train == null || train.TrainData == null)
                return options;

            string trainId = train.TrainData.Id;

            options.Add(new TrainStatUpgradeChoice(train, StatType.AttackDamage));

            if (trainId == CANNON_TRAIN_ID || trainId == ELITE_CANNON_TRAIN_ID)
                options.Add(new TrainStatUpgradeChoice(train, StatType.AttackCount));
            else
                options.Add(new TrainStatUpgradeChoice(train, StatType.AttackInterval));

            if (trainId == ELECTRIC_TRAIN_ID || trainId == ELITE_ELECTRIC_TRAIN_ID)
                options.Add(new TrainStatUpgradeChoice(train, StatType.TargetCount));

            if (!NoAreaTrainIds.Contains(trainId))
                options.Add(new TrainStatUpgradeChoice(train, StatType.AttackArea));

            return options;
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

            // 레벨 = 받은 업그레이드 횟수(획득 0). 만렙이면 더 못 산다.
            return _train.CurrentLevel < Train.MAX_LEVEL;
        }

        public void Execute()
        {
            var mainTrain = TrainManager.Instance?.MainTrain;

            if (mainTrain == null || !IsValid())
                return;

            var upgradeData = _BuildUpgradeData();

            if (upgradeData != null)
                mainTrain.UpgradeTrain(_train.TrainData.Id, upgradeData);
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
                _ => null,
            };

            string deltaText = _statType switch
            {
                StatType.AttackDamage => $"+{_GetBaseAttackDamage() * DAMAGE_UPGRADE_RATE:0.#}",
                StatType.AttackInterval => $"+{ATTACK_INTERVAL_REDUCE_RATE * 100f:0}%",
                StatType.AttackArea => $"+{ATTACK_AREA_UPGRADE_RATE * 100f:0}%",
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

        private ITrainUpgradeData _BuildUpgradeData()
        {
            if (_train is TurretTrain turretTrain)
            {
                var baseStatus = turretTrain.BaseStatus;
                var delta = new TurretTrainStatus();

                switch (_statType)
                {
                    case StatType.AttackDamage: delta.AttackDamage = baseStatus.AttackDamage * DAMAGE_UPGRADE_RATE; break;
                    case StatType.AttackInterval: delta.AttackInterval = -baseStatus.AttackInterval * ATTACK_INTERVAL_REDUCE_RATE; break;
                    case StatType.AttackArea: delta.AttackArea = baseStatus.AttackArea * ATTACK_AREA_UPGRADE_RATE; break;
                    case StatType.TargetCount: delta.TargetCount = 1; break;
                    case StatType.AttackCount: delta.AttackCount = 1; break;
                    default: return null;
                }

                return TurretTrainUpgradeData.CreateRuntimeSingleStat(Id, _train.TrainData.Name, delta, Train.MAX_LEVEL);
            }

            if (_train is RangeTrain rangeTrain)
            {
                var baseStatus = rangeTrain.BaseStatus;
                var delta = new RangeTrainStatus();

                switch (_statType)
                {
                    case StatType.AttackDamage: delta.AttackDamage = baseStatus.AttackDamage * DAMAGE_UPGRADE_RATE; break;
                    case StatType.AttackInterval: delta.AttackInterval = -baseStatus.AttackInterval * ATTACK_INTERVAL_REDUCE_RATE; break;
                    case StatType.AttackArea: delta.AttackArea = baseStatus.AttackArea * ATTACK_AREA_UPGRADE_RATE; break;
                    default: return null;
                }

                return RangeTrainUpgradeData.CreateRuntimeSingleStat(Id, _train.TrainData.Name, delta, Train.MAX_LEVEL);
            }

            return null;
        }
    }
}
