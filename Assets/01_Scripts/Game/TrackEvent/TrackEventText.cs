using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string STAT_KEY_KILL_GOLD_EXP = "TrackEvent_Stat_KillGoldExp";
        private const string STAT_KEY_MONSTER_SPEED = "TrackEvent_Stat_MonsterSpeed";
        private const string STAT_KEY_ATTACK_SPEED = "TrackEvent_Stat_AttackSpeed";
        private const string STAT_KEY_RANGE = "TrackEvent_Stat_Range";
        private const string STAT_KEY_TURRET_DAMAGE = "TrackEvent_Stat_TurretDamage";
        private const string STAT_KEY_REPAIR = "TrackEvent_Stat_Repair";
        private const string STAT_KEY_REMAINING_TIME = "TrackEvent_Stat_RemainingTime";
        private const string STAT_KEY_MONSTER_ATTACK = "TrackEvent_Stat_MonsterAttack";
        private const string STAT_KEY_MONSTER_HEALTH = "TrackEvent_Stat_MonsterHealth";
        private const string EFFECT_KEY_ELITE_SPAWN = "TrackEvent_Effect_EliteSpawn";
        private const string EFFECT_KEY_MONSTER_RUSH = "TrackEvent_Effect_MonsterRush";
        private const string EFFECT_KEY_ELITE_AMBUSH = "TrackEvent_Effect_EliteAmbush";
        private const string EFFECT_KEY_SUPPLY_DROP = "TrackEvent_Effect_SupplyDrop";
        private const string EFFECT_KEY_GOLDEN_MONSTER = "TrackEvent_Effect_GoldenMonster";
        private const string EFFECT_KEY_SUPPORT_BOMBARD = "TrackEvent_Effect_SupportBombard";
        private const string EFFECT_KEY_SAFE_PASS = "TrackEvent_Effect_SafePass";
        private const string DIRECTION_KEY_INCREASE = "TrackEvent_Direction_Increase";
        private const string DIRECTION_KEY_DECREASE = "TrackEvent_Direction_Decrease";
        // 수치가 항목 이름과 다른 줄로 떨어지지 않게 묶는 태그 (_AppendWithValueBound)
        private const string NO_BREAK_OPEN_TAG = "<nobr>";
        private const string NO_BREAK_CLOSE_TAG = "</nobr>";
        // 번역 문장 틀의 {0} 자리를 먼저 채워 값 위치를 찾는 표시(문구에 나오지 않는 제어 문자)
        private const string VALUE_MARKER = "\u0001";
        // 엘리트 코인 아이콘은 그림 아래쪽에 그림자가 있어 글자 줄 가운데에 두면 내려가 보인다 — 글자 크기 비례로 살짝 올린다.
        private const string ELITE_COIN_ICON_FORMAT = "<size={0}%><voffset=0.12em><sprite name=\"EliteCoin\"></voffset></size>";

        private static readonly StringBuilder _builder = new StringBuilder();
        private static bool _isDirectionOnly;
        private static bool _withSubject;

        // 주체를 붙여 쓸 때 바꿔 쓰는 키. 적 항목은 원래 키에 "적"이 들어 있어 그대로 쓴다.
        private static readonly Dictionary<string, string> _subjectStatKeys = new Dictionary<string, string>
        {
            { STAT_KEY_RANGE, "TrackEvent_Banner_Range" },
            { STAT_KEY_ATTACK_SPEED, "TrackEvent_Banner_AttackSpeed" },
            { STAT_KEY_TURRET_DAMAGE, "TrackEvent_Banner_TurretDamage" },
        };

        public static string GetName(TrackEventData data)
        {
            if (data == null)
                return string.Empty;

            return LocalizeHelper.GetByKey(data.NameKey, data.Id);
        }

        /// <summary>효과 줄. 효과가 둘이면 separator로 잇는다. 표기 규칙이 없는 타입은 빈 문자열.
        /// iconSizePercent는 문구 속 아이콘(엘리트 코인) 크기 — TMP의 % 크기는 겉의 size 태그를 따르지 않아 화면마다 따로 정한다.
        /// withSubject면 포탑 항목 앞에 주체(포탑)를 붙인다 — 카드는 짧게, 배너는 누가 바뀌는지 보이게.</summary>
        public static string GetEffectText(TrackEventData data, string separator, int iconSizePercent, bool withSubject = false)
        {
            if (data == null)
                return string.Empty;

            _builder.Clear();
            _withSubject = withSubject;
            // 날씨는 다른 이벤트와 겹쳐 쓰이므로 세기를 숫자로 보여 주지 않고 증가/감소만 알린다.
            _isDirectionOnly = data.Category == TrackEventCategory.Weather;

            switch (data.EventType)
            {
                case TrackEventType.Rain:
                    _AppendMultiplier(STAT_KEY_MONSTER_SPEED, "적 속도", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_RANGE, "사거리", data.SubMagnitude, separator);
                    break;
                case TrackEventType.Fog:
                    _AppendMultiplier(STAT_KEY_RANGE, "사거리", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_KILL_EXP, "처치 경험치", data.SubMagnitude, separator);
                    break;
                case TrackEventType.Heatwave:
                    _AppendMultiplier(STAT_KEY_TURRET_DAMAGE, "피해", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_MONSTER_SPEED, "적 속도", data.SubMagnitude, separator);
                    break;
                case TrackEventType.Sandstorm:
                case TrackEventType.ChoiceRange:
                    _AppendMultiplier(STAT_KEY_RANGE, "사거리", data.Magnitude, separator);
                    break;
                case TrackEventType.ColdSnap:
                case TrackEventType.ChoiceMonsterSlow:
                    _AppendMultiplier(STAT_KEY_MONSTER_SPEED, "적 속도", data.Magnitude, separator);
                    break;
                case TrackEventType.Overdrive:
                case TrackEventType.ChoiceAttackSpeed:
                    _AppendMultiplier(STAT_KEY_ATTACK_SPEED, "공격 속도", data.Magnitude, separator);
                    break;
                case TrackEventType.ChoiceTurretDamage:
                    _AppendMultiplier(STAT_KEY_TURRET_DAMAGE, "피해", data.Magnitude, separator);
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
                    // magnitude = 러시 동안의 전체 등장 배율 (2.5 → 등장 2.5배)
                    _AppendFormatted(EFFECT_KEY_MONSTER_RUSH, "적 등장 {0}배", Effects.MonsterRushTrackEventEffect.GetSpawnMultiplier(data));
                    break;
                case TrackEventType.DetourChoice:
                case TrackEventType.EngineOverload:
                    _AppendFormatted(EFFECT_KEY_MONSTER_RUSH, "적 등장 {0}배", Effects.DetourTrackEventEffect.GetSpawnMultiplier(data));
                    // 골드와 경험치가 같은 배율로 오르므로 한 줄로 쓴다.
                    _AppendMultiplier(STAT_KEY_KILL_GOLD_EXP, "골드, 경험치", Effects.DetourTrackEventEffect.GetRewardMultiplier(data), separator);

                    if (data.EventType == TrackEventType.EngineOverload)
                        _AppendPercent(STAT_KEY_REMAINING_TIME, "남은 구간 시간", -Mathf.RoundToInt(Effects.EngineOverloadTrackEventEffect.SHORTEN_RATIO * 100f), separator);
                    break;
                // 날씨 갈림길 — 줄 순서는 TrackEventEffectFactory의 첫 배율(magnitude)·두 번째 배율(subMagnitude) 순서와 같다
                case TrackEventType.SandstormClearView:
                    _AppendMultiplier(STAT_KEY_RANGE, "사거리", data.Magnitude, separator);
                    break;
                case TrackEventType.SandstormPushThrough:
                    _AppendMultiplier(STAT_KEY_MONSTER_SPEED, "적 속도", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_KILL_GOLD, "처치 골드", data.SubMagnitude, separator);
                    break;
                case TrackEventType.RainCover:
                    _AppendMultiplier(STAT_KEY_RANGE, "사거리", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_ATTACK_SPEED, "공격 속도", data.SubMagnitude, separator);
                    break;
                case TrackEventType.RainCooling:
                    _AppendMultiplier(STAT_KEY_ATTACK_SPEED, "공격 속도", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_RANGE, "사거리", data.SubMagnitude, separator);
                    break;
                case TrackEventType.FogMarch:
                    _AppendMultiplier(STAT_KEY_RANGE, "사거리", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_KILL_EXP, "처치 경험치", data.SubMagnitude, separator);
                    break;
                case TrackEventType.FogSearch:
                case TrackEventType.ColdSnapBlizzardPush:
                    _AppendMultiplier(STAT_KEY_RANGE, "사거리", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_KILL_GOLD, "처치 골드", data.SubMagnitude, separator);
                    break;
                case TrackEventType.HeatwaveOverheat:
                    _AppendMultiplier(STAT_KEY_TURRET_DAMAGE, "피해", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_MONSTER_SPEED, "적 속도", data.SubMagnitude, separator);
                    break;
                case TrackEventType.HeatwaveExhausted:
                    _AppendMultiplier(STAT_KEY_MONSTER_ATTACK, "적 공격력", data.Magnitude, separator);
                    _AppendMultiplier(STAT_KEY_MONSTER_HEALTH, "적 체력", data.SubMagnitude, separator);
                    break;
                case TrackEventType.HeatwaveElites:
                    _AppendFormatted(EFFECT_KEY_ELITE_SPAWN, "엘리트 등장 {0}배", Effects.HeatedEliteTrackEventEffect.GetEliteMultiplier(data));
                    _AppendMultiplier(STAT_KEY_ATTACK_SPEED, "공격 속도", data.SubMagnitude, separator);
                    break;
                case TrackEventType.ColdSnapIceSlide:
                    _AppendFormatted(EFFECT_KEY_MONSTER_RUSH, "적 등장 {0}배", Effects.DetourTrackEventEffect.GetSpawnMultiplier(data));
                    _AppendMultiplier(STAT_KEY_KILL_GOLD, "처치 골드", Effects.DetourTrackEventEffect.GetRewardMultiplier(data), separator);
                    break;
                case TrackEventType.SafePass:
                    _builder.Append(LocalizeHelper.GetByKey(EFFECT_KEY_SAFE_PASS, "변화 없음"));
                    break;
                case TrackEventType.EliteAmbush:
                    // {1} = 엘리트 코인 아이콘
                    _AppendFormatted(EFFECT_KEY_ELITE_AMBUSH, "처치 시 {1} {0}배", Effects.EliteAmbushTrackEventEffect.GetRewardMultiplier(data),
                        string.Format(ELITE_COIN_ICON_FORMAT, iconSizePercent));
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

        private static void _AppendFormatted(string effectKey, string fallback, float value, string icon = "")
        {
            // {0} = 값, {1} = 아이콘. 값 자리를 표시로 먼저 채워 위치를 찾은 뒤 값으로 바꾼다.
            string formatted = string.Format(LocalizeHelper.GetByKey(effectKey, fallback), VALUE_MARKER, icon);
            int valueIndex = formatted.IndexOf(VALUE_MARKER, StringComparison.Ordinal);

            if (valueIndex < 0)
            {
                _builder.Append(formatted);
                return;
            }

            // 기기 지역 설정과 상관없이 소수점은 '.' (다른 수치 표기와 같게)
            string valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
            string text = formatted.Replace(VALUE_MARKER, valueText);
            int valueEnd = valueIndex + valueText.Length;

            // 값 덩어리 끝: "1.3배"·"1.5倍"처럼 뒤에 붙은 단위까지
            while (valueEnd < text.Length && !char.IsWhiteSpace(text[valueEnd]) && text[valueEnd] != '<')
                valueEnd++;

            _AppendWithValueBound(text, valueIndex, valueEnd);
        }

        /// <summary>
        /// 값(valueIndex~valueEnd)이 앞 낱말과 다른 줄로 떨어지지 않게(예: "Enemy spawns / x1.3", "속도 / +5%") 앞 글자부터 nobr로 묶어 붙인다.
        /// 공백과 '-' 뒤가 모두 TMP의 줄바꿈 자리라 NBSP로는 못 막는다. 한중일 글자는 글자 사이마다 줄이 바뀔 수 있어
        /// 공백이 아니라 앞 낱말의 마지막 글자부터 묶는다. 앞이 태그(아이콘)면 태그 덩어리째 묶어 아이콘과 값도 붙어 다닌다.
        /// </summary>
        private static void _AppendWithValueBound(string text, int valueIndex, int valueEnd)
        {
            // 값 낱말의 시작: 값 앞에 붙은 "x"·"+" 같은 글자까지 (공백이나 태그 끝에서 멈춤)
            int wordStart = valueIndex;
            while (wordStart > 0 && !char.IsWhiteSpace(text[wordStart - 1]) && text[wordStart - 1] != '>')
                wordStart--;

            int bindStart;

            if (wordStart == 0)
                // 공백 없는 한중일 문장("敵出現1.3倍")이나 값이 맨 앞 — 값 바로 앞 글자 하나만 묶는다
                bindStart = valueIndex > 0 ? _PreviousElementStart(text, valueIndex) : 0;
            else if (text[wordStart - 1] == '>')
                bindStart = _TagRunStart(text, wordStart - 1);
            else
            {
                int beforeSpace = wordStart - 1;
                bindStart = beforeSpace > 0 && text[beforeSpace - 1] == '>'
                    ? _TagRunStart(text, beforeSpace - 1)
                    : beforeSpace > 0 ? _PreviousElementStart(text, beforeSpace) : beforeSpace;
            }

            _builder.Append(text, 0, bindStart);
            _builder.Append(NO_BREAK_OPEN_TAG);
            _builder.Append(text, bindStart, valueEnd - bindStart);
            _builder.Append(NO_BREAK_CLOSE_TAG);
            _builder.Append(text, valueEnd, text.Length - valueEnd);
        }

        // index 바로 앞 글자(태국어 결합 모음·서로게이트 쌍은 한 글자로)의 시작 위치
        private static int _PreviousElementStart(string text, int index)
        {
            int[] starts = StringInfo.ParseCombiningCharacters(text.Substring(0, index));

            return starts.Length > 0 ? starts[starts.Length - 1] : 0;
        }

        // tagEnd('>')에서 왼쪽으로 붙어 있는 태그들("<size><voffset><sprite></voffset></size>")의 첫 '<' 위치
        private static int _TagRunStart(string text, int tagEnd)
        {
            int start = tagEnd + 1;
            int position = tagEnd;

            while (position >= 0 && text[position] == '>')
            {
                int open = text.LastIndexOf('<', position);

                if (open < 0)
                    break;

                start = open;
                position = open - 1;
            }

            return start;
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

            string statName = _withSubject && _subjectStatKeys.TryGetValue(statKey, out string subjectKey)
                ? LocalizeHelper.GetByKey(subjectKey, "포탑 " + fallback)
                : LocalizeHelper.GetByKey(statKey, fallback);

            if (_isDirectionOnly)
            {
                _builder.Append(percent > 0
                    ? string.Format(LocalizeHelper.GetByKey(DIRECTION_KEY_INCREASE, "{0} 증가"), statName)
                    : string.Format(LocalizeHelper.GetByKey(DIRECTION_KEY_DECREASE, "{0} 감소"), statName));

                return;
            }

            string text = $"{statName} {(percent > 0 ? "+" : "-")}{Mathf.Abs(percent)}%";

            _AppendWithValueBound(text, statName.Length + 1, text.Length);
        }
    }
}
