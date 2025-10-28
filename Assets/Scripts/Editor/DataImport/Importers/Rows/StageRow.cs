#if UNITY_EDITOR
using NPOI.SS.UserModel;
using TrainDefense.Editor.DataImport;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class StageRow : IExcelRow
	{
		public string id;
		public string stageInspectionTime;
		public float stageEndTime;

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			stageInspectionTime = row.GetCell(1)?.ToString();
			float.TryParse(row.GetCell(2)?.ToString(), out stageEndTime);
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, stageInspectionTime);
			Set(row, 2, stageEndTime);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty);
			else if (value is float f) cell.SetCellValue(f);
			else cell.SetCellValue(value.ToString());
		}
	}
}
#endif


