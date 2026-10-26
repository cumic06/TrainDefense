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
		// 선택지 풀(LevelUp/ShopItem). 비어 있으면 LevelUp.
		public string pool;
		// 스탯 여러 개를 담는 아이템용 — "AttackDamage:15;AttackInterval:5". 비어 있으면 stat_type + upgrade_value 한 개.
		public string stats;
		// 상점 아이템 등급(1~5, 등급표 행). 비어 있으면 0.
		public int grade;

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

			if (map.HasColumn("pool"))
				pool = map.GetString(row, "pool");

			if (map.HasColumn("stats"))
				stats = map.GetString(row, "stats");

			if (map.HasColumn("grade"))
				grade = map.GetInt(row, "grade");
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
			map.SetCell(row, "pool", pool);
			map.SetCell(row, "stats", stats);
			map.SetCell(row, "grade", grade);
		}
	}
}
#endif
