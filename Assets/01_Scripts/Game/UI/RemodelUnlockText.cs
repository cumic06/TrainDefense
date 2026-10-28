using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 개조 해금 조건(RemodelUnlockCondition)을 화면 글자로 옮긴다 — 도감 개조 탭과 포탑 상세창이 같은 라벨·형식을 쓴다.
    /// 라벨은 스탯/전투 기록 종류별 로컬라이즈 키, 값은 공격 간격만 초 단위 소수, 나머지는 정수 콤마.
    /// </summary>
    public static class RemodelUnlockText
    {
        /// <summary>조건이 보는 항목의 이름 — "처치 수", "누적 피해", "공격력" 등.</summary>
        public static string Label(RemodelUnlockCondition condition)
        {
            if (condition.Source == RemodelUnlockSource.CombatRecord)
            {
                return condition.RecordType switch
                {
                    TrainCombatRecordType.Kills => _Loc("Collection_Record_Kills", "처치 수"),
                    TrainCombatRecordType.Slows => _Loc("Collection_Record_Slows", "둔화 횟수"),
                    TrainCombatRecordType.Stuns => _Loc("Collection_Record_Stuns", "기절 횟수"),
                    TrainCombatRecordType.Attacks => _Loc("Collection_Record_Attacks", "공격 횟수"),
                    TrainCombatRecordType.CriticalHits => _Loc("Collection_Record_CriticalHits", "치명타 횟수"),
                    TrainCombatRecordType.DamageDealt => _Loc("Collection_Record_DamageDealt", "누적 피해"),
                    _ => condition.ConditionName,
                };
            }

            return condition.StatType switch
            {
                StatType.MaxHp => _Loc("Detail_HP", "체력"),
                StatType.AttackRange => _Loc("Detail_Range", "사거리"),
                StatType.AttackDamage => _Loc("Detail_Damage", "공격력"),
                StatType.AttackCount => _Loc("Collection_AttackCount", "공격 횟수"),
                StatType.AttackInterval => _Loc("Detail_Speed", "공격 속도"),
                StatType.TargetCount => _Loc("Detail_Targets", "타겟 수"),
                StatType.CriticalChance => _Loc("Detail_CritChance", "치명타 확률"),
                StatType.CriticalDamage => _Loc("Detail_CritDamage", "치명타 피해"),
                StatType.AttackArea => _Loc("Detail_Area", "공격 범위"),
                StatType.SlowRate => _Loc("Collection_Slow", "둔화"),
                _ => condition.ConditionName,
            };
        }

        /// <summary>도감용 — "100 이상" / "0.8 이하".</summary>
        public static string Requirement(RemodelUnlockCondition condition)
        {
            string format = condition.Comparison == RemodelUnlockComparison.AtLeast
                ? _Loc("Collection_AtLeast", "{0} 이상")
                : _Loc("Collection_AtMost", "{0} 이하");

            return string.Format(format, FormatValue(condition, condition.Value));
        }

        /// <summary>상세창용 진행도 — "누적 피해 1,230/25,000". 포탑이 없으면 현재값 0.</summary>
        public static string Progress(RemodelUnlockCondition condition, Train train)
        {
            float current = train != null ? condition.GetCurrentValue(train) : 0f;

            return $"{Label(condition)} {FormatValue(condition, current)}/{FormatValue(condition, condition.Value)}";
        }

        // 공격 간격(초)만 소수, 나머지는 정수에 천 단위 콤마. 스탯 값(공격력 24.5 등)은 소수가 있으면 둘째 자리까지.
        public static string FormatValue(RemodelUnlockCondition condition, float value)
        {
            if (condition.Source == RemodelUnlockSource.Stat && condition.StatType == StatType.AttackInterval)
                return $"{value:0.0#}";

            if (condition.Source == RemodelUnlockSource.CombatRecord)
                return value.ToString("#,0");

            return value.ToString("#,0.##");
        }

        private static string _Loc(string key, string fallback)
        {
            return LocalizeHelper.GetByKey(key, fallback);
        }
    }
}
