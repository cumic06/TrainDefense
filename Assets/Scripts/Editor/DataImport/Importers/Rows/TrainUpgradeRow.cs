#if UNITY_EDITOR
using NPOI.SS.UserModel;
using TrainDefense.Editor.DataImport;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TrainUpgradeRow : IExcelRow
	{
		public string upgradeName;
		public string description;

		public void FromExcelRow(IRow row)
		{
			upgradeName = row.GetCell(0)?.ToString();
			description = row.GetCell(1)?.ToString();
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, upgradeName);
			Set(row, 1, description);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			cell.SetCellValue(value?.ToString() ?? string.Empty);
		}
	}
}
#endif


