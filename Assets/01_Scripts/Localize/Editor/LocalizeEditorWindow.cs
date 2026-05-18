#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using System.IO;
using System.Text;
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
        private Vector2 _scrollPos;

        [MenuItem("TrainDefense/Localize")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizeEditorWindow>("Localize");
            window.titleContent = new GUIContent("Localize Manager", EditorGUIUtility.IconContent("d_Project").image);
            window.minSize = new Vector2(480, 600);
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
            // Runtime mode toggle
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
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
                    ? "게임 실행 시 항상 최신 시트를 다운로드합니다."
                    : "빌드에 포함된 LocalizeSource_*.txt를 사용합니다.",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Sheets
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Sheets", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(300));
            if (_setting.sheets != null)
            {
                for (int i = 0; i < _setting.sheets.Count; i++)
                {
                    var sheet = _setting.sheets[i];
                    EditorGUILayout.BeginVertical("box");

                    EditorGUI.BeginChangeCheck();
                    sheet.sheet = (LocalizeSheet)EditorGUILayout.EnumPopup("Sheet", sheet.sheet);
                    sheet.sheetURL = EditorGUILayout.TextField("Export URL (TSV)", sheet.sheetURL ?? "");
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(_setting);
                        AssetDatabase.SaveAssets();
                    }

                    EditorGUI.BeginChangeCheck();
                    var newAsset = (TextAsset)EditorGUILayout.ObjectField("Baked Asset", sheet.bakedTextAsset, typeof(TextAsset), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        sheet.bakedTextAsset = newAsset;
                        EditorUtility.SetDirty(_setting);
                        AssetDatabase.SaveAssets();
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button($"Import '{sheet.sheet}'", GUILayout.Height(24)))
                        ImportSheet(sheet).Forget();
                    if (GUILayout.Button("Local File", GUILayout.Height(24)))
                        AssignLocalFile(sheet);
                    if (GUILayout.Button("X", GUILayout.Width(24), GUILayout.Height(24)))
                    {
                        _setting.sheets.RemoveAt(i);
                        EditorUtility.SetDirty(_setting);
                        AssetDatabase.SaveAssets();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(4);
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ Add Sheet", GUILayout.Height(24)))
            {
                _setting.sheets.Add(new LocalizeSheetEntry());
                EditorUtility.SetDirty(_setting);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Import all
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Import All & Bake", GUILayout.Height(40)))
                ImportAllSheets().Forget();
            GUI.backgroundColor = originalColor;
        }

        private void AssignLocalFile(LocalizeSheetEntry sheet)
        {
            string defaultDir = Path.Combine(Application.dataPath, "../", BaseDirectory);
            string path = EditorUtility.OpenFilePanel(
                $"'{sheet.sheet}' 시트 TSV 파일 선택", defaultDir, "txt");
            if (string.IsNullOrEmpty(path)) return;

            string relPath = "Assets" + path.Replace(Application.dataPath, "").Replace('\\', '/');
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(relPath);
            if (asset == null)
            {
                Debug.LogError($"[Localize] Assets 폴더 내 파일만 선택 가능합니다: {relPath}");
                return;
            }

            sheet.bakedTextAsset = asset;

            // enum 재생성 (모든 baked 데이터 기반)
            var allTsvData = new System.Collections.Generic.List<(LocalizeSheetEntry s, string t)>();
            foreach (var s in _setting.sheets)
                if (s.bakedTextAsset != null)
                    allTsvData.Add((s, s.bakedTextAsset.text));
            GenerateEnumFile(allTsvData);

            EditorUtility.SetDirty(_setting);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[Localize] '{sheet.sheet}' 로컬 파일 할당 완료: {relPath}</color>");
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

        private async UniTaskVoid ImportAllSheets()
        {
            if (_setting.sheets == null || _setting.sheets.Count == 0)
            {
                Debug.LogError("[Localize] 시트가 없습니다. 먼저 시트를 추가하세요.");
                return;
            }

            // Collect all TSV data
            var allTsvData = new System.Collections.Generic.List<(LocalizeSheetEntry sheet, string tsv)>();
            foreach (var sheet in _setting.sheets)
            {
                if (string.IsNullOrEmpty(sheet.sheetURL))
                {
                    Debug.LogWarning($"[Localize] '{sheet.sheet.ToString()}' URL이 비어있습니다. 건너뜁니다.");
                    continue;
                }
                Debug.Log($"[Localize] '{sheet.sheet.ToString()}' 다운로드 중...");
                using UnityWebRequest www = UnityWebRequest.Get(sheet.sheetURL);
                await www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Localize] '{sheet.sheet.ToString()}' 다운로드 실패: {www.error}");
                    continue;
                }
                allTsvData.Add((sheet, www.downloadHandler.text));
            }

            // Generate enum from merged data
            GenerateEnumFile(allTsvData);

            // Bake individual sheet files
            foreach (var (sheet, tsv) in allTsvData)
            {
                if (!_setting.useRuntimeDownload)
                    BakeSheetFile(sheet, tsv);
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Localize] 모든 시트 Import & Bake 완료</color>");
        }

        private async UniTaskVoid ImportSheet(LocalizeSheetEntry sheet)
        {
            if (string.IsNullOrEmpty(sheet.sheetURL))
            {
                Debug.LogError($"[Localize] '{sheet.sheet.ToString()}' URL이 비어있습니다.");
                return;
            }

            Debug.Log($"[Localize] '{sheet.sheet.ToString()}' 다운로드 중...");
            using UnityWebRequest www = UnityWebRequest.Get(sheet.sheetURL);
            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Localize] '{sheet.sheet.ToString()}' 다운로드 실패: {www.error}");
                return;
            }

            string tsv = www.downloadHandler.text;

            // Enum regeneration requires all sheets - just bake this one
            if (!_setting.useRuntimeDownload)
                BakeSheetFile(sheet, tsv);

            // Regenerate enum using all baked data + this new one
            var allTsvData = new System.Collections.Generic.List<(LocalizeSheetEntry s, string t)>();
            foreach (var s in _setting.sheets)
            {
                if (s == sheet)
                    allTsvData.Add((s, tsv));
                else if (s.bakedTextAsset != null)
                    allTsvData.Add((s, s.bakedTextAsset.text));
            }
            GenerateEnumFile(allTsvData);

            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[Localize] '{sheet.sheet.ToString()}' Import & Bake 완료</color>");
        }

        private void GenerateEnumFile(System.Collections.Generic.List<(LocalizeSheetEntry sheet, string tsv)> allData)
        {
            string enumPath = Path.Combine(Application.dataPath, "../", BaseDirectory, "LocalizeKey.cs");

            var sb = new StringBuilder();
            sb.AppendLine("// This file is auto-generated. Do not modify manually.");
            sb.AppendLine("namespace TrainDefense.Localize");
            sb.AppendLine("{");
            sb.AppendLine("    public static class LocalizeKeyExtension");
            sb.AppendLine("    {");
            sb.AppendLine("        public static string ToLocalizeString(this LocalizeKey key) => LocalizeHelper.GetByKey(key.ToString(), key.ToString());");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public enum LocalizeKey");
            sb.AppendLine("    {");

            int globalIndex = 1;
            var seenKeys = new System.Collections.Generic.HashSet<string>();

            foreach (var (sheet, tsv) in allData)
            {
                if (string.IsNullOrEmpty(tsv)) continue;
                string[] lines = tsv.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                sb.AppendLine($"        // {sheet.sheet.ToString()}");
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    string key = lines[i].Split('\t')[0].Trim();
                    if (string.IsNullOrEmpty(key) || seenKeys.Contains(key)) continue;
                    seenKeys.Add(key);
                    sb.AppendLine($"        {key} = {globalIndex++},");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(enumPath, sb.ToString(), Encoding.UTF8);
            Debug.Log("<color=cyan>[Localize] Enum Generated: LocalizeKey.cs</color>");
        }

        private void BakeSheetFile(LocalizeSheetEntry sheet, string tsv)
        {
            string relPath = $"{BaseDirectory}/LocalizeSource_{sheet.sheet}.txt";
            string absPath = Path.Combine(Application.dataPath, "../", relPath);

            File.WriteAllText(absPath, tsv, Encoding.UTF8);
            AssetDatabase.Refresh();

            sheet.bakedTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(relPath);
            EditorUtility.SetDirty(_setting);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>[Localize] Baked: {relPath}</color>");
        }
    }
}
#endif
