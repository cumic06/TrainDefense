using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Xml.Linq;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Excel/CSV 파일에서 게임 데이터를 가져오는 도구
    /// 시트 이름 기반 자동 타입 감지 및 동적 파싱 지원
    /// </summary>
    public static class ExcelImporter
    {
        #region MenuItems
        
        [MenuItem("Tools/ExcelImporter/Open Excel Manager")]
        private static void OpenExcelManager()
        {
            ExcelImportView.ShowWindow();
        }
        
        #endregion

        #region Data Import

        /// <summary>
        /// 엑셀 파일에서 시트별로 데이터를 자동 감지하여 불러오기
        /// 불러온 데이터 개수를 반환
        /// </summary>
        public static int ImportAllDataFromExcel(string filePath, DB targetDB)
        {
            try
            {
                string ext = Path.GetExtension(filePath).ToLower();
                
                if (ext == ".csv")
                {
                    return ImportFromCSV(filePath, targetDB);
                }
                else if (ext == ".xlsx" || ext == ".xlsm")
                {
                    return ImportFromExcel(filePath, targetDB);
                }
                else
                {
                    Debug.LogWarning($"지원하지 않는 파일 형식입니다: {ext}");
                    return 0;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"엑셀에서 데이터 불러오기 실패: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// CSV 파일에서 데이터 불러오기
        /// </summary>
        private static int ImportFromCSV(string filePath, DB targetDB)
        {
            return ImportSingleSheet(filePath, targetDB);
        }

        /// <summary>
        /// Excel 파일에서 데이터 불러오기
        /// </summary>
        private static int ImportFromExcel(string filePath, DB targetDB)
        {
            string tempDir = null;
            try
            {
                // 파일 접근 가능 여부 확인
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"파일을 찾을 수 없습니다: {filePath}");
                    return 0;
                }

                // 파일이 다른 프로그램에서 사용 중인지 확인
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // 파일 접근 가능
                    }
                }
                catch (System.IO.IOException ex)
                {
                    Debug.LogError($"파일이 다른 프로그램에서 사용 중입니다. Excel 파일을 닫고 다시 시도해주세요: {ex.Message}");
                    return 0;
                }

                tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);

                Debug.Log($"Excel 파일 압축 해제 시작: {filePath}");
                // Excel 파일을 ZIP으로 압축 해제
                ZipFile.ExtractToDirectory(filePath, tempDir);
                Debug.Log($"압축 해제 완료: {tempDir}");

                // workbook.xml에서 시트 정보 가져오기
                string workbookPath = Path.Combine(tempDir, "xl", "workbook.xml");
                if (!File.Exists(workbookPath))
                {
                    Debug.LogError($"workbook.xml을 찾을 수 없습니다: {workbookPath}");
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                    return 0;
                }

                Debug.Log($"workbook.xml 로드 시작");
                XDocument workbook = XDocument.Load(workbookPath);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var sheets = workbook.Descendants(ns + "sheet").ToList();

                if (sheets.Count == 0)
                {
                    Debug.LogWarning("시트를 찾을 수 없습니다.");
                }
                else
                {
                    Debug.Log($"발견된 시트 개수: {sheets.Count}");
                }

                int totalImported = 0;

                // 각 시트 처리
                foreach (var sheet in sheets)
                {
                    string sheetId = sheet.Attribute(XName.Get("sheetId"))?.Value;
                    string sheetName = sheet.Attribute(XName.Get("name"))?.Value;

                    if (string.IsNullOrEmpty(sheetName))
                    {
                        Debug.LogWarning("시트명이 비어있습니다.");
                        continue;
                    }

                    Debug.Log($"시트 처리 중: '{sheetName}' (ID: {sheetId})");

                    // 시트 파일 경로
                    string sheetPath = Path.Combine(tempDir, "xl", "worksheets", $"sheet{sheetId}.xml");
                    if (!File.Exists(sheetPath))
                    {
                        Debug.LogWarning($"시트 파일을 찾을 수 없습니다: {sheetPath}");
                        continue;
                    }

                    Debug.Log($"시트 파일 읽기: {sheetPath}");
                    // 시트 데이터 읽기
                    var dataList = ReadExcelSheetData(sheetPath, tempDir);
                    Debug.Log($"읽은 데이터 개수: {dataList.Count}");

                    if (dataList.Count > 0)
                    {
                        // 파일명을 시트명으로 사용하여 타입 감지
                        Type dataType = GetDataTypeFromSheetName(sheetName);

                        if (dataType != null)
                        {
                            Debug.Log($"데이터 타입 감지됨: {dataType.Name}");
                            AddDataToDB(dataType, dataList, targetDB);
                            totalImported += dataList.Count;
                            Debug.Log($"✅ 성공적으로 불러옴: {dataList.Count}개의 {dataType.Name} 데이터");
                        }
                        else
                        {
                            Debug.LogWarning($"시트명 '{sheetName}'에서 데이터 타입을 감지할 수 없습니다.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"시트 '{sheetName}'에 데이터가 없습니다.");
                    }
                }

                EditorUtility.SetDirty(targetDB);
                AssetDatabase.SaveAssets();

                Debug.Log($"총 {totalImported}개의 데이터를 불러왔습니다.");
                return totalImported;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Excel 파일 읽기 실패: {e.Message}\n스택 트레이스: {e.StackTrace}");
                return 0;
            }
            finally
            {
                // 임시 디렉토리 정리
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                        Debug.Log($"임시 디렉토리 삭제 완료: {tempDir}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"임시 디렉토리 삭제 실패: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Excel 시트 데이터 읽기
        /// </summary>
        private static List<Dictionary<string, string>> ReadExcelSheetData(string sheetPath, string tempDir)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            try
            {
                XDocument sheetDoc = XDocument.Load(sheetPath);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                // sharedStrings.xml 읽기
                string sharedStringsPath = Path.Combine(tempDir, "xl", "sharedStrings.xml");
                Dictionary<int, string> sharedStrings = new Dictionary<int, string>();
                if (File.Exists(sharedStringsPath))
                {
                    XDocument sharedStringsDoc = XDocument.Load(sharedStringsPath);
                    var stringElements = sharedStringsDoc.Descendants(ns + "t").ToList();
                    for (int i = 0; i < stringElements.Count; i++)
                    {
                        sharedStrings[i] = stringElements[i].Value;
                    }
                }

                // 모든 row 찾기
                var rows = sheetDoc.Descendants(ns + "row").ToList();

                if (rows.Count == 0) return result;

                // 첫 번째 행: 필드명
                var firstRow = rows[0];
                var firstRowCells = firstRow.Descendants(ns + "c").ToList();
                List<string> fieldNames = new List<string>();

                foreach (var cell in firstRowCells)
                {
                    string cellRef = cell.Attribute(XName.Get("r"))?.Value;
                    string cellType = cell.Attribute(XName.Get("t"))?.Value;
                    var valueElement = cell.Descendants(ns + "v").FirstOrDefault();

                    string value = "";
                    if (valueElement != null)
                    {
                        if (cellType == "s" && int.TryParse(valueElement.Value, out int sharedStringIndex))
                        {
                            value = sharedStrings.ContainsKey(sharedStringIndex) ? sharedStrings[sharedStringIndex] : "";
                        }
                        else
                        {
                            value = valueElement.Value;
                        }
                    }

                    fieldNames.Add(value);
                }

                Debug.Log($"필드 발견: {string.Join(", ", fieldNames)}");

                // 두 번째 행부터: 데이터
                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var cells = row.Descendants(ns + "c").ToList();

                    Dictionary<string, string> dataRow = new Dictionary<string, string>();

                    for (int j = 0; j < fieldNames.Count && j < cells.Count; j++)
                    {
                        string fieldName = NormalizeFieldName(fieldNames[j]);
                        var cell = cells[j];

                        string cellType = cell.Attribute(XName.Get("t"))?.Value;
                        var valueElement = cell.Descendants(ns + "v").FirstOrDefault();

                        string value = "";
                        if (valueElement != null)
                        {
                            if (cellType == "s" && int.TryParse(valueElement.Value, out int sharedStringIndex))
                            {
                                value = sharedStrings.ContainsKey(sharedStringIndex) ? sharedStrings[sharedStringIndex] : "";
                            }
                            else
                            {
                                value = valueElement.Value;
                            }
                        }

                        dataRow[fieldName] = value;
                    }

                    result.Add(dataRow);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Excel 시트 읽기 실패: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// 단일 CSV 파일(시트)에서 데이터 불러오기
        /// 불러온 데이터 개수를 반환
        /// </summary>
        private static int ImportSingleSheet(string filePath, DB targetDB)
        {
            // 파일명에서 데이터 타입 감지
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            Type dataType = GetDataTypeFromSheetName(fileName);
            
            if (dataType == null)
            {
                Debug.LogError($"파일명에서 데이터 타입을 감지할 수 없습니다: {fileName}");
                return 0;
            }

            Debug.Log($"데이터 타입 감지됨: {dataType.Name}, 파일명: {fileName}");

            // CSV 파싱
            var dataList = ParseCSVData(filePath);
            
            if (dataList.Count == 0)
            {
                Debug.LogWarning("파일에 데이터가 없습니다");
                return 0;
            }

            // DB에 데이터 추가
            AddDataToDB(dataType, dataList, targetDB);

            EditorUtility.SetDirty(targetDB);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"성공적으로 불러옴: {dataList.Count}개의 {dataType.Name} 데이터");
            return dataList.Count;
        }

        /// <summary>
        /// CSV 파일에서 데이터를 파싱하여 리스트로 반환
        /// 첫 번째 행: 필드명
        /// 두 번째 행부터: 데이터 값들
        /// </summary>
        private static List<Dictionary<string, string>> ParseCSVData(string filePath)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            // 파일 접근 가능 여부 확인
            if (!File.Exists(filePath))
            {
                Debug.LogError($"파일을 찾을 수 없습니다: {filePath}");
                return result;
            }

            // 파일이 다른 프로그램에서 사용 중인지 확인
            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // 파일 접근 가능
                }
            }
            catch (System.IO.IOException ex)
            {
                Debug.LogError($"파일이 다른 프로그램에서 사용 중입니다: {ex.Message}");
                return result;
            }

                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
            
            if (lines.Length < 2)
            {
                Debug.LogError("CSV 파일은 최소 필드 행과 한 개의 데이터 행이 필요합니다");
                return result;
            }

            // 첫 번째 행: 필드명 파싱
            string[] fieldNames = ParseCSVLine(lines[0]);
            Debug.Log($"필드 발견: {string.Join(", ", fieldNames)}");

            // 두 번째 행부터: 데이터 값 파싱
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);
                
                if (values.Length != fieldNames.Length)
                {
                    Debug.LogWarning($"행 {i}의 컬럼 수가 {values.Length}개입니다. 예상: {fieldNames.Length}개. 건너뜁니다.");
                    continue;
                }

                Dictionary<string, string> dataRow = new Dictionary<string, string>();
                for (int j = 0; j < fieldNames.Length; j++)
                {
                    string fieldName = NormalizeFieldName(fieldNames[j]);
                    dataRow[fieldName] = values[j];
                }
                
                result.Add(dataRow);
            }

            return result;
        }

        /// <summary>
        /// 시트 이름으로 데이터 타입 감지
        /// 언더스코어 형식의 시트명을 PascalCase 클래스명으로 변환
        /// ex) train_data -> TrainData, monster_data -> MonsterData
        /// </summary>
        private static Type GetDataTypeFromSheetName(string sheetName)
        {
            string normalizedName = sheetName.Replace("_", "").Replace("-", "");
            
            // Assembly에서 모든 데이터 타입 검색
            Assembly assembly = Assembly.GetAssembly(typeof(TrainData));
            Type[] types = assembly.GetTypes();
            
            foreach (Type type in types)
            {
                if (type.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// 필드명 정규화 (camelCase/PascalCase로 변환)
        /// </summary>
        private static string NormalizeFieldName(string fieldName)
        {
            // 언더스코어 제거하고 camelCase로 변환
            string[] parts = fieldName.Split(new char[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0)
                return fieldName;

            string result = parts[0].ToLower(); // 첫 글자는 소문자
            
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    result += char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }

            return result;
        }

        /// <summary>
        /// 파싱된 데이터를 해당 타입의 객체로 변환하여 DB에 추가
        /// ID가 이미 존재하는 경우 덮어쓰기
        /// </summary>
        private static void AddDataToDB(Type dataType, List<Dictionary<string, string>> dataList, DB targetDB)
        {
            // DB에서 해당 타입의 리스트 찾기
            object dataListContainer = GetDataListFromDB(dataType, targetDB);
            
            if (dataListContainer == null)
            {
                Debug.LogError($"DB에서 {dataType.Name} 타입의 데이터 리스트를 찾을 수 없습니다");
                return;
            }

            // 리플렉션으로 리스트 메서드 가져오기
            Type listType = dataListContainer.GetType();
            MethodInfo addMethod = listType.GetMethod("Add");
            MethodInfo getItemMethod = listType.GetProperty("Item")?.GetGetMethod();
            MethodInfo countMethod = listType.GetProperty("Count")?.GetGetMethod();
            
            if (addMethod == null || countMethod == null)
            {
                Debug.LogError($"데이터 리스트의 메서드를 찾을 수 없습니다");
                return;
            }

            // ID 필드 가져오기
            FieldInfo idField = dataType.GetField("id", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            // 각 데이터 행을 객체로 변환
            foreach (var dataRow in dataList)
            {
                if (dataRow.Count == 0) continue;

                object dataObject = CreateDataObjectFromRow(dataType, dataRow);
                
                if (dataObject == null) continue;

                // ID로 기존 데이터 찾기
                string newId = "";
                if (idField != null)
                {
                    newId = idField.GetValue(dataObject)?.ToString() ?? "";
                }

                if (!string.IsNullOrEmpty(newId))
                {
                    int count = (int)countMethod.Invoke(dataListContainer, null);
                    bool found = false;

                    // 기존 데이터 찾기
                    for (int i = 0; i < count; i++)
                    {
                        object existingItem = getItemMethod.Invoke(dataListContainer, new object[] { i });
                        if (existingItem != null && idField != null)
                        {
                            string existingId = idField.GetValue(existingItem)?.ToString() ?? "";
                            if (existingId == newId)
                            {
                                // 기존 데이터 덮어쓰기
                                CopyDataToExistingObject(existingItem, dataObject, dataType);
                                found = true;
                                Debug.Log($"데이터 업데이트: {existingId}");
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        // 새 데이터 추가
                        addMethod.Invoke(dataListContainer, new object[] { dataObject });
                        Debug.Log($"새 데이터 추가: {newId}");
                    }
                }
                else
                {
                    // ID가 없으면 그냥 추가
                    addMethod.Invoke(dataListContainer, new object[] { dataObject });
                    Debug.Log("ID가 없는 데이터 추가");
                }
            }
        }

        /// <summary>
        /// 기존 객체에 새 데이터 복사 (중첩된 구조체 포함)
        /// </summary>
        private static void CopyDataToExistingObject(object existingObj, object newObj, Type objectType)
        {
            // 모든 필드 복사
            FieldInfo[] fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
            foreach (var field in fields)
            {
                try
                {
                    object newValue = field.GetValue(newObj);
                    
                    // 값 타입인 경우
                    if (field.FieldType.IsValueType)
                    {
                        field.SetValue(existingObj, newValue);
                    }
                    // 참조 타입인 경우
                    else
                    {
                        // 구조체나 값 타입인 nested field의 경우 별도 처리
                        if (newValue != null)
                        {
                            // 중첩된 값 타입(구조체) 처리
                            if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
                            {
                                // 구조체의 모든 필드를 복사
                                CopyNestedStructValue(existingObj, newValue, field, field.FieldType);
                            }
                            else
                            {
                                field.SetValue(existingObj, newValue);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"필드 {field.Name} 복사 실패: {e.Message}");
                }
            }

            // 부모 타입의 필드도 복사
            Type baseType = objectType.BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                CopyDataToExistingObject(existingObj, newObj, baseType);
            }
        }

        /// <summary>
        /// 중첩된 구조체 값 복사
        /// </summary>
        private static void CopyNestedStructValue(object targetObj, object structValue, FieldInfo field, Type structType)
        {
            if (structValue == null) return;

            // 기존 구조체 값 가져오기
            object existingStruct = field.GetValue(targetObj);
            
            // 구조체의 모든 필드 복사
            FieldInfo[] structFields = structType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            
            foreach (var structField in structFields)
            {
                try
                {
                    object newFieldValue = structField.GetValue(structValue);
                    structField.SetValue(existingStruct, newFieldValue);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"구조체 필드 {structField.Name} 복사 실패: {e.Message}");
                }
            }
            
            // 변경된 구조체를 다시 설정
            field.SetValue(targetObj, existingStruct);
        }

        /// <summary>
        /// DB에서 해당 타입의 데이터 리스트 가져오기
        /// </summary>
        private static object GetDataListFromDB(Type dataType, DB db)
        {
            Type dbType = typeof(DB);
            FieldInfo[] fields = dbType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (FieldInfo field in fields)
            {
                // List<> 타입인지 확인
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type listElementType = field.FieldType.GetGenericArguments()[0];
                    
                    // 타입 매칭
                    if (listElementType == dataType || dataType.IsSubclassOf(listElementType))
                    {
                        return field.GetValue(db);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 데이터 행을 이용하여 객체 생성
        /// </summary>
        private static object CreateDataObjectFromRow(Type objectType, Dictionary<string, string> dataRow)
        {
            try
            {
                object instance = Activator.CreateInstance(objectType);

                // 모든 필드에 대해 데이터 설정
                foreach (var kvp in dataRow)
                {
                    SetFieldValue(instance, kvp.Key, kvp.Value, objectType);
                }

                return instance;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"행에서 객체 생성 실패: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 객체의 필드에 값 설정 (리플렉션 사용)
        /// 중첩된 구조체 필드도 지원 (예: MonsterStatusData.maxHp)
        /// </summary>
        private static void SetFieldValue(object instance, string fieldName, string value, Type objectType)
        {
            // 중첩 필드 체크 (예: "monsterStatusData.maxHp")
            if (fieldName.Contains("."))
            {
                SetNestedFieldValue(instance, fieldName, value, objectType);
                return;
            }

            // 현재 타입과 부모 타입을 모두 확인
            Type currentType = objectType;
            
            while (currentType != null)
            {
                FieldInfo field = currentType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                
                if (field != null)
                {
                    try
                    {
                        object convertedValue = ConvertValueToType(value, field.FieldType);
                        field.SetValue(instance, convertedValue);
                        return;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"필드 {fieldName}에 값 {value} 설정 실패: {e.Message}");
                    }
                    return;
                }

                currentType = currentType.BaseType;
            }

            Debug.LogWarning($"필드를 찾을 수 없습니다: {fieldName} (타입: {objectType.Name})");
        }

        /// <summary>
        /// 중첩된 구조체 필드에 값 설정
        /// </summary>
        private static void SetNestedFieldValue(object instance, string fieldPath, string value, Type objectType)
        {
            string[] parts = fieldPath.Split('.');
            if (parts.Length < 2)
            {
                Debug.LogWarning($"잘못된 중첩 필드 경로: {fieldPath}");
                return;
            }

            try
            {
                // 현재 타입과 부모 타입에서 첫 번째 필드 찾기
                Type currentType = objectType;
                FieldInfo parentField = null;
                
                while (currentType != null && parentField == null)
                {
                    parentField = currentType.GetField(parts[0], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    if (parentField != null) break;
                    currentType = currentType.BaseType;
                }

                if (parentField == null)
                {
                    Debug.LogWarning($"부모 필드를 찾을 수 없습니다: {parts[0]}");
                    return;
                }

                // 부모 필드의 값 가져오기 (구조체이므로 boxing/unboxing 필요)
                object parentValue = parentField.GetValue(instance);
                
                if (parentValue == null)
                {
                    // 구조체인 경우 새 인스턴스 생성
                    if (parentField.FieldType.IsValueType)
                    {
                        parentValue = Activator.CreateInstance(parentField.FieldType);
                        parentField.SetValue(instance, parentValue);
                    }
                    else
                    {
                        Debug.LogWarning($"부모 필드가 null입니다: {parts[0]}");
                        return;
                    }
                }

                // 중첩된 필드 찾기
                Type nestedType = parentField.FieldType;
                FieldInfo nestedField = nestedType.GetField(parts[1], BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                if (nestedField == null)
                {
                    Debug.LogWarning($"중첩 필드를 찾을 수 없습니다: {parts[1]} in {nestedType.Name}");
                    return;
                }

                // 값 변환 및 설정
                object convertedValue = ConvertValueToType(value, nestedField.FieldType);
                nestedField.SetValue(parentValue, convertedValue);

                // 변경된 구조체를 다시 설정 (구조체는 값 복사이므로)
                if (parentField.FieldType.IsValueType)
                {
                    parentField.SetValue(instance, parentValue);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"중첩 필드 설정 실패: {fieldPath} = {value}, 오류: {e.Message}");
            }
        }

        /// <summary>
        /// 문자열 값을 적절한 타입으로 변환
        /// </summary>
        private static object ConvertValueToType(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (targetType.IsValueType)
                {
                    return Activator.CreateInstance(targetType);
                }
                return null;
            }

            // Nullable 타입 처리
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            // 타입별 변환
            if (targetType == typeof(string))
            {
                return value;
            }
            else if (targetType == typeof(int))
            {
                if (int.TryParse(value, out int result))
                    return result;
            }
            else if (targetType == typeof(float))
            {
                if (float.TryParse(value, out float result))
                    return result;
            }
            else if (targetType == typeof(double))
            {
                if (double.TryParse(value, out double result))
                    return result;
            }
            else if (targetType == typeof(bool))
            {
                return bool.Parse(value.ToLower());
            }
            else if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value, true);
            }
            else if (targetType == typeof(Vector2))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 2 && float.TryParse(parts[0].Trim(), out float x) && float.TryParse(parts[1].Trim(), out float y))
                {
                    return new Vector2(x, y);
                }
            }
            else if (targetType == typeof(Vector3))
            {
                string[] parts = value.Split(',');
                if (parts.Length == 3 && float.TryParse(parts[0].Trim(), out float x) && float.TryParse(parts[1].Trim(), out float y) && float.TryParse(parts[2].Trim(), out float z))
                {
                    return new Vector3(x, y, z);
                }
            }
            else if (targetType == typeof(int[]) || targetType == typeof(float[]) || targetType == typeof(string[]))
            {
                string[] parts = value.Split(',');
                if (targetType == typeof(int[]))
                {
                    int[] array = new int[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        int.TryParse(parts[i].Trim(), out array[i]);
                    }
                    return array;
                }
                else if (targetType == typeof(float[]))
                {
                    float[] array = new float[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        float.TryParse(parts[i].Trim(), out array[i]);
                    }
                    return array;
                }
                else if (targetType == typeof(string[]))
                {
                    string[] array = new string[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        array[i] = parts[i].Trim();
                    }
                    return array;
                }
            }
            else if (targetType.IsValueType) // 구조체 처리
            {
                return Activator.CreateInstance(targetType);
            }

            return Activator.CreateInstance(targetType);
        }

        /// <summary>
        /// 데이터 리스트에 객체 추가
        /// </summary>
        private static void AddToDataList(object listContainer, object dataObject)
        {
            Type listType = listContainer.GetType();
            MethodInfo addMethod = listType.GetMethod("Add");
            
            if (addMethod != null)
            {
                addMethod.Invoke(listContainer, new object[] { dataObject });
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// CSV 라인 파싱 (따옴표 처리 포함)
        /// </summary>
        private static string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.Trim());
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            result.Add(currentField.Trim());
            return result.ToArray();
        }

        /// <summary>
        /// DB 인스턴스 찾기
        /// </summary>
        private static DB FindDBInstance()
        {
            DB db = Resources.Load<DB>("DB");
            if (db == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:DB");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    db = AssetDatabase.LoadAssetAtPath<DB>(path);
                }
            }

            if (db == null)
            {
                Debug.LogError("DB asset not found. Please create a DB asset first.");
            }

            return db;
        }

        #endregion
    }
}

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Excel Import View - Excel 데이터 가져오기 전용 창
    /// </summary>
    public class ExcelImportView : EditorWindow
    {
        private string targetExcelFile = "";
        private DB targetDB;
        
        public static void ShowWindow()
        {
            ExcelImportView window = GetWindow<ExcelImportView>("Excel 데이터 가져오기");
            window.minSize = new Vector2(600, 400);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Excel 데이터 관리", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            DrawImportTab();
        }
        
        private void DrawImportTab()
        {
            GUILayout.Label("엑셀/CSV 파일에서 데이터 불러오기", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // 1. 파일 선택 영역
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("1. 파일 선택", EditorStyles.boldLabel);
            GUILayout.Label("엑셀 파일:", EditorStyles.label);
            EditorGUILayout.BeginHorizontal();
            targetExcelFile = EditorGUILayout.TextField(targetExcelFile);
            if (GUILayout.Button("파일 찾기", GUILayout.Width(80)))
            {
                string filePath = EditorUtility.OpenFilePanelWithFilters(
                    "엑셀 파일 선택",
                    "",
                    new string[] { "Excel 파일", "xlsx,xlsm", "모든 파일", "*" }
                );
                if (!string.IsNullOrEmpty(filePath))
                {
                    targetExcelFile = filePath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(targetExcelFile))
            {
                EditorGUILayout.HelpBox("선택된 파일: " + Path.GetFileName(targetExcelFile), MessageType.Info);
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // 2. DB 선택 영역
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("2. DB 선택", EditorStyles.boldLabel);
            targetDB = (DB)EditorGUILayout.ObjectField("대상 DB:", targetDB, typeof(DB), false);
            
            if (targetDB == null)
            {
                EditorGUILayout.HelpBox("데이터를 불러올 DB를 선택해주세요.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"DB 연결됨: {AssetDatabase.GetAssetPath(targetDB)}", MessageType.Info);
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(20);
            
            // 3. 불러오기 버튼
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(targetExcelFile) || targetDB == null);
            
            // 큰 불러오기 버튼
            if (GUILayout.Button("데이터 불러오기", GUILayout.Height(40)))
            {
                ImportData();
            }
            
            EditorGUI.EndDisabledGroup();
            
            GUILayout.Space(10);
            
            // 4. 도움말
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("사용 방법", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "📋 Excel 파일 형식 가이드:\n\n" +
                "1️⃣ 첫 번째 행: 필드명 (예: id, trainName, maxHp)\n" +
                "2️⃣ 두 번째 행부터: 데이터 값들\n" +
                "3️⃣ 시트명 형식: 클래스명_언더스코어 (예: train_data → TrainData)\n" +
                "4️⃣ 필드명은 언더스코어 지원 (예: train_name → trainName)\n\n" +
                "💡 팁: 여러 시트를 포함하여 모든 데이터를 한 번에 불러올 수 있습니다.",
                MessageType.Info);
            GUILayout.EndVertical();
        }
        
        private void ImportData()
        {
            if (string.IsNullOrEmpty(targetExcelFile))
            {
                EditorUtility.DisplayDialog("오류", "엑셀 파일을 선택해주세요.", "확인");
                return;
            }
            
            if (targetDB == null)
            {
                EditorUtility.DisplayDialog("오류", "대상 DB를 선택해주세요.", "확인");
                return;
            }
            
            try
            {
                // 데이터 불러오기
                int importedCount = ExcelImporter.ImportAllDataFromExcel(targetExcelFile, targetDB);
                
                if (importedCount > 0)
                {
                    string fileName = Path.GetFileName(targetExcelFile);
                    string dataType = Path.GetFileNameWithoutExtension(targetExcelFile);
                    
                    EditorUtility.DisplayDialog(
                        "불러오기 완료", 
                        $"데이터 불러오기가 완료되었습니다!\n\n" +
                        $"파일: {fileName}\n" +
                        $"데이터 타입: {dataType}\n" +
                        $"불러온 개수: {importedCount}개",
                        "확인");
                }
                else
                {
                    EditorUtility.DisplayDialog("오류", "데이터를 불러올 수 없습니다. 파일 형식과 데이터를 확인해주세요.", "확인");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"데이터 불러오기 실패: {e.Message}", "확인");
                Debug.LogError(e);
            }
        }
    }
}