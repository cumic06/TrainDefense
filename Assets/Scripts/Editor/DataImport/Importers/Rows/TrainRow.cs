#if UNITY_EDITOR
using NPOI.SS.UserModel;
using TrainDefense.Editor.DataImport;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TrainRow : IExcelRow
	{
		public string id;
		public string trainName;
		public string description;
		public bool isMainTrain;

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			trainName = row.GetCell(1)?.ToString();
			description = row.GetCell(2)?.ToString();
			bool.TryParse(row.GetCell(3)?.ToString(), out isMainTrain);
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, trainName);
			Set(row, 2, description);
			Set(row, 3, isMainTrain);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty);
			else if (value is bool b) cell.SetCellValue(b);
			else cell.SetCellValue(value.ToString());
		}
	}
}
#endif


