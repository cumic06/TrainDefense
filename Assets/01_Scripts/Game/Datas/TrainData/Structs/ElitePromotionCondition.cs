using System;
using System.Collections.Generic;
using System.Globalization;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    public enum ElitePromotionComparison
    {
        AtLeast,   // 현재값 >= Value
        AtMost,    // 현재값 <= Value
    }

    public enum ElitePromotionSource
    {
        Stat,          // 포탑의 현재 스탯(StatType)
        CombatRecord,  // 이번 판 전투 기록(TrainCombatRecordType) — 처치 수·둔화/기절 횟수 등
    }

    /// <summary>
    /// 엘리트 승격 조건 — 원본 포탑의 현재(업그레이드 반영) 스탯 또는 이번 판 전투 기록이 기준을 만족해야 상점에 엘리트 카드가 뜬다.
    /// 엘리트 포탑 데이터(31xxx·41xxx)에 실리며, 시트 표기는 "AttackDamage>=100;AttackInterval<=0.8;Kills>=300"
    /// (StatType 또는 TrainCombatRecordType 이름, >= 또는 <=, 값을 ;로 나열). 비어 있으면 조건 없이 승격할 수 있다.
    /// 전투 기록 이름: Kills(처치 수) / Slows(둔화 횟수) / Stuns(기절 횟수) / Attacks(공격 횟수) / CriticalHits(치명타 횟수) / DamageDealt(준 피해).
    /// </summary>
    [Serializable]
    public struct ElitePromotionCondition
    {
        // 기존 데이터는 필드가 없어 기본값(Stat)으로 읽혀 그대로 스탯 조건으로 동작한다.
        public ElitePromotionSource Source;
        public StatType StatType;
        public TrainCombatRecordType RecordType;
        public ElitePromotionComparison Comparison;
        public float Value;

        // 공격 간격처럼 나눗셈 누적으로 만들어지는 값이 0.8000001로 기준을 살짝 넘겨 탈락하지 않도록 둔다.
        private const float COMPARE_TOLERANCE = 0.001f;
        private const string AT_LEAST_TOKEN = ">=";
        private const string AT_MOST_TOKEN = "<=";
        private const char CONDITION_SEPARATOR = ';';

        public bool IsSatisfiedBy(Train train)
        {
            if (train == null) return false;

            float current = GetCurrentValue(train);

            return Comparison == ElitePromotionComparison.AtLeast
                ? current >= Value - COMPARE_TOLERANCE
                : current <= Value + COMPARE_TOLERANCE;
        }

        /// <summary>조건이 보는 원본 포탑의 현재값 — 스탯 조건이면 현재 스탯, 전투 기록 조건이면 누적 기록.</summary>
        public float GetCurrentValue(Train train)
        {
            if (train == null) return 0f;

            return Source == ElitePromotionSource.CombatRecord
                ? train.CombatRecord.GetValue(RecordType)
                : train.GetCurrentStatValue(StatType);
        }

        // 시트에 적는 이름(StatType 또는 TrainCombatRecordType 이름).
        public string ConditionName => Source == ElitePromotionSource.CombatRecord ? RecordType.ToString() : StatType.ToString();

        /// <summary>시트 문자열 → 조건 배열. 형식이 맞지 않는 항목은 건너뛴다.</summary>
        public static ElitePromotionCondition[] ParseList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<ElitePromotionCondition>();

            var result = new List<ElitePromotionCondition>();

            foreach (var token in raw.Split(CONDITION_SEPARATOR))
            {
                if (TryParse(token, out var condition))
                    result.Add(condition);
            }

            return result.ToArray();
        }

        public static bool TryParse(string token, out ElitePromotionCondition condition)
        {
            condition = default;

            if (string.IsNullOrWhiteSpace(token)) return false;

            string trimmed = token.Trim();
            int operatorIndex = trimmed.IndexOf(AT_LEAST_TOKEN, StringComparison.Ordinal);
            var comparison = ElitePromotionComparison.AtLeast;

            if (operatorIndex < 0)
            {
                operatorIndex = trimmed.IndexOf(AT_MOST_TOKEN, StringComparison.Ordinal);
                comparison = ElitePromotionComparison.AtMost;
            }

            if (operatorIndex <= 0) return false;

            string statName = trimmed.Substring(0, operatorIndex).Trim();
            string valueText = trimmed.Substring(operatorIndex + AT_LEAST_TOKEN.Length).Trim();

            if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return false;

            // 스탯 이름을 먼저 보고, 아니면 전투 기록 이름으로 본다(기존 시트의 스탯 조건 해석은 그대로).
            if (Enum.TryParse(statName, true, out StatType statType))
            {
                condition = new ElitePromotionCondition { Source = ElitePromotionSource.Stat, StatType = statType, Comparison = comparison, Value = value };

                return true;
            }

            if (Enum.TryParse(statName, true, out TrainCombatRecordType recordType))
            {
                condition = new ElitePromotionCondition { Source = ElitePromotionSource.CombatRecord, RecordType = recordType, Comparison = comparison, Value = value };

                return true;
            }

            return false;
        }

        /// <summary>조건 배열 → 시트 문자열(ParseList의 역). 시트 덮어쓰기 툴이 쓴다.</summary>
        public static string ToSheetString(ElitePromotionCondition[] conditions)
        {
            if (conditions == null || conditions.Length == 0) return string.Empty;

            var parts = new string[conditions.Length];

            for (int i = 0; i < conditions.Length; i++)
            {
                string comparisonToken = conditions[i].Comparison == ElitePromotionComparison.AtLeast ? AT_LEAST_TOKEN : AT_MOST_TOKEN;
                parts[i] = conditions[i].ConditionName + comparisonToken + conditions[i].Value.ToString(CultureInfo.InvariantCulture);
            }

            return string.Join(CONDITION_SEPARATOR.ToString(), parts);
        }
    }
}
