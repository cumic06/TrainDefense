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

			// Header row — 이미 있는 헤더는 자리와 이름을 그대로 둔다.
			// 임포트는 열 이름으로 읽으므로 시트의 열 순서가 코드와 달라도 되는데, 위치 기준으로 덮어쓰면
			// 이름만 바뀌고 데이터는 그대로라 이후 읽기가 엉뚱한 열을 읽는다(stage_data에 name_key가 없을 때 전부 한 칸씩 밀림).
			// 시트에 없는 헤더만 맨 뒤에 덧붙인다.
			var headerRow = sheet.GetRow(0) ?? sheet.CreateRow(0);
			var headerList = new List<string>();
			int nextColumn = 0;
			for (int i = 0; i < headerRow.LastCellNum; i++)
			{
				string existing = headerRow.GetCell(i)?.ToString()?.Trim();
				if (string.IsNullOrEmpty(existing)) continue;

				headerList.Add(existing);
				nextColumn = i + 1;
			}

			foreach (var h in headers)
			{
				if (headerList.Exists(existing => string.Equals(existing, h, System.StringComparison.OrdinalIgnoreCase)))
					continue;

				var cell = headerRow.GetCell(nextColumn) ?? headerRow.CreateCell(nextColumn);
				cell.SetCellValue(h);
				headerList.Add(h);
				nextColumn++;
			}

			// Optional example rows
			if (exampleRows != null)
			{
				var map = new HeaderMap(headerList);
				int r = 1;
				foreach (var ex in exampleRows)
				{
					var row = sheet.GetRow(r) ?? sheet.CreateRow(r);
					ex.ToExcelRow(row, map);
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


