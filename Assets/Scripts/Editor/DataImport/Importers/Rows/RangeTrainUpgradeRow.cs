#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class RangeTrainUpgradeRow : TrainUpgradeRow
	{
		public float attackRange;
		public int attackDamage;
		public int attackCount;
		public float attackInterval;

		public override void FromExcelRow(IRow row)
		{
			base.FromExcelRow(row);
			if (row.LastCellNum > 2)
			{
				float.TryParse(row.GetCell(2)?.ToString(), out attackRange);
				int.TryParse(row.GetCell(3)?.ToString(), out attackDamage);
				int.TryParse(row.GetCell(4)?.ToString(), out attackCount);
				float.TryParse(row.GetCell(5)?.ToString(), out attackInterval);
			}
		}

		public override void ToExcelRow(IRow row)
		{
			base.ToExcelRow(row);
			Set(row, 2, attackRange);
			Set(row, 3, attackDamage);
			Set(row, 4, attackCount);
			Set(row, 5, attackInterval);
		}
	}
}
#endif

