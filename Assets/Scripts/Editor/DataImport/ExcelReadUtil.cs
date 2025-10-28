#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace TrainDefense.Editor.DataImport
{
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


