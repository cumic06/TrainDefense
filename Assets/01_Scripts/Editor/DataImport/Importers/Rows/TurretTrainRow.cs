#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TurretTrainRow : TrainRow
	{
		public float attackRange;
		public float attackArea;
		public int attackDamage;
		public int attackCount;
		public float attackInterval;
		public int targetCount;
		public string turretProjectilePrefabId;
		public float criticalChance;
		public float criticalDamage;
		public string passiveSkillDataId;

		public override void FromExcelRow(IRow row, HeaderMap map)
		{
			base.FromExcelRow(row, map);
			attackRange = map.GetFloat(row, "attack_range");
			attackArea = map.GetFloat(row, "attack_area");
			attackDamage = map.GetInt(row, "attack_damage");
			attackCount = map.GetInt(row, "attack_count");
			attackInterval = map.GetFloat(row, "attack_interval");
			targetCount = map.GetInt(row, "target_count");
			turretProjectilePrefabId = map.GetString(row, "turret_projectile_prefab_id");
			criticalChance = map.GetFloat(row, "critical_chance");
			criticalDamage = map.GetFloat(row, "critical_damage");
			passiveSkillDataId = map.GetString(row, "passive_skill_data_id");
		}

		public override void ToExcelRow(IRow row, HeaderMap map)
		{
			base.ToExcelRow(row, map);
			map.SetCell(row, "attack_range", attackRange);
			map.SetCell(row, "attack_area", attackArea);
			map.SetCell(row, "attack_damage", attackDamage);
			map.SetCell(row, "attack_count", attackCount);
			map.SetCell(row, "attack_interval", attackInterval);
			map.SetCell(row, "target_count", targetCount);
			map.SetCell(row, "turret_projectile_prefab_id", turretProjectilePrefabId);
			map.SetCell(row, "critical_chance", criticalChance);
			map.SetCell(row, "critical_damage", criticalDamage);
			map.SetCell(row, "passive_skill_data_id", passiveSkillDataId);
		}
	}
}
#endif
