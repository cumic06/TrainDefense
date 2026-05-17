using UnityEngine;

namespace TrainDefense.Localize
{
    public class LocalizeSetting : ScriptableObject
    {
        [Header("Spreadsheet Settings")]
        public string sheetURL = "";

        [Header("Runtime Mode")]
        public bool useRuntimeDownload = false;

        [Header("Generated Data")]
        public TextAsset localizedTextAsset;
    }
}
