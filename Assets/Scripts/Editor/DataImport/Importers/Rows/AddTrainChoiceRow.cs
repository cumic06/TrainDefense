#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class AddTrainChoiceRow : IExcelRow
	{
		public string id;
		public string trainDataId;
		public int weight;

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			trainDataId = row.GetCell(1)?.ToString();
			int.TryParse(row.GetCell(2)?.ToString(), out weight);
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, trainDataId);
			Set(row, 2, weight);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty);
			else if (value is int i) cell.SetCellValue(i);
			else cell.SetCellValue(value.ToString());
		}
	}
}
#endif

