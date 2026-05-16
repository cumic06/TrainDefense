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
            GUILayout.Label("언어 강제 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            bool isPlaying = Application.isPlaying;
            bool isInitialized = isPlaying && Localization.IsInitialized;

            if (isPlaying && !isInitialized)
                EditorGUILayout.HelpBox("Localization 초기화 중...", MessageType.Warning);
            else if (!isPlaying)
                EditorGUILayout.HelpBox("에디터 모드: 설정만 저장됩니다. 플레이 시 적용됩니다.", MessageType.Info);

            EditorGUILayout.Space(3);

            DrawLanguageButton("한국어 (Korean)", SystemLanguage.Korean);
            DrawLanguageButton("English", SystemLanguage.English);
            DrawLanguageButton("日本語 (Japanese)", SystemLanguage.Japanese);
            DrawLanguageButton("中文 (Chinese Simplified)", SystemLanguage.ChineseSimplified);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("OS 언어로 초기화", GUILayout.Height(30)))
                Localization.ClearLanguageOverride();

            EditorGUILayout.Space(10);
            string current = Localization.CurrentOverride.HasValue
                ? Localization.CurrentOverride.Value.ToString()
                : $"OS ({Application.systemLanguage})";
            EditorGUILayout.LabelField("현재 언어:", current, EditorStyles.boldLabel);
        }

        private void DrawLanguageButton(string label, SystemLanguage lang)
        {
            bool isCurrent = Localization.CurrentOverride == lang;
            GUI.backgroundColor = isCurrent ? Color.green : Color.white;
            if (GUILayout.Button(label, GUILayout.Height(35)))
                Localization.SetLanguage(lang);
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif
