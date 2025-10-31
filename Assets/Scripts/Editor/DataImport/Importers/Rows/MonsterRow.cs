#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class MonsterRow : IExcelRow
	{
		public string id;
		public string name;
		public string description;
		public int maxHp;
		public int damage;
		public float moveSpeed;
		public float attackDelay;
		public int dropExpMin;
		public int dropExpMax;
		public int dropMoneyMin;
		public int dropMoneyMax;
		public float attackRange;
		public string prefabId;

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			name = row.GetCell(1)?.ToString();
			description = row.GetCell(2)?.ToString();
			int.TryParse(row.GetCell(3)?.ToString(), out maxHp);
			int.TryParse(row.GetCell(4)?.ToString(), out damage);
			float.TryParse(row.GetCell(5)?.ToString(), out moveSpeed);
			float.TryParse(row.GetCell(6)?.ToString(), out attackDelay);
			int.TryParse(row.GetCell(7)?.ToString(), out dropExpMin);
			int.TryParse(row.GetCell(8)?.ToString(), out dropExpMax);
			int.TryParse(row.GetCell(9)?.ToString(), out dropMoneyMin);
			int.TryParse(row.GetCell(10)?.ToString(), out dropMoneyMax);
			float.TryParse(row.GetCell(11)?.ToString(), out attackRange);
			prefabId = row.GetCell(12)?.ToString();
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, name);
			Set(row, 2, description);
			Set(row, 3, maxHp);
			Set(row, 4, damage);
			Set(row, 5, moveSpeed);
			Set(row, 6, attackDelay);
			Set(row, 7, dropExpMin);
			Set(row, 8, dropExpMax);
			Set(row, 9, dropMoneyMin);
			Set(row, 10, dropMoneyMax);
			Set(row, 11, attackRange);
			Set(row, 12, prefabId);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty); else cell.SetCellValue(value.ToString());
		}
	}
}
#endif


