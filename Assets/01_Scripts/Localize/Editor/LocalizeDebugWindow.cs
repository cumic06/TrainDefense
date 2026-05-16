#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TrainDefense.Localize
{
    public class LocalizeDebugWindow : EditorWindow
    {
        [MenuItem("TrainDefense/Localize Debug")]
        public static void ShowWindow()
        {
            GetWindow<LocalizeDebugWindow>("Localize Debug");
        }

        private void OnGUI()
        {
            GUILayout.Label("언어 강제 설정 (플레이 중에만 적용)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 사용 가능합니다.", MessageType.Info);
                return;
            }

            if (!Localization.IsInitialized)
            {
                EditorGUILayout.HelpBox("Localization이 아직 초기화되지 않았습니다.", MessageType.Warning);
                return;
            }

            DrawLanguageButton("한국어 (Korean)", SystemLanguage.Korean);
            DrawLanguageButton("English", SystemLanguage.English);
            DrawLanguageButton("日本語 (Japanese)", SystemLanguage.Japanese);
            DrawLanguageButton("中文 (Chinese Simplified)", SystemLanguage.ChineseSimplified);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("OS 언어로 초기화"))
                Localization.ClearLanguageOverride();
        }

        private void DrawLanguageButton(string label, SystemLanguage lang)
        {
            if (GUILayout.Button(label, GUILayout.Height(35)))
                Localization.SetLanguage(lang);
        }
    }
}
#endif
