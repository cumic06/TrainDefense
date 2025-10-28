#if UNITY_EDITOR
using NPOI.SS.UserModel;
using TrainDefense.Editor.DataImport;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class ChoiceRow : IExcelRow
	{
		public string id;
		public string choiceType;
		public string targetTrainId;

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			choiceType = row.GetCell(1)?.ToString();
			targetTrainId = row.GetCell(2)?.ToString();
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, choiceType);
			Set(row, 2, targetTrainId);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			cell.SetCellValue(value?.ToString() ?? string.Empty);
		}
	}
}
#endif


