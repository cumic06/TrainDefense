#if UNITY_EDITOR
using NPOI.SS.UserModel;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TrainSkillDataRow : IExcelRow
	{
		public string id;
		public TrainSkillType skillType;
		public float skillCooldown;
		public string skillIconId;
		public string skillProjectilePrefabId;
		public int skillProjectileDamage;
		public float skillProjectileRange;
		public int skillProjectileCount;
		public float skillBuffDuration;
		public StatType skillBuffStatType;
		public float skillBuffPercent;

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");

			var skillTypeValue = map.GetString(row, "skill_type");
			if (!System.Enum.TryParse(skillTypeValue, true, out skillType))
			{
				if (bool.TryParse(skillTypeValue, out bool hasSkill) && hasSkill)
				{
					skillType = TrainSkillType.Projectile;
				}
			}

			skillCooldown = map.GetFloat(row, "skill_cooldown");
			skillIconId = map.GetString(row, "skill_icon_id");
			skillProjectilePrefabId = map.GetString(row, "skill_projectile_prefab_id");
			skillProjectileDamage = map.GetInt(row, "skill_projectile_damage");
			skillProjectileRange = map.GetFloat(row, "skill_projectile_range");
			skillProjectileCount = map.GetInt(row, "skill_projectile_count");

			skillBuffDuration = map.GetFloat(row, "skill_buff_duration");
			var buffStatRaw = map.GetString(row, "skill_buff_stat_type");
			System.Enum.TryParse(buffStatRaw, true, out skillBuffStatType);
			skillBuffPercent = map.GetFloat(row, "skill_buff_percent");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "skill_type", skillType.ToString());
			map.SetCell(row, "skill_cooldown", skillCooldown);
			map.SetCell(row, "skill_icon_id", skillIconId);
			map.SetCell(row, "skill_projectile_prefab_id", skillProjectilePrefabId);
			map.SetCell(row, "skill_projectile_damage", skillProjectileDamage);
			map.SetCell(row, "skill_projectile_range", skillProjectileRange);
			map.SetCell(row, "skill_projectile_count", skillProjectileCount);
			map.SetCell(row, "skill_buff_duration", skillBuffDuration);
			map.SetCell(row, "skill_buff_stat_type", skillBuffStatType.ToString());
			map.SetCell(row, "skill_buff_percent", skillBuffPercent);
		}
	}
}
#endif
