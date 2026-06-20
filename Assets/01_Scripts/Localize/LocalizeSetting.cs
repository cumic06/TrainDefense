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
