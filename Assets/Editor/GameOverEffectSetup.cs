using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using TrainDefense.Game.Manager;

/// <summary>
/// GameOver 효과 초기 세팅 도구.
/// 메뉴: Tools > Setup GameOver Effect
/// 실행 후 이 파일은 삭제해도 됩니다.
/// </summary>
public static class GameOverEffectSetup
{
    [MenuItem("Tools/Setup GameOver Effect")]
    public static void Run()
    {
        try
        {
            // ── 1. 쉐이더 확인 ───────────────────────────────────────────
            var shader = Shader.Find("Custom/GameOverEffect");
            if (shader == null)
            {
                Show("오류", "Custom/GameOverEffect 쉐이더를 찾을 수 없습니다.\nAssets/02_Resources/Shaders/GameOverEffect.shader 가 임포트됐는지 확인하세요.");
                return;
            }

            // ── 2. Material 생성 ─────────────────────────────────────────
            const string matFolder = "Assets/02_Resources/Materials";
            if (!AssetDatabase.IsValidFolder(matFolder))
                AssetDatabase.CreateFolder("Assets/02_Resources", "Materials");

            const string matPath = matFolder + "/GameOverEffectMat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.SetFloat("_Intensity", 0f);
                mat.SetColor("_ColorTint", new Color(0.3f, 0.3f, 0.3f, 1f));
                mat.SetFloat("_GrayscaleStrength", 1f);
                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[GameOverEffectSetup] Material 생성: " + matPath);
            }

            // ── 3. Renderer2D.asset 로드 ─────────────────────────────────
            const string rendererPath = "Assets/Settings/Renderer2D.asset";
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
            if (rendererData == null)
            {
                Show("오류", "Assets/Settings/Renderer2D.asset 을 찾을 수 없습니다.");
                return;
            }

            // ── 4. 기존 "GameOverEffect" Feature 모두 제거 ───────────────
            _RemoveExistingFeature(rendererData);

            // ── 5. GameOverEffectFeature 추가 ────────────────────────────
            var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(typeof(GameOverEffectFeature));
            feature.name = "GameOverEffect";
            feature.hideFlags = HideFlags.HideInHierarchy;

            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.SaveAssets();

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

            var rdSO = new SerializedObject(rendererData);
            var featList = rdSO.FindProperty("m_RendererFeatures");
            var mapList  = rdSO.FindProperty("m_RendererFeatureMap");

            featList.arraySize++;
            featList.GetArrayElementAtIndex(featList.arraySize - 1).objectReferenceValue = feature;

            if (mapList != null)
            {
                mapList.arraySize++;
                mapList.GetArrayElementAtIndex(mapList.arraySize - 1).longValue = localId;
            }

            rdSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Show("완료",
                "GameOver Effect 설정 완료!\n\n" +
                "Material: " + matPath + "\n\n" +
                "남은 작업:\n" +
                "1. 씬에 빈 GameObject 추가\n" +
                "2. GameOverEffectController 컴포넌트 추가\n" +
                "3. Material 필드에 GameOverEffectMat 할당");
        }
        catch (Exception e)
        {
            Debug.LogError("[GameOverEffectSetup] 오류: " + e.Message + "\n" + e.StackTrace);
            Show("오류", e.Message);
        }
    }

    private static void _RemoveExistingFeature(ScriptableRendererData rendererData)
    {
        var toRemove = rendererData.rendererFeatures
            .Where(f => f != null && f.name == "GameOverEffect")
            .ToList();

        if (toRemove.Count == 0)
            return;

        var so = new SerializedObject(rendererData);
        var featList = so.FindProperty("m_RendererFeatures");
        var mapList  = so.FindProperty("m_RendererFeatureMap");

        foreach (var feature in toRemove)
        {
            for (int i = featList.arraySize - 1; i >= 0; i--)
            {
                if (featList.GetArrayElementAtIndex(i).objectReferenceValue == feature)
                {
                    featList.DeleteArrayElementAtIndex(i);

                    if (mapList != null && i < mapList.arraySize)
                        mapList.DeleteArrayElementAtIndex(i);

                    break;
                }
            }

            AssetDatabase.RemoveObjectFromAsset(feature);
            UnityEngine.Object.DestroyImmediate(feature, true);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();

        Debug.Log("[GameOverEffectSetup] 기존 GameOverEffect Feature 제거 완료");
    }

    private static void Show(string title, string msg) =>
        EditorUtility.DisplayDialog(title, msg, "확인");
}
