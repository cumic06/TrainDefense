#if HAS_UNITASK
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace TrainDefense.Localize
{
    public class LocalizeEditorWindow : EditorWindow
    {
        private const string BaseDirectory = "Assets/TrainDefense_Generated/Localize";
        private const string SettingPath = BaseDirectory + "/Resources/LocalizeSetting.asset";
        private LocalizeSetting _setting;

        [MenuItem("TrainDefense/Localize")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizeEditorWindow>("Localize");
            window.titleContent = new GUIContent("Localize Manager", EditorGUIUtility.IconContent("d_Project").image);
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            _setting = AssetDatabase.LoadAssetAtPath<LocalizeSetting>(SettingPath);
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(10);

            if (_setting == null)
                DrawStartSettingBody();
            else
                DrawMainGUI();
        }

        private void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            EditorGUILayout.Space(10);
            GUILayout.Label(new GUIContent(" Localize Manager", EditorGUIUtility.IconContent("d_Settings").image), headerStyle);

            string helpText = _setting == null
                ? "초기 설정이 필요합니다."
                : (_setting.useRuntimeDownload ? "현재 [런타임 다운로드] 모드입니다." : "현재 [로컬 베이크 데이터] 모드입니다.");
            EditorGUILayout.HelpBox(helpText, MessageType.Info);
        }

        private void DrawStartSettingBody()
        {
            EditorGUILayout.HelpBox($"파일들이 '{BaseDirectory}' 경로에 생성됩니다.", MessageType.Warning);
            if (GUILayout.Button("Start Setting", GUILayout.Height(40)))
                CreateSettingAsset();
        }

        private void DrawMainGUI()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Google Spreadsheet Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            _setting.sheetURL = EditorGUILayout.TextField("Export URL (TSV)", _setting.sheetURL);
            EditorGUILayout.Space(5);
            _setting.useRuntimeDownload = EditorGUILayout.ToggleLeft(
                " <b>Use Runtime Download</b>", _setting.useRuntimeDownload,
                new GUIStyle(EditorStyles.label) { richText = true });

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_setting);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.HelpBox(
                _setting.useRuntimeDownload
                    ? "게임 실행 시 항상 최신 시트를 다운로드합니다.\n(빌드 시에도 인터넷 연결이 필요할 수 있음)"
                    : "빌드에 포함된 'LocalizeSource.txt'를 사용합니다.",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            if (GUILayout.Button("Import & Bake Localize Data", GUILayout.Height(45)))
                GenerateEnumFromGoogleSheetAsync().Forget();

            EditorGUILayout.Space(10);

            GUI.enabled = false;
            EditorGUILayout.LabelField("Baked Data Info", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Text Asset", _setting.localizedTextAsset, typeof(TextAsset), false);
            GUI.enabled = true;
        }

        private void CreateSettingAsset()
        {
            string absolutePath = Path.Combine(Application.dataPath, "../", BaseDirectory, "Resources");

            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
                AssetDatabase.Refresh();
            }

            _setting = CreateInstance<LocalizeSetting>();
            AssetDatabase.CreateAsset(_setting, SettingPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[Localize]</b> 세팅 파일 생성 완료: {SettingPath}</color>");
        }

        private async UniTaskVoid GenerateEnumFromGoogleSheetAsync()
        {
            if (string.IsNullOrEmpty(_setting.sheetURL))
            {
                Debug.LogError("[Localize] URL이 비어있습니다.");
                return;
            }

            Debug.Log("[Localize] 시트 데이터 다운로드 중...");
            try
            {
                using UnityWebRequest www = UnityWebRequest.Get(_setting.sheetURL);
                await www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                    Debug.LogError($"[Localize] 다운로드 실패: {www.error}");
                else
                    ProcessAndBake(www.downloadHandler.text);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ProcessAndBake(string tsvContent)
        {
            GenerateEnumFile(tsvContent);

            if (!_setting.useRuntimeDownload)
                BakeLocalDataFile(tsvContent);
            else
                Debug.Log("<color=yellow>[Localize]</color> Runtime Download 모드이므로 LocalizeSource.txt 생성은 건너뜁니다.");

            AssetDatabase.Refresh();
        }

        private void GenerateEnumFile(string tsvContent)
        {
            string enumPath = Path.Combine(Application.dataPath, "../", BaseDirectory, "LocalizeKey.cs");
            string[] lines = tsvContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            var sb = new StringBuilder();
            sb.AppendLine("// This file is auto-generated. Do not modify manually.");
            sb.AppendLine("namespace TrainDefense.Localize");
            sb.AppendLine("{");
            sb.AppendLine("    public static class LocalizeKeyExtension");
            sb.AppendLine("    {");
            sb.AppendLine("        public static string ToLocalizeString(this LocalizeKey key) => Localization.Get((int)key);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public enum LocalizeKey");
            sb.AppendLine("    {");

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string key = lines[i].Split('\t')[0].Trim();
                if (!string.IsNullOrEmpty(key))
                    sb.AppendLine($"        {key} = {i},");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(enumPath, sb.ToString(), Encoding.UTF8);
            Debug.Log("<color=cyan>[Localize] Enum Generated: LocalizeKey.cs</color>");
        }

        private void BakeLocalDataFile(string tsvContent)
        {
            string dataRelativePath = BaseDirectory + "/LocalizeSource.txt";
            string dataAbsolutePath = Path.Combine(Application.dataPath, "../", dataRelativePath);

            File.WriteAllText(dataAbsolutePath, tsvContent, Encoding.UTF8);
            AssetDatabase.Refresh();

            _setting.localizedTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataRelativePath);
            EditorUtility.SetDirty(_setting);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[Localize] Data Baked & Linked: LocalizeSource.txt</color>");
        }
    }
}
#endif
