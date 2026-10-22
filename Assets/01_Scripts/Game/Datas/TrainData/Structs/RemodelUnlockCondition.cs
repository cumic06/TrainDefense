using System;
using System.Collections.Generic;
using System.Globalization;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    public enum RemodelUnlockComparison
    {
        AtLeast,   // 현재값 >= Value
        AtMost,    // 현재값 <= Value
    }

    public enum RemodelUnlockSource
    {
        Stat,          // 포탑의 현재 스탯(StatType)
        CombatRecord,  // 이번 판 전투 기록(TrainCombatRecordType) — 처치 수·둔화/기절 횟수 등
    }

    /// <summary>
    /// 개조 해금 조건 — 한 판 안에서 원본 포탑(또는 이미 개조한 그 포탑)의 현재 스탯이나 전투 기록이 기준을 만족하면
    /// 판이 끝날 때 그 개조가 해금되어 다음 판부터 상점에 뜬다(RemodelUnlockEvaluator가 판정, UserDataManager가 저장).
    /// 개조 스킬 데이터(TrainPassiveSkillData)에 실리며, 시트 표기는 "AttackDamage>=100;Kills>=300"
    /// (StatType 또는 TrainCombatRecordType 이름, >= 또는 <=, 값을 ;로 나열). 비어 있으면 처음부터 해금돼 있다.
    /// 전투 기록 이름: Kills(처치 수) / Slows(둔화 횟수) / Stuns(기절 횟수) / Attacks(공격 횟수) / CriticalHits(치명타 횟수) / DamageDealt(준 피해).
    /// </summary>
    [Serializable]
    public struct RemodelUnlockCondition
    {
        public RemodelUnlockSource Source;
        public StatType StatType;
        public TrainCombatRecordType RecordType;
        public RemodelUnlockComparison Comparison;
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

            return Comparison == RemodelUnlockComparison.AtLeast
                ? current >= Value - COMPARE_TOLERANCE
                : current <= Value + COMPARE_TOLERANCE;
        }

        /// <summary>조건이 보는 포탑의 현재값 — 스탯 조건이면 현재 스탯, 전투 기록 조건이면 누적 기록.</summary>
        public float GetCurrentValue(Train train)
        {
            if (train == null) return 0f;

            return Source == RemodelUnlockSource.CombatRecord
                ? train.CombatRecord.GetValue(RecordType)
                : train.GetCurrentStatValue(StatType);
        }

        // 시트에 적는 이름(StatType 또는 TrainCombatRecordType 이름).
        public string ConditionName => Source == RemodelUnlockSource.CombatRecord ? RecordType.ToString() : StatType.ToString();

        /// <summary>시트 문자열 → 조건 배열. 형식이 맞지 않는 항목은 건너뛴다.</summary>
        public static RemodelUnlockCondition[] ParseList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<RemodelUnlockCondition>();

            var result = new List<RemodelUnlockCondition>();

            foreach (var token in raw.Split(CONDITION_SEPARATOR))
            {
                if (TryParse(token, out var condition))
                    result.Add(condition);
            }

            return result.ToArray();
        }

        public static bool TryParse(string token, out RemodelUnlockCondition condition)
        {
            condition = default;

            if (string.IsNullOrWhiteSpace(token)) return false;

            string trimmed = token.Trim();
            int operatorIndex = trimmed.IndexOf(AT_LEAST_TOKEN, StringComparison.Ordinal);
            var comparison = RemodelUnlockComparison.AtLeast;

            if (operatorIndex < 0)
            {
                operatorIndex = trimmed.IndexOf(AT_MOST_TOKEN, StringComparison.Ordinal);
                comparison = RemodelUnlockComparison.AtMost;
            }

            if (operatorIndex <= 0) return false;

            string statName = trimmed.Substring(0, operatorIndex).Trim();
            string valueText = trimmed.Substring(operatorIndex + AT_LEAST_TOKEN.Length).Trim();

            if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return false;

            // 스탯 이름을 먼저 보고, 아니면 전투 기록 이름으로 본다.
            if (Enum.TryParse(statName, true, out StatType statType))
            {
                condition = new RemodelUnlockCondition { Source = RemodelUnlockSource.Stat, StatType = statType, Comparison = comparison, Value = value };

                return true;
            }

            if (Enum.TryParse(statName, true, out TrainCombatRecordType recordType))
            {
                condition = new RemodelUnlockCondition { Source = RemodelUnlockSource.CombatRecord, RecordType = recordType, Comparison = comparison, Value = value };

                return true;
            }

            return false;
        }
    }
}
