using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TrainDefense.Localize
{
    public enum LocalizeSheet
    {
        UI,
        Monster,
        Train,
        Skill,
        Upgrade,
        PermanentUpgrade,
    }

    [Serializable]
    public class LocalizeSheetEntry
    {
        public LocalizeSheet sheet;
        public string sheetURL;
        public TextAsset bakedTextAsset;
    }

    [Serializable]
    public class LanguageFontEntry
    {
        [Tooltip("Localization 헤더명과 동일한 언어코드(예: Japanese, ChineseSimplified, Arabic, Thai, Russian)")]
        public string langCode;
        public TMP_FontAsset font;
    }

    [Serializable]
    public class LanguageDisplayName
    {
        [Tooltip("Localization 헤더명과 동일한 언어코드(예: Korean, English, es-419, pt-BR)")]
        public string langCode;
        [Tooltip("언어 선택 드롭다운에 표시할 이름(예: 한국어, English, 日本語)")]
        public string displayName;
    }

    public class LocalizeSetting : ScriptableObject
    {
        [Header("Spreadsheet Settings")]
        public List<LocalizeSheetEntry> sheets = new();

        [Header("Runtime Mode")]
        public bool useRuntimeDownload = false;

        [Header("Language")]
        public SystemLanguage defaultLanguage = SystemLanguage.Korean;

        [Header("Font per Language")]
        [Tooltip("언어코드별 폰트. 비어 있으면 해당 TMP의 기본 폰트를 사용한다.")]
        public List<LanguageFontEntry> languageFonts = new();

        [Tooltip("(구버전 호환) 일본어 폰트. languageFonts에 Japanese 항목이 없을 때 사용한다.")]
        public TMP_FontAsset japaneseFontAsset;

        [Header("Language Display Names (드롭다운 표시명)")]
        [Tooltip("언어코드별 화면 표시명. 등록하지 않으면 언어코드를 그대로 표시한다.")]
        public List<LanguageDisplayName> displayNames = new();

        /// <summary>
        /// 언어코드의 화면 표시명을 반환한다. 매핑이 없으면 언어코드 그대로 반환.
        /// </summary>
        public string GetDisplayName(string langCode)
        {
            if (string.IsNullOrEmpty(langCode))
                return langCode;

            if (displayNames != null)
            {
                foreach (var entry in displayNames)
                {
                    if (entry != null && entry.langCode == langCode)
                        return string.IsNullOrEmpty(entry.displayName) ? langCode : entry.displayName;
                }
            }

            return langCode;
        }

        /// <summary>
        /// 언어코드에 지정된 폰트를 반환한다. 없으면 null(기본 폰트 사용).
        /// </summary>
        public TMP_FontAsset GetFont(string langCode)
        {
            if (string.IsNullOrEmpty(langCode))
                return null;

            if (languageFonts != null)
            {
                foreach (var entry in languageFonts)
                {
                    if (entry != null && entry.langCode == langCode)
                        return entry.font;
                }
            }

            // 구버전 호환: 테이블에 Japanese 항목이 없으면 단일 일본어 폰트 사용.
            if (langCode == "Japanese")
                return japaneseFontAsset;

            return null;
        }
    }
}
