#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TrainUpgradeRow : IExcelRow
	{
		public string upgradeName;
		public string description;

		public virtual void FromExcelRow(IRow row)
		{
			upgradeName = row.GetCell(0)?.ToString();
			description = row.GetCell(1)?.ToString();
		}

		public virtual void ToExcelRow(IRow row)
		{
			Set(row, 0, upgradeName);
			Set(row, 1, description);
		}

		protected static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty);
			else if (value is int i) cell.SetCellValue(i);
			else if (value is bool b) cell.SetCellValue(b);
			else if (value is float f) cell.SetCellValue(f);
			else cell.SetCellValue(value.ToString());
		}
	}
}
#endif


