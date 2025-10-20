using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Cumic.Excel;

namespace Cumic.Excel.Editor
{
    /// <summary>
    /// Excel 파일을 ScriptableObject로 임포트하는 Unity 에디터 윈도우
    /// Sheet별 선택적 로드 지원
    /// </summary>
    public class ExcelImporterWindow : EditorWindow
    {
        #region Fields
        private string _excelFilePath = "";
        private List<string> _sheetNames = new List<string>();
        private int _selectedSheetIndex = 0;
        private List<Dictionary<string, object>> _previewData = new List<Dictionary<string, object>>();
        private Vector2 _scrollPosition;
        private bool _showPreview = true;
        private string _outputFolder = "Assets/Resources/Datas";
        private Type _targetScriptableObjectType;
        private string[] _availableTypes;
        private int _selectedTypeIndex = 0;
        #endregion

        #region Unity Editor Menu
        [MenuItem("Tools/Excel Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExcelImporterWindow>("Excel Importer");
            window.minSize = new Vector2(600, 400);
        }
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            LoadAvailableScriptableObjectTypes();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Excel Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawFileSelection();
            EditorGUILayout.Space();

            DrawSheetSelection();
            EditorGUILayout.Space();

            DrawScriptableObjectTypeSelection();
            EditorGUILayout.Space();

            DrawOutputFolderSelection();
            EditorGUILayout.Space();

            DrawActionButtons();
            EditorGUILayout.Space();

            DrawPreview();
        }
        #endregion

