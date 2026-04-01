#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TurretTrainUpgradeRow : TrainUpgradeRow
	{
		public int attackDamage;
		public float attackRange;
		public int attackCount;
		public float attackInterval;

		public override void FromExcelRow(IRow row)
		{
			base.FromExcelRow(row);
			if (row.LastCellNum > 5)
			{
				int.TryParse(row.GetCell(5)?.ToString(), out attackDamage);
				float.TryParse(row.GetCell(6)?.ToString(), out attackRange);
				int.TryParse(row.GetCell(7)?.ToString(), out attackCount);
				float.TryParse(row.GetCell(8)?.ToString(), out attackInterval);
			}
		}

		public override void ToExcelRow(IRow row)
		{
			base.ToExcelRow(row);
			Set(row, 5, attackDamage);
			Set(row, 6, attackRange);
			Set(row, 7, attackCount);
			Set(row, 8, attackInterval);
		}
	}
}
#endif