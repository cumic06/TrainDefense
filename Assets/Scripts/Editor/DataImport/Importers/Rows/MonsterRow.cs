#if UNITY_EDITOR
using NPOI.SS.UserModel;
using TrainDefense.Editor.DataImport;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class MonsterRow : IExcelRow
	{
		public string id;
		public string monsterName;
		public string description;

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			monsterName = row.GetCell(1)?.ToString();
			description = row.GetCell(2)?.ToString();
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, monsterName);
			Set(row, 2, description);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty); else cell.SetCellValue(value.ToString());
		}
	}
}
#endif