        #region UI Drawing Methods
        private void DrawFileSelection()
        {
            EditorGUILayout.LabelField("Excel 파일 선택", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("파일 경로:", GUILayout.Width(80));
            EditorGUILayout.TextField(_excelFilePath, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("찾기", GUILayout.Width(50)))
            {
                string path = EditorUtility.OpenFilePanel("Excel 파일 선택", "", "xlsx,xls");
                if (!string.IsNullOrEmpty(path))
                {
                    _excelFilePath = path;
                    LoadSheetNames();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSheetSelection()
        {
            EditorGUILayout.LabelField("시트 선택", EditorStyles.boldLabel);
            
            if (_sheetNames.Count > 0)
            {
                _selectedSheetIndex = EditorGUILayout.Popup("시트:", _selectedSheetIndex, _sheetNames.ToArray());
                
                if (GUILayout.Button("시트 미리보기"))
                {
                    LoadPreviewData();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Excel 파일을 먼저 선택해주세요.", MessageType.Info);
            }
        }

        private void DrawScriptableObjectTypeSelection()
        {
            EditorGUILayout.LabelField("ScriptableObject 타입 선택", EditorStyles.boldLabel);
            
            if (_availableTypes != null && _availableTypes.Length > 0)
            {
                _selectedTypeIndex = EditorGUILayout.Popup("타입:", _selectedTypeIndex, _availableTypes);
                _targetScriptableObjectType = Type.GetType(_availableTypes[_selectedTypeIndex]);
            }
            else
            {
                EditorGUILayout.HelpBox("ScriptableObject 타입을 찾을 수 없습니다.", MessageType.Warning);
            }
        }

        private void DrawOutputFolderSelection()
        {
            EditorGUILayout.LabelField("출력 폴더", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("폴더:", GUILayout.Width(50));
            EditorGUILayout.TextField(_outputFolder, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("찾기", GUILayout.Width(50)))
            {
                string path = EditorUtility.OpenFolderPanel("출력 폴더 선택", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _outputFolder = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("작업", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !string.IsNullOrEmpty(_excelFilePath) && _sheetNames.Count > 0 && _targetScriptableObjectType != null;
            
            if (GUILayout.Button("ScriptableObject 생성"))
            {
                CreateScriptableObjects();
            }
            
            GUI.enabled = true;
            
            if (GUILayout.Button("폴더 열기"))
            {
                EditorUtility.RevealInFinder(_outputFolder);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreview()
        {
            if (_previewData.Count == 0) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("데이터 미리보기", EditorStyles.boldLabel);
            
            _showPreview = EditorGUILayout.Foldout(_showPreview, $"미리보기 ({_previewData.Count}행)");
            
            if (_showPreview)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
                
                if (_previewData.Count > 0)
                {
                    var firstRow = _previewData[0];
                    var columns = firstRow.Keys.ToArray();
                    
                    // 헤더 그리기
                    EditorGUILayout.BeginHorizontal();
                    foreach (var column in columns)
                    {
                        EditorGUILayout.LabelField(column, EditorStyles.boldLabel, GUILayout.Width(100));
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // 데이터 행들 그리기 (최대 10행만 표시)
                    int maxRows = Mathf.Min(10, _previewData.Count);
                    for (int i = 0; i < maxRows; i++)
                    {
                        var row = _previewData[i];
                        EditorGUILayout.BeginHorizontal();
                        
                        foreach (var column in columns)
                        {
                            var value = row.ContainsKey(column) ? row[column]?.ToString() ?? "" : "";
                            EditorGUILayout.LabelField(value, GUILayout.Width(100));
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (_previewData.Count > 10)
                    {
                        EditorGUILayout.LabelField($"... 및 {_previewData.Count - 10}행 더", EditorStyles.centeredGreyMiniLabel);
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        #endregion

        #region Data Loading Methods
        private void LoadAvailableScriptableObjectTypes()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(ScriptableObject)) && !type.IsAbstract)
                .Select(type => $"{type.Namespace}.{type.Name}")
                .ToArray();
            
            _availableTypes = types;
        }

        private void LoadSheetNames()
        {
            if (string.IsNullOrEmpty(_excelFilePath)) return;
            
            try
            {
                _sheetNames = ExcelLoader.GetSheetNames(_excelFilePath);
                _selectedSheetIndex = 0;
                _previewData.Clear();
                
                Debug.Log($"Excel 파일에서 {_sheetNames.Count}개의 시트를 찾았습니다: {string.Join(", ", _sheetNames)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"시트 목록 로드 실패: {ex.Message}");
                _sheetNames.Clear();
            }
        }

        private void LoadPreviewData()
        {
            if (string.IsNullOrEmpty(_excelFilePath) || _sheetNames.Count == 0) return;
            
            try
            {
                var sheetName = _sheetNames[_selectedSheetIndex];
                _previewData = ExcelLoader.LoadSheet(_excelFilePath, sheetName);
                
                Debug.Log($"시트 '{sheetName}'에서 {_previewData.Count}행의 데이터를 미리보기로 로드했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"미리보기 데이터 로드 실패: {ex.Message}");
                _previewData.Clear();
            }
        }
        #endregion

        #region ScriptableObject Creation
        private void CreateScriptableObjects()
        {
            if (string.IsNullOrEmpty(_excelFilePath) || _sheetNames.Count == 0 || _targetScriptableObjectType == null)
            {
                EditorUtility.DisplayDialog("오류", "모든 필드를 입력해주세요.", "확인");
                return;
            }

            try
            {
                var sheetName = _sheetNames[_selectedSheetIndex];
                var data = ExcelLoader.LoadSheet(_excelFilePath, sheetName);
                
                if (data.Count == 0)
                {
                    EditorUtility.DisplayDialog("경고", "로드할 데이터가 없습니다.", "확인");
                    return;
                }

                // 출력 폴더가 존재하지 않으면 생성
                if (!Directory.Exists(_outputFolder))
                {
                    Directory.CreateDirectory(_outputFolder);
                }

                int createdCount = 0;
                foreach (var rowData in data)
                {
                    try
                    {
                        var scriptableObject = CreateScriptableObjectFromRow(rowData);
                        if (scriptableObject != null)
                        {
                            SaveScriptableObject(scriptableObject, rowData);
                            createdCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"ScriptableObject 생성 실패: {ex.Message}");
                    }
                }

                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("완료", 
                    $"{createdCount}개의 ScriptableObject가 생성되었습니다.\n출력 폴더: {_outputFolder}", "확인");
                
                Debug.Log($"Excel 임포트 완료: {createdCount}개 객체 생성");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ScriptableObject 생성 중 오류 발생: {ex.Message}");
                EditorUtility.DisplayDialog("오류", $"ScriptableObject 생성 중 오류가 발생했습니다:\n{ex.Message}", "확인");
            }
        }

        private ScriptableObject CreateScriptableObjectFromRow(Dictionary<string, object> rowData)
        {
            var scriptableObject = CreateInstance(_targetScriptableObjectType);
            
            // IExcelLoadable 인터페이스를 구현한 경우 해당 메서드 사용
            if (scriptableObject is IExcelLoadable excelLoadable)
            {
                if (excelLoadable.ValidateRequiredColumns(rowData))
                {
                    excelLoadable.LoadFromExcelRow(rowData);
                }
                else
                {
                    Debug.LogWarning("필수 컬럼이 누락되었습니다.");
                    return null;
                }
            }
            else
            {
                // 기본적인 리플렉션을 통한 필드 설정
                SetFieldsByReflection(scriptableObject, rowData);
            }
            
            return scriptableObject;
        }

        private void SetFieldsByReflection(ScriptableObject scriptableObject, Dictionary<string, object> rowData)
        {
            var type = scriptableObject.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (rowData.ContainsKey(field.Name))
                {
                    var value = rowData[field.Name];
                    if (value != null)
                    {
                        try
                        {
                            var convertedValue = ConvertValueToFieldType(value, field.FieldType);
                            field.SetValue(scriptableObject, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"필드 '{field.Name}' 설정 실패: {ex.Message}");
                        }
                    }
                }
            }
        }

        private object ConvertValueToFieldType(object value, Type targetType)
        {
            if (value == null) return null;
            
            if (targetType.IsAssignableFrom(value.GetType()))
                return value;
            
            return targetType switch
            {
                Type t when t == typeof(int) => ExcelDataConverter.ConvertToInt(value),
                Type t when t == typeof(float) => ExcelDataConverter.ConvertToFloat(value),
                Type t when t == typeof(string) => ExcelDataConverter.ConvertToString(value),
                Type t when t == typeof(bool) => ExcelDataConverter.ConvertToBool(value),
                Type t when t == typeof(DateTime) => ExcelDataConverter.ConvertToDateTime(value),
                _ when targetType.IsEnum => ExcelDataConverter.ConvertToEnum(value, (Enum)Activator.CreateInstance(targetType)),
                _ => value
            };
        }

        private void SaveScriptableObject(ScriptableObject scriptableObject, Dictionary<string, object> rowData)
        {
            // ID나 이름 필드를 찾아서 파일명으로 사용
            string fileName = GetFileNameFromRowData(rowData);
            string filePath = Path.Combine(_outputFolder, $"{fileName}.asset");
            
            // 파일명 중복 처리
            int counter = 1;
            string originalPath = filePath;
            while (File.Exists(filePath))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
                filePath = Path.Combine(_outputFolder, $"{nameWithoutExt}_{counter}.asset");
                counter++;
            }
            
            AssetDatabase.CreateAsset(scriptableObject, filePath);
        }

        private string GetFileNameFromRowData(Dictionary<string, object> rowData)
        {
            // 일반적인 ID 필드들을 우선순위로 검색
            string[] idFields = { "id", "ID", "Id", "name", "Name", "key", "Key" };
            
            foreach (var field in idFields)
            {
                if (rowData.ContainsKey(field))
                {
                    return ExcelDataConverter.ConvertToString(rowData[field]);
                }
            }
            
            // 첫 번째 필드 사용
            var firstKey = rowData.Keys.FirstOrDefault();
            if (firstKey != null)
            {
                return ExcelDataConverter.ConvertToString(rowData[firstKey]);
            }
            
            return "NewObject";
        }
        #endregion
    }
}
