using System.Text;
using TMPro;

namespace TrainDefense.Localize
{
    // TMP의 isRightToLeftText는 양방향(bidi) 알고리즘 없이 문자열을 글자 순서 그대로 오른쪽→왼쪽으로 놓는다.
    // 그래서 아랍어 문장 속 숫자·라틴 단어("30$", "Free")까지 뒤집혀 보인다("$03", "eerF").
    // RTL 언어일 때만 TMP에 붙여, 숫자·라틴 구간을 미리 역순으로 바꿔 두면 TMP가 다시 뒤집어 제대로 보인다.
    // 리치 텍스트 태그(<color>, <size>, <sprite> 등)는 손대지 않는다.
    public class RightToLeftTextPreprocessor : ITextPreprocessor
    {
        public static readonly RightToLeftTextPreprocessor Shared = new();

        private const int MAX_TAG_LENGTH = 128;

        // 숫자 앞에 붙는 기호("$30", "+5", "-5")와 뒤에 붙는 기호("30$", "5%")는 숫자와 한 덩어리로 본다.
        private const string LEADING_ATTACHED_SYMBOLS = "$+-";
        private const string TRAILING_ATTACHED_SYMBOLS = "$%";
        // 구간 안쪽에서만 허용되는 구분 기호("1,000", "1.5", "10:30", "a/b"). 구간 양 끝에 오면 구간에서 뺀다.
        private const string INNER_SEPARATORS = ".,:/ ";

        [System.ThreadStatic] private static StringBuilder _builder;

        public string PreprocessText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            _builder ??= new StringBuilder(text.Length);
            _builder.Clear();

            int index = 0;
            while (index < text.Length)
            {
                char current = text[index];

                if (current == '<')
                {
                    int tagEnd = _FindTagEnd(text, index);
                    if (tagEnd > index)
                    {
                        _builder.Append(text, index, tagEnd - index + 1);
                        index = tagEnd + 1;
                        continue;
                    }
                }

                if (!_IsRunCharacter(current))
                {
                    _builder.Append(current);
                    index++;
                    continue;
                }

                int runEnd = index;
                while (runEnd < text.Length && _IsRunCharacter(text[runEnd])) runEnd++;

                _AppendReversedRun(text, index, runEnd);
                index = runEnd;
            }

            return _builder.ToString();
        }

        // [start, end) 구간에서 양 끝의 느슨한 기호를 뺀 뒤, 글자·숫자가 있으면 그 부분만 역순으로 붙인다.
        private static void _AppendReversedRun(string text, int start, int end)
        {
            int coreStart = start;
            while (coreStart < end && !_IsStrong(text[coreStart]) && LEADING_ATTACHED_SYMBOLS.IndexOf(text[coreStart]) < 0)
                coreStart++;

            int coreEnd = end;
            while (coreEnd > coreStart && !_IsStrong(text[coreEnd - 1]) && TRAILING_ATTACHED_SYMBOLS.IndexOf(text[coreEnd - 1]) < 0)
                coreEnd--;

            bool hasStrong = false;
            for (int i = coreStart; i < coreEnd; i++)
            {
                if (_IsStrong(text[i])) { hasStrong = true; break; }
            }

            if (!hasStrong)
            {
                _builder.Append(text, start, end - start);
                return;
            }

            _builder.Append(text, start, coreStart - start);
            for (int i = coreEnd - 1; i >= coreStart; i--) _builder.Append(text[i]);
            _builder.Append(text, coreEnd, end - coreEnd);
        }

        // 왼쪽→오른쪽으로 읽어야 하는 글자: RTL 문자권(히브리·아랍)이 아닌 모든 글자와 숫자. 아랍 문자권 숫자(٠~٩, ۰~۹)도 숫자는 왼쪽→오른쪽.
        private static bool _IsStrong(char c)
        {
            if (c >= '0' && c <= '9') return true;
            if (c >= '٠' && c <= '٩') return true;
            if (c >= '۰' && c <= '۹') return true;
            return char.IsLetter(c) && !_IsRightToLeftLetter(c);
        }

        // 히브리(0590~05FF)·아랍(0600~06FF, 0750~077F, 08A0~08FF)·아랍 표시형(FB1D~FDFF, FE70~FEFF) 블록.
        private static bool _IsRightToLeftLetter(char c)
        {
            return (c >= '֐' && c <= 'ۿ')
                || (c >= 'ݐ' && c <= 'ݿ')
                || (c >= 'ࢠ' && c <= 'ࣿ')
                || (c >= 'יִ' && c <= '﷿')
                || (c >= 'ﹰ' && c <= '﻿');
        }

        private static bool _IsRunCharacter(char c)
        {
            if (c == '<') return false;
            return _IsStrong(c)
                || LEADING_ATTACHED_SYMBOLS.IndexOf(c) >= 0
                || TRAILING_ATTACHED_SYMBOLS.IndexOf(c) >= 0
                || INNER_SEPARATORS.IndexOf(c) >= 0;
        }

        // '<'에서 시작하는 리치 텍스트 태그의 '>' 위치. 태그가 아니면 -1.
        private static int _FindTagEnd(string text, int openIndex)
        {
            int limit = System.Math.Min(text.Length, openIndex + MAX_TAG_LENGTH);
            if (openIndex + 1 >= limit) return -1;

            char first = text[openIndex + 1];
            if (!(char.IsLetter(first) || first == '/' || first == '#')) return -1;

            for (int i = openIndex + 1; i < limit; i++)
            {
                if (text[i] == '<') return -1;
                if (text[i] == '>') return i;
            }

            return -1;
        }
    }
}
