using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TrainDefense.Localize
{
    public static class Localization
    {
        private static LocalizeSetting _setting;

        // key 이름 → 언어별 텍스트
        private static Dictionary<string, Dictionary<SystemLanguage, string>> _cache = new();
        private static bool _isInitialized = false;
        private static SystemLanguage? _languageOverride = null;

        public static bool IsInitialized => _isInitialized;
        public static SystemLanguage? CurrentOverride => _languageOverride;
        public static event Action OnLanguageChanged;
        public static event Action OnInitialized;

        public static void SetLanguage(SystemLanguage language)
        {
            _languageOverride = language;
            OnLanguageChanged?.Invoke();
        }

        public static void ClearLanguageOverride()
        {
            _languageOverride = _setting?.defaultLanguage;
            OnLanguageChanged?.Invoke();
        }

        public static async UniTask InitializeAsync()
        {
            _setting = Resources.Load<LocalizeSetting>("LocalizeSetting");
            if (_setting == null)
            {
                Debug.LogError("[Localization] LocalizeSetting을 찾을 수 없습니다. Import & Bake를 먼저 실행하세요.");
                return;
            }

            if (_setting.sheets == null || _setting.sheets.Count == 0)
            {
                Debug.LogError("[Localization] 시트 설정이 없습니다. Localize Manager에서 시트를 추가하세요.");
                return;
            }

            _cache.Clear();

            if (!_setting.useRuntimeDownload)
            {
                foreach (var sheet in _setting.sheets)
                {
                    if (sheet.bakedTextAsset == null)
                    {
                        Debug.LogWarning($"[Localization] '{sheet.sheet}' 시트의 로컬 데이터가 없습니다. Import & Bake를 실행하세요.");
                        continue;
                    }
                    ParseTSV(sheet.bakedTextAsset.text);
                }
                _isInitialized = true;
                _languageOverride = _setting.defaultLanguage;
                Debug.Log("<color=green>[Localization] 로컬 데이터 초기화 완료</color>");
                OnInitialized?.Invoke();
                return;
            }

            try
            {
                foreach (var sheet in _setting.sheets)
                {
                    if (string.IsNullOrEmpty(sheet.sheetURL))
                    {
                        Debug.LogWarning($"[Localization] '{sheet.sheet}' 시트 URL이 비어있습니다.");
                        continue;
                    }

                    using UnityWebRequest www = UnityWebRequest.Get(sheet.sheetURL);
                    await www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[Localization] '{sheet.sheet}' 다운로드 실패: {www.error}");
                        continue;
                    }

                    ParseTSV(www.downloadHandler.text);
                }
                _isInitialized = true;
                _languageOverride = _setting.defaultLanguage;
                Debug.Log("<color=green>[Localization] 런타임 다운로드 초기화 완료</color>");
                OnInitialized?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void ParseTSV(string tsv)
        {
            string[] lines = tsv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 1) return;

            string[] headers = lines[0].Split('\t');
            var headerMap = new Dictionary<int, SystemLanguage>();

            for (int i = 1; i < headers.Length; i++)
            {
                string headerName = headers[i].Trim();
                if (Enum.TryParse(headerName, true, out SystemLanguage lang))
                    headerMap[i] = lang;
                else
                    Debug.LogWarning($"[Localization] 헤더 '{headerName}'를 SystemLanguage로 변환할 수 없습니다.");
            }

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cols = lines[i].Split('\t');
                string keyName = cols[0].Trim();
                if (string.IsNullOrEmpty(keyName)) continue;

                var langDict = new Dictionary<SystemLanguage, string>();
                foreach (var pair in headerMap)
                {
                    if (pair.Key < cols.Length)
                        langDict[pair.Value] = cols[pair.Key];
                }
                _cache[keyName] = langDict;
            }
        }

        public static string GetByKey(string keyName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(keyName)) return null;

            if (_cache.TryGetValue(keyName, out var languages))
            {
                SystemLanguage currentLang = _languageOverride ?? Application.systemLanguage;
                if (languages.TryGetValue(currentLang, out string text)) return text;
                if (languages.TryGetValue(SystemLanguage.Korean, out string korean)) return korean;
                if (languages.TryGetValue(SystemLanguage.English, out string english)) return english;
                foreach (var val in languages.Values) return val;
            }

            return null;
        }

        // LocalizeKey enum 기반 접근 (에디터 자동완성 용도)
        public static string Get(LocalizeKey key) => GetByKey(key.ToString());
    }
}
