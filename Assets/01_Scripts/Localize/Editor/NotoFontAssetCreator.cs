using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TrainDefense.Localize.EditorTools
{
    /// <summary>
    /// 다운로드해 둔 Noto 폰트(.ttf/.otf)를 TMP_FontAsset(Dynamic SDF)으로 변환하고
    /// LocalizeSetting.languageFonts에 언어코드별로 자동 등록한다.
    /// Dynamic 모드라 런타임에 필요한 글리프만 아틀라스에 추가된다(31개 정적 베이크 회피).
    ///
    /// 사용법: Unity 메뉴 → TrainDefense/Localize/Create Noto Font Assets
    /// (먼저 Noto 폰트들이 프로젝트에 import 되어 있어야 한다 — 에디터를 한 번 포커스하면 자동 import됨)
    /// </summary>
    public static class NotoFontAssetCreator
    {
        private const string NotoDir = "Assets/02_Resources/Font/Noto";
        private const string SdfDir = "Assets/02_Resources/Font/Noto/SDF";
        private const string SettingPath =
            "Assets/TrainDefense_Generated/Localize/Resources/LocalizeSetting.asset";

        // SDF 베이크 파라미터(기존 폰트들과 동일 계열: 90pt 샘플링, 9px 패딩, 1024 아틀라스)
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1024;

        private class FontJob
        {
            public string sourcePath;
            public string outName;
            public string[] langCodes;
            public bool addLatinFallback; // 비라틴 폰트에 NotoSans(라틴/숫자) fallback 부여
        }

        [MenuItem("TrainDefense/Localize/Create Noto Font Assets")]
        public static void CreateAll()
        {
            var jobs = new List<FontJob>
            {
                // 라틴 확장 + 키릴 + 그리스 + 베트남 성조 (24개 언어)
                new FontJob
                {
                    sourcePath = $"{NotoDir}/NotoSans-Regular.ttf",
                    outName = "NotoSans SDF",
                    addLatinFallback = false,
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

            // 비라틴 폰트에 NotoSans를 fallback으로 추가(라틴 문자/숫자/기호 보강)
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

            // LocalizeSetting.languageFonts 등록
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
            Debug.Log("<color=green>[Noto] 폰트 에셋 생성 + languageFonts 등록 완료. " +
                      "키릴/그리스/태국/아랍/번체/간체 표시 가능.</color>");
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

            // 아틀라스 텍스처/머티리얼을 메인 에셋의 서브 에셋으로 저장(없으면 재import 시 깨짐)
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
