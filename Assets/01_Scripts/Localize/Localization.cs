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

        // <행 번호(int), <언어, 텍스트>>
        private static Dictionary<int, Dictionary<SystemLanguage, string>> _cache = new();
        private static bool _isInitialized = false;
        private static SystemLanguage? _languageOverride = null;

        public static void SetLanguage(SystemLanguage language) => _languageOverride = language;
        public static void ClearLanguageOverride() => _languageOverride = null;
        public static bool IsInitialized => _isInitialized;

        public static async UniTask InitializeAsync()
        {
            _setting = Resources.Load<LocalizeSetting>("LocalizeSetting");
            if (_setting == null)
            {
                Debug.LogError("[Localization] LocalizeSetting을 찾을 수 없습니다. Import & Bake를 먼저 실행하세요.");
                return;
            }

            if (!_setting.useRuntimeDownload)
            {
                if (_setting.localizedTextAsset == null)
                {
                    Debug.LogError("[Localization] 로컬 데이터가 없습니다. Import & Bake를 실행하세요.");
                    return;
                }

                ParseTSV(_setting.localizedTextAsset.text);
                _isInitialized = true;
                Debug.Log("<color=green>[Localization] 로컬 데이터 초기화 완료</color>");
                return;
            }

            if (string.IsNullOrEmpty(_setting.sheetURL))
            {
                Debug.LogError("[Localization] 시트 URL이 비어있습니다.");
                return;
            }

            try
            {
                using UnityWebRequest www = UnityWebRequest.Get(_setting.sheetURL);
                await www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Localization] 다운로드 실패: {www.error}");
                    return;
                }

                ParseTSV(www.downloadHandler.text);
                _isInitialized = true;
                Debug.Log("<color=green>[Localization] 런타임 다운로드 초기화 완료</color>");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void ParseTSV(string tsv)
        {
            _cache.Clear();
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

                var langDict = new Dictionary<SystemLanguage, string>();
                foreach (var pair in headerMap)
                {
                    if (pair.Key < cols.Length)
                        langDict[pair.Value] = cols[pair.Key];
                }
                _cache[i] = langDict;
            }
        }

        public static string Get(int key)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[Localization] 초기화되지 않았습니다.");
                return key.ToString();
            }

            SystemLanguage currentLang = _languageOverride ?? Application.systemLanguage;

            if (_cache.TryGetValue(key, out var languages))
            {
                if (languages.TryGetValue(currentLang, out string text)) return text;
                if (languages.TryGetValue(SystemLanguage.Korean, out string koreanText)) return koreanText;
                if (languages.TryGetValue(SystemLanguage.English, out string englishText)) return englishText;
                foreach (var val in languages.Values) return val;
            }

            return key.ToString();
        }

        public static string GetByKey(string keyName)
        {
            if (!_isInitialized) return null;
            if (Enum.TryParse<LocalizeKey>(keyName, out var key))
                return Get((int)key);
            return null;
        }
    }
}
