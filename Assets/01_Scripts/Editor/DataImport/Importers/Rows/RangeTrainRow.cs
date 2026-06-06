#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class RangeTrainRow : TrainRow
	{
		public float attackRange;
		public float attackArea;
		public float attackDamage;
		public int attackCount;
		public float attackInterval;
		public string rangeProjectilePrefabId;
		public float criticalChance;
		public float criticalDamage;
		public float slowRate;

		public override void FromExcelRow(IRow row, HeaderMap map)
		{
			base.FromExcelRow(row, map);
			attackRange = map.GetFloat(row, "attack_range");
			attackArea = map.GetFloat(row, "attack_area");
			attackDamage = map.GetFloat(row, "attack_damage");
			attackCount = map.GetInt(row, "attack_count");
			attackInterval = map.GetFloat(row, "attack_interval");
			rangeProjectilePrefabId = map.GetString(row, "range_projectile_prefab_id");
			criticalChance = map.GetFloat(row, "critical_chance");
			criticalDamage = map.GetFloat(row, "critical_damage");
			slowRate = map.GetFloat(row, "slow_rate");
		}

		public override void ToExcelRow(IRow row, HeaderMap map)
		{
			base.ToExcelRow(row, map);
			map.SetCell(row, "attack_range", attackRange);
			map.SetCell(row, "attack_area", attackArea);
			map.SetCell(row, "attack_damage", attackDamage);
			map.SetCell(row, "attack_count", attackCount);
			map.SetCell(row, "attack_interval", attackInterval);
			map.SetCell(row, "range_projectile_prefab_id", rangeProjectilePrefabId);
			map.SetCell(row, "critical_chance", criticalChance);
			map.SetCell(row, "critical_damage", criticalDamage);
			map.SetCell(row, "slow_rate", slowRate);
		}
	}
}
#endif
