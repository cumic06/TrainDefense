#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TurretTrainRow : TrainRow
	{
		public float attackRange;
		public int attackDamage;
		public int attackCount;
		public float attackDelay;
		public string turretProjectilePrefabId;

	public override void FromExcelRow(IRow row)
	{
		base.FromExcelRow(row);
		float.TryParse(row.GetCell(7)?.ToString(), out attackRange);
		int.TryParse(row.GetCell(8)?.ToString(), out attackDamage);
		int.TryParse(row.GetCell(9)?.ToString(), out attackCount);
		float.TryParse(row.GetCell(10)?.ToString(), out attackDelay);
		turretProjectilePrefabId = row.GetCell(11)?.ToString();
	}

	public override void ToExcelRow(IRow row)
	{
		base.ToExcelRow(row);
		Set(row, 7, attackRange);
		Set(row, 8, attackDamage);
		Set(row, 9, attackCount);
		Set(row, 10, attackDelay);
		Set(row, 11, turretProjectilePrefabId);
	}
	}
}
#endif