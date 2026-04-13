#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace TrainDefense.Editor.DataImport
{
	public static class ExcelWriter
	{
		public static void WriteToSheet<T>(string excelPath, string sheetName, IEnumerable<T> rows) where T : IExcelRow
		{
			IWorkbook wb;
			if (File.Exists(excelPath))
			{
				using var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				wb = GetWorkbook(excelPath, fs);
			}
			else
			{
				wb = CreateWorkbook(excelPath);
			}

			var sheet = GetSheetCaseInsensitive(wb, sheetName) ?? wb.CreateSheet(sheetName);

			// 헤더 행에서 HeaderMap 생성
			var headerRow = sheet.GetRow(0);
			var headers = new List<string>();
			if (headerRow != null)
			{
				for (int i = 0; i < headerRow.LastCellNum; i++)
					headers.Add(headerRow.GetCell(i)?.ToString() ?? string.Empty);
			}
			var map = new HeaderMap(headers);

			int rowIndex = 1; // 0 is header, assumed ensured by ExcelTemplate
			foreach (var r in rows)
			{
				var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
				r.ToExcelRow(row, map);
				rowIndex++;
			}

			WriteWorkbook(excelPath, wb);
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
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
			using var ms = new System.IO.MemoryStream();
			wb.Write(ms);
			File.WriteAllBytes(path, ms.ToArray());
		}
	}
}
#endif


