using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TrainDefense.Localize.EditorTools
{
    /// <summary>
    /// 다운로드해 둔 Noto 폰트(.ttf/.otf)를 TMP_FontAsset(Dynamic SDF)으로 변환하고,
    /// 실제 번역 텍스트에 쓰이는 글리프를 미리 베이크한 뒤 LocalizeSetting.languageFonts에 자동 등록한다.
    ///
    /// 글리프를 미리 베이크하는 이유: 기존 폰트(DNFBitBit 11,684자 / DotGothic16 7,168자)는
    /// Dynamic 모드여도 글리프가 전부 베이크돼 있다. 새 폰트를 글리프 0개로 두면 런타임 베이크에
    /// 의존하는데 그게 안 채워져 전부 □로 나온다. 그래서 LocalizeSource_*.txt에 실제 등장하는
    /// 문자만 골라 미리 베이크한다(245키 한정이라 아틀라스 폭발 없음).
    ///
    /// 사용법: Unity 메뉴 → TrainDefense/Localize/Create Noto Font Assets
    /// </summary>
    public static class NotoFontAssetCreator
    {
        private const string NotoDir = "Assets/02_Resources/Font/Noto";
        private const string SdfDir = "Assets/02_Resources/Font/Noto/SDF";
        private const string SettingPath =
            "Assets/TrainDefense_Generated/Localize/Resources/LocalizeSetting.asset";

        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1024;

        // 글리프 수집 대상 번역 시트(헤더의 언어코드 컬럼에서 문자를 모은다).
        private static readonly string[] SheetFiles =
        {
            "Assets/TrainDefense_Generated/Localize/LocalizeSource_UI.txt",
            "Assets/TrainDefense_Generated/Localize/LocalizeSource_Monster.txt",
            "Assets/TrainDefense_Generated/Localize/LocalizeSource_Train.txt",
            "Assets/TrainDefense_Generated/Localize/LocalizeSource_Skill.txt",
            "Assets/TrainDefense_Generated/Localize/LocalizeSource_Upgrade.txt",
            "Assets/TrainDefense_Generated/Localize/LocalizeSource_PermanentUpgrade.txt",
        };

        private class FontJob
        {
            public string sourcePath;
            public string outName;
            public string[] langCodes;
            public bool addLatinFallback; // 비라틴 폰트에 NotoSans(라틴/숫자) fallback 부여
            public bool collectAllColumns; // NotoSans는 전 컬럼 문자를 시도(라틴/숫자/태그/키릴/그리스 보강)
        }

        [MenuItem("TrainDefense/Localize/Create Noto Font Assets")]
        public static void CreateAll()
        {
            var jobs = new List<FontJob>
            {
                new FontJob
                {
                    sourcePath = $"{NotoDir}/NotoSans-Regular.ttf",
                    outName = "NotoSans SDF",
                    addLatinFallback = false,
                    collectAllColumns = true,
                    langCodes = new[]
                    {
                        "French", "Italian", "German", "es-ES", "es-419", "Greek",
                        "Dutch", "Norwegian", "Danish", "Russian", "Romanian", "Malay",
                        "Vietnamese", "Bulgarian", "Swedish", "Ukrainian", "Indonesian",
                        "Czech", "Turkish", "pt-BR", "pt-PT", "Polish", "Finnish", "Hungarian",
                    },
                },
                new FontJob
                {
                    sourcePath = $"{NotoDir}/NotoSansArabic-Regular.ttf",
                    outName = "NotoSansArabic SDF",
                    addLatinFallback = true,
                    langCodes = new[] { "Arabic" },
                },
                new FontJob
                {
                    sourcePath = $"{NotoDir}/NotoSansThai-Regular.ttf",
                    outName = "NotoSansThai SDF",
                    addLatinFallback = true,
                    langCodes = new[] { "Thai" },
                },
                new FontJob
                {
                    sourcePath = $"{NotoDir}/NotoSansTC-Regular.otf",
                    outName = "NotoSansTC SDF",
                    addLatinFallback = true,
                    langCodes = new[] { "ChineseTraditional" },
                },
                new FontJob
                {
                    sourcePath = $"{NotoDir}/NotoSansSC-Regular.otf",
                    outName = "NotoSansSC SDF",
                    addLatinFallback = true,
                    langCodes = new[] { "ChineseSimplified" },
                },
            };

            if (!AssetDatabase.IsValidFolder(SdfDir))
                AssetDatabase.CreateFolder(NotoDir, "SDF");

            var created = new Dictionary<string, TMP_FontAsset>();
            TMP_FontAsset sansAsset = null;

            // 1) SDF 에셋 생성(없으면 생성, 있으면 재사용)
            foreach (var job in jobs)
            {
                string outPath = $"{SdfDir}/{job.outName}.asset";
                var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
                if (fa == null)
                    fa = CreateDynamicSDF(job.sourcePath, outPath);

                if (fa == null)
                {
                    Debug.LogError($"[Noto] 폰트 에셋 생성 실패: {job.sourcePath}");
                    continue;
                }

                created[job.outName] = fa;
                if (job.outName == "NotoSans SDF")
                    sansAsset = fa;
            }

            // 2) 비라틴 폰트에 NotoSans fallback 추가(라틴/숫자/기호 보강)
            if (sansAsset != null)
            {
                foreach (var job in jobs)
                {
                    if (!job.addLatinFallback) continue;
                    if (!created.TryGetValue(job.outName, out var fa)) continue;

                    fa.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                    if (!fa.fallbackFontAssetTable.Contains(sansAsset))
                    {
                        fa.fallbackFontAssetTable.Add(sansAsset);
                        EditorUtility.SetDirty(fa);
                    }
                }
            }

            // 3) 실제 번역 텍스트의 글리프를 미리 베이크(핵심: 런타임 베이크 의존 제거)
            foreach (var job in jobs)
            {
                if (!created.TryGetValue(job.outName, out var fa)) continue;

                string chars = CollectCharacters(job.langCodes, job.collectAllColumns);
                if (string.IsNullOrEmpty(chars)) continue;

                bool ok = fa.TryAddCharacters(chars, out string missing);
                // 새 글리프로 추가 아틀라스가 생겼다면 서브에셋으로 등록(안 하면 저장 시 유실)
                if (fa.atlasTextures != null)
                {
                    foreach (var tex in fa.atlasTextures)
                    {
                        if (tex != null && !AssetDatabase.Contains(tex))
                        {
                            tex.name = $"{fa.name} Atlas {System.Array.IndexOf(fa.atlasTextures, tex)}";
                            AssetDatabase.AddObjectToAsset(tex, fa);
                        }
                    }
                }
                EditorUtility.SetDirty(fa);

                int tried = chars.Length;
                int miss = string.IsNullOrEmpty(missing) ? 0 : missing.Length;
                Debug.Log($"[Noto] {fa.name}: {tried}자 베이크 시도, 글리프 없음 {miss}자 " +
                          $"(없는 문자는 다른 폰트/fallback이 담당). 성공={ok}");
            }

            // 3.5) 베이크 완료 후 Static으로 고정.
            // Dynamic 모드는 에디터 플레이/빌드/reimport 시 베이크된 글리프가 클리어돼
            // 비라틴(특히 아랍/태국 등 런타임 베이크 불가한 complex script)이 □로 나온다.
            // 필요한 글자를 위에서 모두 미리 베이크했으므로 Static으로 굳혀 글리프를 영구 보존한다.
            foreach (var job in jobs)
            {
                if (!created.TryGetValue(job.outName, out var fa)) continue;
                fa.atlasPopulationMode = AtlasPopulationMode.Static;
                var so = new SerializedObject(fa);
                var clearProp = so.FindProperty("m_ClearDynamicDataOnBuild");
                if (clearProp != null)
                {
                    clearProp.boolValue = false;
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(fa);
                Debug.Log($"[Noto] {fa.name}: AtlasPopulationMode=Static 고정 (글리프 손실 방지)");
            }

            // 4) LocalizeSetting.languageFonts 등록
            var setting = AssetDatabase.LoadAssetAtPath<LocalizeSetting>(SettingPath);
            if (setting == null)
            {
                Debug.LogError($"[Noto] LocalizeSetting을 찾을 수 없습니다: {SettingPath}");
            }
            else
            {
                setting.languageFonts ??= new List<LanguageFontEntry>();
                int registered = 0;
                foreach (var job in jobs)
                {
                    if (!created.TryGetValue(job.outName, out var fa)) continue;
                    foreach (var code in job.langCodes)
                    {
                        var entry = setting.languageFonts.Find(e => e != null && e.langCode == code);
                        if (entry == null)
                        {
                            entry = new LanguageFontEntry { langCode = code };
                            setting.languageFonts.Add(entry);
                        }
                        entry.font = fa;
                        registered++;
                    }
                }
                EditorUtility.SetDirty(setting);
                Debug.Log($"[Noto] languageFonts에 {registered}개 언어코드 등록");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Noto] 폰트 생성 + 글리프 베이크 + languageFonts 등록 완료. " +
                      "플레이에서 각 언어 표시 확인하세요.</color>");
        }

        /// <summary>
        /// SheetFiles의 헤더에서 langCodes(또는 collectAll이면 전 컬럼)에 해당하는 컬럼의
        /// 모든 문자를 모아 중복 없는 문자열로 반환한다(탭/개행 제외).
        /// </summary>
        private static string CollectCharacters(string[] langCodes, bool collectAllColumns)
        {
            var codes = new HashSet<string>(langCodes);
            var set = new HashSet<char>();

            foreach (var file in SheetFiles)
            {
                if (!File.Exists(file)) continue;
                string[] lines = File.ReadAllLines(file);
                if (lines.Length < 1) continue;

                string[] headers = lines[0].Split('\t');
                var cols = new List<int>();
                for (int i = 1; i < headers.Length; i++)
                {
                    string h = headers[i].Trim();
                    if (collectAllColumns || codes.Contains(h))
                        cols.Add(i);
                }
                if (cols.Count == 0) continue;

                for (int r = 1; r < lines.Length; r++)
                {
                    if (string.IsNullOrEmpty(lines[r])) continue;
                    string[] f = lines[r].Split('\t');
                    foreach (int ci in cols)
                    {
                        if (ci >= f.Length) continue;
                        foreach (char ch in f[ci])
                        {
                            if (ch == '\t' || ch == '\r' || ch == '\n') continue;
                            set.Add(ch);
                        }
                    }
                }
            }

            var sb = new StringBuilder(set.Count);
            foreach (char ch in set)
                sb.Append(ch);
            return sb.ToString();
        }

        private static TMP_FontAsset CreateDynamicSDF(string sourcePath, string outPath)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                Debug.LogError($"[Noto] 소스 폰트 로드 실패(Unity import 필요): {sourcePath}");
                return null;
            }

            var fa = TMP_FontAsset.CreateFontAsset(
                font,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasWidth,
                AtlasHeight,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fa == null)
                return null;

            fa.name = Path.GetFileNameWithoutExtension(outPath);
            AssetDatabase.CreateAsset(fa, outPath);

            if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
            {
                fa.atlasTextures[0].name = fa.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            }
            if (fa.material != null)
            {
                fa.material.name = fa.name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }

            EditorUtility.SetDirty(fa);
            return fa;
        }
    }
}
