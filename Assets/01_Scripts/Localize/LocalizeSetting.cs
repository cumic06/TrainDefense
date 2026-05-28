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
    }

    [Serializable]
    public class LocalizeSheetEntry
    {
        public LocalizeSheet sheet;
        public string sheetURL;
        public TextAsset bakedTextAsset;
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
        public TMP_FontAsset japaneseFontAsset;
    }
}
