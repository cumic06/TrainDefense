using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Cumic.Excel
{
    /// <summary>
    /// NPOI를 사용한 Excel 파일 로더
    /// 첫 번째 행을 헤더로 사용하고 나머지 행들을 Dictionary 리스트로 반환
    /// Sheet별 개별 로드 지원
    /// </summary>
    public static class ExcelLoader
    {
        /// <summary>
        /// Excel 파일에서 모든 시트 이름을 가져옵니다
        /// </summary>
        /// <param name="filePath">Excel 파일 경로</param>
        /// <returns>시트 이름 목록</returns>
        public static List<string> GetSheetNames(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Excel 파일을 찾을 수 없습니다: {filePath}");
                return new List<string>();
            }

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var workbook = GetWorkbook(filePath, fileStream);
                
                var sheetNames = new List<string>();
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    sheetNames.Add(workbook.GetSheetName(i));
                }
                
                return sheetNames;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Excel 파일 읽기 실패: {filePath}\n{ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Excel 파일에서 특정 시트를 이름으로 로드합니다
        /// </summary>
        /// <param name="filePath">Excel 파일 경로</param>
        /// <param name="sheetName">시트 이름</param>
        /// <returns>행 데이터 리스트 (첫 번째 행은 헤더)</returns>
        public static List<Dictionary<string, object>> LoadSheet(string filePath, string sheetName)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Excel 파일을 찾을 수 없습니다: {filePath}");
                return new List<Dictionary<string, object>>();
            }

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var workbook = GetWorkbook(filePath, fileStream);
                var sheet = workbook.GetSheet(sheetName);
                
                if (sheet == null)
                {
                    Debug.LogError($"시트를 찾을 수 없습니다: {sheetName}");
                    return new List<Dictionary<string, object>>();
                }

                return LoadSheetData(sheet);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Excel 시트 로드 실패: {filePath} - {sheetName}\n{ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Excel 파일에서 특정 시트를 인덱스로 로드합니다
        /// </summary>
        /// <param name="filePath">Excel 파일 경로</param>
        /// <param name="sheetIndex">시트 인덱스 (0부터 시작)</param>
        /// <returns>행 데이터 리스트 (첫 번째 행은 헤더)</returns>
        public static List<Dictionary<string, object>> LoadSheet(string filePath, int sheetIndex)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Excel 파일을 찾을 수 없습니다: {filePath}");
                return new List<Dictionary<string, object>>();
            }

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var workbook = GetWorkbook(filePath, fileStream);
                
                if (sheetIndex < 0 || sheetIndex >= workbook.NumberOfSheets)
                {
                    Debug.LogError($"시트 인덱스가 범위를 벗어났습니다: {sheetIndex} (총 시트 수: {workbook.NumberOfSheets})");
                    return new List<Dictionary<string, object>>();
                }

                var sheet = workbook.GetSheetAt(sheetIndex);
                return LoadSheetData(sheet);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Excel 시트 로드 실패: {filePath} - 인덱스 {sheetIndex}\n{ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Excel 파일의 모든 시트를 로드합니다
        /// </summary>
        /// <param name="filePath">Excel 파일 경로</param>
        /// <returns>시트 이름을 키로 하는 데이터 딕셔너리</returns>
        public static Dictionary<string, List<Dictionary<string, object>>> LoadAllSheets(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Excel 파일을 찾을 수 없습니다: {filePath}");
                return new Dictionary<string, List<Dictionary<string, object>>>();
            }

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var workbook = GetWorkbook(filePath, fileStream);
                
                var allSheetsData = new Dictionary<string, List<Dictionary<string, object>>>();
                
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var sheetName = workbook.GetSheetName(i);
                    var sheet = workbook.GetSheetAt(i);
                    var sheetData = LoadSheetData(sheet);
                    allSheetsData[sheetName] = sheetData;
                }
                
                return allSheetsData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Excel 파일 전체 로드 실패: {filePath}\n{ex.Message}");
                return new Dictionary<string, List<Dictionary<string, object>>>();
            }
        }

        /// <summary>
        /// 파일 확장자에 따라 적절한 Workbook을 생성합니다
        /// </summary>
        private static IWorkbook GetWorkbook(string filePath, FileStream fileStream)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            
            return extension switch
            {
                ".xlsx" => new XSSFWorkbook(fileStream),
                ".xls" => new HSSFWorkbook(fileStream),
                _ => throw new NotSupportedException($"지원하지 않는 파일 형식입니다: {extension}")
            };
        }

        /// <summary>
        /// 시트에서 데이터를 로드합니다 (첫 번째 행을 헤더로 사용)
        /// </summary>
        private static List<Dictionary<string, object>> LoadSheetData(ISheet sheet)
        {
            var data = new List<Dictionary<string, object>>();
            
            if (sheet.LastRowNum < 0)
            {
                Debug.LogWarning($"시트 '{sheet.SheetName}'에 데이터가 없습니다.");
                return data;
            }

            // 첫 번째 행을 헤더로 사용
            var headerRow = sheet.GetRow(0);
            if (headerRow == null)
            {
                Debug.LogWarning($"시트 '{sheet.SheetName}'의 첫 번째 행이 비어있습니다.");
                return data;
            }

            var headers = new List<string>();
            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                var headerValue = GetCellValue(cell);
                headers.Add(headerValue?.ToString() ?? $"Column_{i}");
            }

            // 데이터 행들을 처리
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                var rowData = new Dictionary<string, object>();
                
                for (int colIndex = 0; colIndex < headers.Count; colIndex++)
                {
                    var cell = row.GetCell(colIndex);
                    var cellValue = GetCellValue(cell);
                    rowData[headers[colIndex]] = cellValue;
                }
                
                data.Add(rowData);
            }

            Debug.Log($"시트 '{sheet.SheetName}'에서 {data.Count}행의 데이터를 로드했습니다.");
            return data;
        }

        /// <summary>
        /// 셀의 값을 적절한 타입으로 변환하여 반환합니다
        /// </summary>
        private static object GetCellValue(ICell cell)
        {
            if (cell == null) return null;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue : cell.NumericCellValue,
                CellType.Boolean => cell.BooleanCellValue,
                CellType.Formula => GetFormulaCellValue(cell),
                CellType.Blank => null,
                _ => cell.ToString()
            };
        }

        /// <summary>
        /// 수식 셀의 값을 계산하여 반환합니다
        /// </summary>
        private static object GetFormulaCellValue(ICell cell)
        {
            try
            {
                var evaluator = cell.Sheet.Workbook.GetCreationHelper().CreateFormulaEvaluator();
                var cellValue = evaluator.Evaluate(cell);
                
                return cellValue.CellType switch
                {
                    CellType.String => cellValue.StringValue,
                    CellType.Numeric => cellValue.NumberValue,
                    CellType.Boolean => cellValue.BooleanValue,
                    _ => cellValue.ToString()
                };
            }
            catch
            {
                return cell.ToString();
            }
        }
    }
}
