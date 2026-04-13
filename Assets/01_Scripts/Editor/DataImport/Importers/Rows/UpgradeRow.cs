#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class UpgradeRow : IExcelRow
	{
		public string id;
		public string name;
		public string description;
		public int needMoney;
		public float upgradeValue;
		public int maxUpgradeCount;
		public string iconId;
		public string upgradeType;
		public string statType;
		public float growthRate;

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			name = map.GetString(row, "upgrade_name");
			description = map.GetString(row, "description");
			needMoney = map.GetInt(row, "need_money");
			upgradeValue = map.GetFloat(row, "upgrade_value");
			maxUpgradeCount = map.GetInt(row, "max_upgrade_count");
			iconId = map.GetString(row, "icon_id");

			if (map.HasColumn("upgrade_type"))
				upgradeType = map.GetString(row, "upgrade_type");

			if (map.HasColumn("stat_type"))
				statType = map.GetString(row, "stat_type");

			if (map.HasColumn("growth_rate"))
				growthRate = map.GetFloat(row, "growth_rate");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "upgrade_name", name);
			map.SetCell(row, "description", description);
			map.SetCell(row, "need_money", needMoney);
			map.SetCell(row, "upgrade_value", upgradeValue);
			map.SetCell(row, "max_upgrade_count", maxUpgradeCount);
			map.SetCell(row, "icon_id", iconId);
			map.SetCell(row, "upgrade_type", upgradeType);
			map.SetCell(row, "stat_type", statType);
			map.SetCell(row, "growth_rate", growthRate);
		}
	}
}
#endif
