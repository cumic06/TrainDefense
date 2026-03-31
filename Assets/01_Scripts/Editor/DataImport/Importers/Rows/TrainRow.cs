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

		public virtual void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			name = row.GetCell(1)?.ToString();
			description = row.GetCell(2)?.ToString();
			int.TryParse(row.GetCell(3)?.ToString(), out maxHp);
			bool.TryParse(row.GetCell(4)?.ToString(), out isMainTrain);
			prefabId = row.GetCell(5)?.ToString();
			iconId = row.GetCell(6)?.ToString();

			var skillTypeValue = row.GetCell(7)?.ToString();
			if (!System.Enum.TryParse(skillTypeValue, true, out skillType))
			{
				if (bool.TryParse(skillTypeValue, out bool hasSkill) && hasSkill)
				{
					skillType = TrainSkillType.Projectile;
				}
			}

			float.TryParse(row.GetCell(8)?.ToString(), out skillCooldown);
			skillIconId = row.GetCell(9)?.ToString();
			skillProjectilePrefabId = row.GetCell(10)?.ToString();
			int.TryParse(row.GetCell(11)?.ToString(), out skillProjectileDamage);
			float.TryParse(row.GetCell(12)?.ToString(), out skillProjectileRange);
			int.TryParse(row.GetCell(13)?.ToString(), out skillProjectileCount);
		}

		public virtual void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, name);
			Set(row, 2, description);
			Set(row, 3, maxHp);
			Set(row, 4, isMainTrain);
			Set(row, 5, prefabId);
			Set(row, 6, iconId);
			Set(row, 7, skillType.ToString());
			Set(row, 8, skillCooldown);
			Set(row, 9, skillIconId);
			Set(row, 10, skillProjectilePrefabId);
			Set(row, 11, skillProjectileDamage);
			Set(row, 12, skillProjectileRange);
			Set(row, 13, skillProjectileCount);
		}

	protected static void Set(IRow row, int idx, object value)
	{
		var cell = row.GetCell(idx) ?? row.CreateCell(idx);
		if (value is null) cell.SetCellValue(string.Empty);
		else if (value is int i) cell.SetCellValue(i);
		else if (value is bool b) cell.SetCellValue(b);
		else if (value is float f) cell.SetCellValue(f);
		else cell.SetCellValue(value.ToString());
	}
	}
}
#endif


