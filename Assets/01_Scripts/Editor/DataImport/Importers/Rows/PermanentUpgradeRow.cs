#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class PermanentUpgradeRow : IExcelRow
	{
		public string id;
		public string name;
		public string description;
		public string iconId;
		public int needMoney;
		public int maxUpgradeCount;
		public float growthRate;
		public string category;
		public string passiveType;
		public float passiveValuePerLevel;
		public string statType;
		public float statValue;

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			name = map.GetString(row, "name");
			description = map.GetString(row, "description");
			iconId = map.GetString(row, "icon_id");
			needMoney = map.GetInt(row, "need_money");
			maxUpgradeCount = map.GetInt(row, "max_upgrade_count");

			if (map.HasColumn("growth_rate"))
				growthRate = map.GetFloat(row, "growth_rate");

			category = map.GetString(row, "category");

			if (map.HasColumn("passive_type"))
				passiveType = map.GetString(row, "passive_type");

			if (map.HasColumn("passive_value_per_level"))
				passiveValuePerLevel = map.GetFloat(row, "passive_value_per_level");

			if (map.HasColumn("stat_type"))
				statType = map.GetString(row, "stat_type");

			if (map.HasColumn("stat_value"))
				statValue = map.GetFloat(row, "stat_value");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "name", name);
			map.SetCell(row, "description", description);
			map.SetCell(row, "icon_id", iconId);
			map.SetCell(row, "need_money", needMoney);
			map.SetCell(row, "max_upgrade_count", maxUpgradeCount);
			map.SetCell(row, "growth_rate", growthRate);
			map.SetCell(row, "category", category);
			map.SetCell(row, "passive_type", passiveType);
			map.SetCell(row, "passive_value_per_level", passiveValuePerLevel);
			map.SetCell(row, "stat_type", statType);
			map.SetCell(row, "stat_value", statValue);
		}
	}
}
#endif
