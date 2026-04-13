#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace TrainDefense.Editor.DataImport
{
	/// <summary>
	/// 헤더 이름 → 컬럼 인덱스 매핑
	/// </summary>
	public class HeaderMap
	{
		private readonly Dictionary<string, int> _map = new();

		public HeaderMap(List<string> headers)
		{
			for (int i = 0; i < headers.Count; i++)
			{
				var key = headers[i]?.Trim().ToLowerInvariant() ?? string.Empty;
				if (!string.IsNullOrEmpty(key) && !_map.ContainsKey(key))
					_map[key] = i;
			}
		}

		public int IndexOf(string columnName)
		{
			var key = columnName?.Trim().ToLowerInvariant() ?? string.Empty;
			return _map.TryGetValue(key, out int idx) ? idx : -1;
		}

		public string GetString(IRow row, string columnName)
		{
			int idx = IndexOf(columnName);
			return idx >= 0 ? row.GetCell(idx)?.ToString() : null;
		}

		public int GetInt(IRow row, string columnName)
		{
			int.TryParse(GetString(row, columnName), out int v);
			return v;
		}

		public float GetFloat(IRow row, string columnName)
		{
			float.TryParse(GetString(row, columnName), out float v);
			return v;
		}

		public bool GetBool(IRow row, string columnName)
		{
			bool.TryParse(GetString(row, columnName), out bool v);
			return v;
		}

		public T GetEnum<T>(IRow row, string columnName) where T : struct, System.Enum
		{
			System.Enum.TryParse<T>(GetString(row, columnName), true, out T v);
			return v;
		}

		public bool HasColumn(string columnName) => IndexOf(columnName) >= 0;

		public void SetCell(IRow row, string columnName, object value)
		{
			int idx = IndexOf(columnName);
			if (idx < 0) return;
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty);
			else if (value is int i) cell.SetCellValue(i);
			else if (value is bool b) cell.SetCellValue(b);
			else if (value is float f) cell.SetCellValue(f);
			else cell.SetCellValue(value.ToString());
		}
	}

	public static class ExcelReadUtil
	{
		public static List<IRow> ReadRows(string excelPath, string sheetName)
		{
			using var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			IWorkbook wb = GetWorkbook(excelPath, fs);
			var sheet = GetSheetCaseInsensitive(wb, sheetName);
			List<IRow> rows = new List<IRow>();
			if (sheet == null) return rows;
			for (int i = 1; i <= sheet.LastRowNum; i++)
			{
				var row = sheet.GetRow(i);
				if (row != null) rows.Add(row);
			}
			return rows;
		}

		public static List<string> ReadHeaders(string excelPath, string sheetName)
		{
			using var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			IWorkbook wb = GetWorkbook(excelPath, fs);
			var sheet = GetSheetCaseInsensitive(wb, sheetName);
			List<string> headers = new List<string>();
			if (sheet == null) return headers;
			var headerRow = sheet.GetRow(0);
			if (headerRow == null) return headers;
			for (int i = 0; i < headerRow.LastCellNum; i++)
			{
				var cell = headerRow.GetCell(i);
				headers.Add(cell?.ToString() ?? string.Empty);
			}
			return headers;
		}

		public static HeaderMap ReadHeaderMap(string excelPath, string sheetName)
		{
			return new HeaderMap(ReadHeaders(excelPath, sheetName));
		}

		private static ISheet GetSheetCaseInsensitive(IWorkbook wb, string sheetName)
		{
			for (int i = 0; i < wb.NumberOfSheets; i++)
			{
				if (string.Equals(wb.GetSheetName(i), sheetName, System.StringComparison.OrdinalIgnoreCase))
				{
					return wb.GetSheetAt(i);
				}
			}
			return null;
		}

		private static IWorkbook GetWorkbook(string path, FileStream fs)
		{
			var ext = Path.GetExtension(path).ToLower();
			return ext == ".xls" ? new HSSFWorkbook(fs) : new XSSFWorkbook(fs);
		}
	}
}
#endif


