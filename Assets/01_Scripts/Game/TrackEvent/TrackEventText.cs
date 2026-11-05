using System.Text;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 선로 이벤트의 화면 표기 문구(이름 / 효과 줄)를 만든다. 선택 카드와 날씨 배너가 같이 쓴다.
    /// 효과 줄의 숫자는 데이터 배율(magnitude·subMagnitude)에서 계산하므로 수치를 바꿔도 문구가 따라간다.
    /// 부호는 ASCII "+"/"-" — RightToLeftTextPreprocessor가 숫자와 한 덩어리로 묶어 아랍어에서도 뒤집히지 않는다.
    /// </summary>
    public static class TrackEventText
    {
        private const string STAT_KEY_KILL_GOLD = "TrackEvent_Stat_KillGold";
        private const string STAT_KEY_KILL_EXP = "TrackEvent_Stat_KillExp";
        private const string STAT_KEY_MONSTER_SPEED = "TrackEvent_Stat_MonsterSpeed";
        private const string STAT_KEY_ATTACK_SPEED = "TrackEvent_Stat_AttackSpeed";
        private const string STAT_KEY_RANGE = "TrackEvent_Stat_Range";
        private const string STAT_KEY_REPAIR = "TrackEvent_Stat_Repair";
        private const string STAT_KEY_REMAINING_TIME = "TrackEvent_Stat_RemainingTime";
        private const string EFFECT_KEY_MONSTER_RUSH = "TrackEvent_Effect_MonsterRush";
        private const string EFFECT_KEY_ELITE_AMBUSH = "TrackEvent_Effect_EliteAmbush";
        private const string EFFECT_KEY_SUPPLY_DROP = "TrackEvent_Effect_SupplyDrop";
        private const string EFFECT_KEY_GOLDEN_MONSTER = "TrackEvent_Effect_GoldenMonster";
        private const string EFFECT_KEY_SUPPORT_BOMBARD = "TrackEvent_Effect_SupportBombard";

        private static readonly StringBuilder _builder = new StringBuilder();

        public static string GetName(TrackEventData data)
        {
            if (data == null)
                return string.Empty;

            return LocalizeHelper.GetByKey(data.NameKey, data.Id);
        }

        /// <summary>효과 줄. 효과가 둘이면 separator로 잇는다. 표기 규칙이 없는 타입은 빈 문자열.</summary>
        public static string GetEffectText(TrackEventData data, string separator)
        {
            if (data == null)
                return string.Empty;

            _builder.Clear();

            switch (data.EventType)
            {
                case TrackEventType.Rain:
                    _AppendMultiplier(STAT_KEY_MONSTER_SPEED, "몬스터 이동 속도", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_RANGE, "포탑 사거리", data.SubMagnitude, separator);
                    break;
                case TrackEventType.Fog:
                    _AppendMultiplier(STAT_KEY_RANGE, "포탑 사거리", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_KILL_EXP, "처치 경험치", data.SubMagnitude, separator);
                    break;
                case TrackEventType.Heatwave:
                    _AppendMultiplier(STAT_KEY_ATTACK_SPEED, "포탑 공격 속도", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_MONSTER_SPEED, "몬스터 이동 속도", data.SubMagnitude, separator);
                    break;
                case TrackEventType.Sandstorm:
                case TrackEventType.ChoiceRange:
                    _AppendMultiplier(STAT_KEY_RANGE, "포탑 사거리", data.Magnitude, separator);
                    break;
                case TrackEventType.ColdSnap:
                case TrackEventType.ChoiceMonsterSlow:
                    _AppendMultiplier(STAT_KEY_MONSTER_SPEED, "몬스터 이동 속도", data.Magnitude, separator);
                    break;
                case TrackEventType.Overdrive:
                case TrackEventType.ChoiceAttackSpeed:
                    _AppendMultiplier(STAT_KEY_ATTACK_SPEED, "포탑 공격 속도", data.Magnitude, separator);
                    break;
                case TrackEventType.ChoiceGoldBonus:
                    _AppendMultiplier(STAT_KEY_KILL_GOLD, "처치 골드", data.Magnitude, separator);
                    break;
                case TrackEventType.ExpBoost:
                case TrackEventType.ChoiceExpBonus:
                    _AppendMultiplier(STAT_KEY_KILL_EXP, "처치 경험치", data.Magnitude, separator);
                    break;
                case TrackEventType.RepairStop:
                case TrackEventType.ChoiceRepair:
                    // magnitude = 최대 체력 대비 회복 비율 (0.1 → +10%)
                    _AppendPercent(STAT_KEY_REPAIR, "기차 체력 회복", Mathf.RoundToInt(data.Magnitude * 100f), separator);
                    break;
                case TrackEventType.MonsterRush:
                    // magnitude = 스폰 간격 배율 (0.5 → 등장 2배)
                    _AppendFormatted(EFFECT_KEY_MONSTER_RUSH, "몬스터 등장 {0}배", data.Magnitude > 0f ? 1f / data.Magnitude : 1f);
                    break;
                case TrackEventType.EliteAmbush:
                    _AppendFormatted(EFFECT_KEY_ELITE_AMBUSH, "처치 시 <sprite name=\"EliteCoin\"> {0}배", Effects.EliteAmbushTrackEventEffect.GetRewardMultiplier(data));
                    break;
                case TrackEventType.SupplyDrop:
                    _builder.Append(LocalizeHelper.GetByKey(EFFECT_KEY_SUPPLY_DROP, "눌러서 골드 획득"));
                    break;
                case TrackEventType.GoldenMonster:
                    _builder.Append(LocalizeHelper.GetByKey(EFFECT_KEY_GOLDEN_MONSTER, "처치 시 많은 골드"));
                    break;
                case TrackEventType.SupportBombard:
                    _builder.Append(LocalizeHelper.GetByKey(EFFECT_KEY_SUPPORT_BOMBARD, "화면 곳곳에 포격"));
                    break;
                case TrackEventType.Tailwind:
                    // magnitude = 남은 시간 단축 비율 (0.2 → -20%)
                    _AppendPercent(STAT_KEY_REMAINING_TIME, "남은 구간 시간", -Mathf.RoundToInt(data.Magnitude * 100f), separator);
                    break;
            }

            return _builder.ToString();
        }

        private static void _AppendFormatted(string effectKey, string fallback, float value)
        {
            _builder.Append(string.Format(LocalizeHelper.GetByKey(effectKey, fallback), value.ToString("0.#")));
        }

        private static void _AppendMultiplier(string statKey, string fallback, float multiplier, string separator)
        {
            if (multiplier <= 0f || Mathf.Approximately(multiplier, 1f))
                return;

            _AppendPercent(statKey, fallback, Mathf.RoundToInt((multiplier - 1f) * 100f), separator);
        }

        private static void _AppendPercent(string statKey, string fallback, int percent, string separator)
        {
            if (percent == 0)
                return;

            if (_builder.Length > 0)
                _builder.Append(separator);

            _builder.Append(LocalizeHelper.GetByKey(statKey, fallback));
            _builder.Append(' ');
            _builder.Append(percent > 0 ? "+" : "-");
            _builder.Append(Mathf.Abs(percent));
            _builder.Append('%');
        }
    }
}
