using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TrainDefense.Localize
{
    public static class LocalizeHelper
    {
        private static readonly HashSet<string> _warnedKeys = new();

        // zero-width, 줄바꿈 금지 문자(U+2060 WORD JOINER). 인접 글자 사이에 넣으면 그 지점에서 줄이 나뉘지 않는다.
        private const char WordJoiner = (char)0x2060;

        public static string GetByKey(string keyName, string fallback)
        {
            string result = Localization.GetByKey(keyName);

            if (result == null && Localization.IsInitialized && !string.IsNullOrEmpty(keyName) && _warnedKeys.Add(keyName))
                Debug.LogWarning($"[Localize] 키 누락: '{keyName}' — fallback: '{fallback}'");

            return result ?? fallback;
        }

        /// <summary>
        /// 한글이 단어 중간(글자 단위)에서 줄바꿈되는 것을 막는다.
        /// TMP는 한글을 띄어쓰기가 아닌 글자 단위로 줄바꿈하기 때문에 "주황"이 "주/황",
        /// "줍니다."가 "줍니/다."처럼 깨진다. 띄어쓰기/개행에서만 줄이 나뉘도록 그 외 인접
        /// 글자 사이에 WordJoiner(U+2060, zero-width)를 삽입한다.
        /// 리치텍스트 태그(&lt;...&gt;)·플레이스홀더({...})는 통째로 보존하며, 태그는 조인 판정에서
        /// 투명하게 취급해 "&lt;color&gt;빨강&lt;/color&gt;으로"가 "빨강/으로"로 끊기지 않게 한다.
        /// 일본어·중국어는 글자 단위 줄바꿈이 정상이므로 한국어가 아닐 땐 원문을 그대로 반환한다.
        /// </summary>
        public static string ProtectWordBreak(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (Localization.CurrentLanguage != SystemLanguage.Korean) return text;

            var sb = new StringBuilder(text.Length * 2);
            bool prevJoinable = false; // 직전에 출력한 '보이는 일반 글자'가 있었는가 (공백/개행에서 false)

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // 태그/플레이스홀더는 통째로 복사. prevJoinable은 유지(태그는 조인에서 투명).
                if (c == '<' || c == '{')
                {
                    char close = c == '<' ? '>' : '}';
                    int end = text.IndexOf(close, i);
                    if (end < 0) end = text.Length - 1;
                    sb.Append(text, i, end - i + 1);
                    i = end;
                    continue;
                }

                // 공백/개행 = 줄바꿈 허용 지점 → 조인하지 않는다.
                if (c == ' ' || c == '\n' || c == '\r' || c == '\t')
                {
                    sb.Append(c);
                    prevJoinable = false;
                    continue;
                }

                if (prevJoinable)
                    sb.Append(WordJoiner);
                sb.Append(c);
                prevJoinable = true;
            }

            return sb.ToString();
        }
    }
}
