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

        // key 이름 → 언어코드(문자열) → 텍스트.
        // 언어코드는 TSV 헤더명을 그대로 사용한다("Korean","English","Japanese","ChineseSimplified","es-419","pt-BR" 등).
        // SystemLanguage enum으로 표현 못 하는 지역 변형(중남미 스페인어, 브라질 포르투갈어, 말레이어 등)까지 수용하기 위해 문자열 키를 쓴다.
        private static Dictionary<string, Dictionary<string, string>> _cache = new();
        private static bool _isInitialized = false;
        private static string _langCodeOverride = null;

        // TSV 헤더에서 수집한 사용 가능한 언어코드 목록(등장 순서 유지). 드롭다운 등 언어 선택 UI에서 사용.
        private static readonly List<string> _availableCodes = new();

        // 구버전: (int)SystemLanguage 저장. 신버전: 언어코드 문자열 저장.
        private const string LanguagePrefKey = "localize_language";
        private const string LangCodePrefKey = "localize_langcode";

        public static bool IsInitialized => _isInitialized;

        // 데이터에 존재하는 언어코드 목록(헤더에서 자동 수집). 시트에 언어 컬럼을 추가하면 자동 반영된다.
        public static IReadOnlyList<string> AvailableLanguageCodes => _availableCodes;

        // 현재 언어코드(문자열). override가 없으면 OS 언어를 코드로 매핑한다.
        public static string CurrentLanguageCode => _langCodeOverride ?? SystemLanguageToCode(Application.systemLanguage);

        // ── 하위 호환 API (기존 코드가 SystemLanguage로 비교/전달하던 부분 유지) ──
        public static SystemLanguage? CurrentOverride
            => _langCodeOverride == null ? (SystemLanguage?)null : CodeToSystemLanguage(_langCodeOverride);
        public static SystemLanguage CurrentLanguage => CodeToSystemLanguage(CurrentLanguageCode);

        public static LocalizeSetting Setting => _setting;
        public static event Action OnLanguageChanged;
        public static event Action OnInitialized;

        // 언어코드 직접 지정(31개 언어/지역변형 지원).
        public static void SetLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return;
            _langCodeOverride = langCode;
            PlayerPrefs.SetString(LangCodePrefKey, langCode);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        // 하위 호환: SystemLanguage로 호출하던 곳을 그대로 지원.
        public static void SetLanguage(SystemLanguage language) => SetLanguage(SystemLanguageToCode(language));

        public static void ClearLanguageOverride()
        {
            _langCodeOverride = null;
            PlayerPrefs.DeleteKey(LangCodePrefKey);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        // SystemLanguage → 언어코드. TSV 헤더명과 일치하도록 enum 이름을 그대로 쓴다.
        public static string SystemLanguageToCode(SystemLanguage lang) => lang.ToString();

        // 언어코드 → SystemLanguage(호환용). 지역변형 등 매칭 실패 시 Unknown.
        public static SystemLanguage CodeToSystemLanguage(string code)
            => Enum.TryParse(code, out SystemLanguage lang) ? lang : SystemLanguage.Unknown;

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
            _availableCodes.Clear();

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
                _LoadSavedLanguage();
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
                _LoadSavedLanguage();
                Debug.Log("<color=green>[Localization] 런타임 다운로드 초기화 완료</color>");
                OnInitialized?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void _LoadSavedLanguage()
        {
            // 신버전(문자열) 우선, 없으면 구버전(int SystemLanguage)에서 마이그레이션.
            if (PlayerPrefs.HasKey(LangCodePrefKey))
            {
                _langCodeOverride = PlayerPrefs.GetString(LangCodePrefKey);
            }
            else if (PlayerPrefs.HasKey(LanguagePrefKey))
            {
                _langCodeOverride = SystemLanguageToCode((SystemLanguage)PlayerPrefs.GetInt(LanguagePrefKey));
            }
        }

        private static void ParseTSV(string tsv)
        {
            string[] lines = tsv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 1) return;

            string[] headers = lines[0].Split('\t');
            // 컬럼 인덱스 → 언어코드(헤더명 그대로). SystemLanguage 매칭 강제하지 않아 임의 언어 추가 가능.
            var headerMap = new Dictionary<int, string>();

            for (int i = 1; i < headers.Length; i++)
            {
                string headerName = headers[i].Trim();
                if (!string.IsNullOrEmpty(headerName))
                {
                    headerMap[i] = headerName;
                    if (!_availableCodes.Contains(headerName))
                        _availableCodes.Add(headerName);
                }
            }

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cols = lines[i].Split('\t');
                string keyName = cols[0].Trim();
                if (string.IsNullOrEmpty(keyName)) continue;

                var langDict = new Dictionary<string, string>();
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
                string code = CurrentLanguageCode;
                if (languages.TryGetValue(code, out string text) && !string.IsNullOrEmpty(text)) return text;
                if (languages.TryGetValue("Korean", out string korean)) return korean;
                if (languages.TryGetValue("English", out string english)) return english;
                foreach (var val in languages.Values) return val;
            }

            return null;
        }

        // LocalizeKey enum 기반 접근 (에디터 자동완성 용도)
        public static string Get(LocalizeKey key) => GetByKey(key.ToString());
    }
}
