#if UNITY_EDITOR
using NPOI.SS.UserModel;
using TrainDefense.Game.Datas;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TrainRow : IExcelRow
	{
		public string id;
		public string name;
		public string description;
		public int maxHp;
		public bool isMainTrain;
		public string prefabId;
		public string iconId;
		public TrainSkillType skillType;
		public float skillCooldown;
		public string skillIconId;
		public string skillProjectilePrefabId;
		public int skillProjectileDamage;
		public float skillProjectileRange;
		public int skillProjectileCount;

		public virtual void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			name = map.GetString(row, "train_name");
			description = map.GetString(row, "description");
			maxHp = map.GetInt(row, "max_hp");
			isMainTrain = map.GetBool(row, "is_main_train");
			prefabId = map.GetString(row, "prefab_id");
			iconId = map.GetString(row, "icon_id");

			var skillTypeValue = map.GetString(row, "has_skill");
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
		}

		public virtual void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "train_name", name);
			map.SetCell(row, "description", description);
			map.SetCell(row, "max_hp", maxHp);
			map.SetCell(row, "is_main_train", isMainTrain);
			map.SetCell(row, "prefab_id", prefabId);
			map.SetCell(row, "icon_id", iconId);
			map.SetCell(row, "has_skill", skillType.ToString());
			map.SetCell(row, "skill_cooldown", skillCooldown);
			map.SetCell(row, "skill_icon_id", skillIconId);
			map.SetCell(row, "skill_projectile_prefab_id", skillProjectilePrefabId);
			map.SetCell(row, "skill_projectile_damage", skillProjectileDamage);
			map.SetCell(row, "skill_projectile_range", skillProjectileRange);
			map.SetCell(row, "skill_projectile_count", skillProjectileCount);
		}
	}
}
#endif
