#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class RangeTrainRow : TrainRow
	{
		public float attackRange;
		public int attackDamage;
		public int attackCount;
		public float attackInterval;
		public string rangeProjectilePrefabId;

	public override void FromExcelRow(IRow row)
	{
		base.FromExcelRow(row);
		float.TryParse(row.GetCell(10)?.ToString(), out attackRange);
		int.TryParse(row.GetCell(11)?.ToString(), out attackDamage);
		int.TryParse(row.GetCell(12)?.ToString(), out attackCount);
		float.TryParse(row.GetCell(13)?.ToString(), out attackInterval);
		rangeProjectilePrefabId = row.GetCell(14)?.ToString();
	}

	public override void ToExcelRow(IRow row)
	{
		base.ToExcelRow(row);
		Set(row, 10, attackRange);
		Set(row, 11, attackDamage);
		Set(row, 12, attackCount);
		Set(row, 13, attackInterval);
		Set(row, 14, rangeProjectilePrefabId);
	}
	}
}
#endif