#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class RangeTrainUpgradeRow : TrainUpgradeRow
	{
		public int attackDamage;
		public float attackRange;
		public float attackArea;
		public int attackCount;
		public float attackInterval;
		public float criticalChance;
		public float criticalDamage;

		public override void FromExcelRow(IRow row, HeaderMap map)
		{
			base.FromExcelRow(row, map);
			attackDamage = map.GetInt(row, "attack_damage");
			attackRange = map.GetFloat(row, "attack_range");
			attackArea = map.GetFloat(row, "attack_area");
			attackCount = map.GetInt(row, "attack_count");
			attackInterval = map.GetFloat(row, "attack_interval");
			criticalChance = map.GetFloat(row, "critical_chance");
			criticalDamage = map.GetFloat(row, "critical_damage");
		}

		public override void ToExcelRow(IRow row, HeaderMap map)
		{
			base.ToExcelRow(row, map);
			map.SetCell(row, "attack_damage", attackDamage);
			map.SetCell(row, "attack_range", attackRange);
			map.SetCell(row, "attack_area", attackArea);
			map.SetCell(row, "attack_count", attackCount);
			map.SetCell(row, "attack_interval", attackInterval);
			map.SetCell(row, "critical_chance", criticalChance);
			map.SetCell(row, "critical_damage", criticalDamage);
		}
	}
}
#endif
