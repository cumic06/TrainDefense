#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;

namespace TrainDefense.Editor.DataImport
{
	public static class ExcelTemplate
	{
		public static bool SheetExists(string excelPath, string sheetName)
		{
			if (!File.Exists(excelPath)) return false;
			using var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var wb = GetWorkbook(excelPath, fs);
			for (int i = 0; i < wb.NumberOfSheets; i++)
			{
				if (string.Equals(wb.GetSheetName(i), sheetName, System.StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		public static void EnsureSheetWithHeaders(string excelPath, string sheetName, IEnumerable<string> headers, IEnumerable<IExcelRow> exampleRows = null)
		{
			IWorkbook wb;
			ISheet sheet;

			if (File.Exists(excelPath))
			{
				using var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				wb = GetWorkbook(excelPath, fs);
			}
			else
			{
				wb = CreateWorkbook(excelPath);
			}

			sheet = GetSheetCaseInsensitive(wb, sheetName) ?? wb.CreateSheet(sheetName);

			// Header row
			var headerRow = sheet.GetRow(0) ?? sheet.CreateRow(0);
			int col = 0;
			foreach (var h in headers)
			{
				var cell = headerRow.GetCell(col) ?? headerRow.CreateCell(col);
				cell.SetCellValue(h);
				col++;
			}

			// Optional example rows
			if (exampleRows != null)
			{
				int r = 1;
				foreach (var ex in exampleRows)
				{
					var row = sheet.GetRow(r) ?? sheet.CreateRow(r);
					ex.ToExcelRow(row);
					r++;
				}
			}

			WriteWorkbook(excelPath, wb);
		}

		public static bool SheetHasData(string excelPath, string sheetName)
		{
			if (!File.Exists(excelPath)) return false;
			using var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var wb = GetWorkbook(excelPath, fs);
			var sheet = GetSheetCaseInsensitive(wb, sheetName);
			if (sheet == null) return false;
			return sheet.LastRowNum >= 1; // any row beyond header
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

		private static IWorkbook CreateWorkbook(string path)
		{
			var ext = Path.GetExtension(path).ToLower();
			return ext == ".xls" ? new HSSFWorkbook() : new XSSFWorkbook();
		}

		private static void WriteWorkbook(string path, IWorkbook wb)
		{
			// ensure directory
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
			using var ms = new MemoryStream();
			wb.Write(ms);
			File.WriteAllBytes(path, ms.ToArray());
		}
	}
}
#endif


