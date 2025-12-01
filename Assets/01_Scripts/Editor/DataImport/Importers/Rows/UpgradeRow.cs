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
		public float upgradeValue;	// NonTrainUpgrade: 그대로 사용, TrainUpgrade: stat 값으로도 사용
		public int maxUpgradeCount;
		public string iconId;

		// 새 필드들 (UpgradeData의 확장과 매핑)
		// upgradeType: "TrainUpgrade" / "NonTrainUpgrade" 또는 0 / 1 등 문자열로 표기
		public string upgradeType;
		// statType: StatType 이름 (예: "AttackDamage", "MaxHp" ...)
		public string statType;

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			name = row.GetCell(1)?.ToString();
			description = row.GetCell(2)?.ToString();
			int.TryParse(row.GetCell(3)?.ToString(), out needMoney);
			float.TryParse(row.GetCell(4)?.ToString(), out upgradeValue);
			int.TryParse(row.GetCell(5)?.ToString(), out maxUpgradeCount);
			iconId = row.GetCell(6)?.ToString();

			// 선택 컬럼: 시트에 없으면 무시
			if (row.LastCellNum > 7)
			{
				upgradeType = row.GetCell(7)?.ToString();
			}

			if (row.LastCellNum > 8)
			{
				statType = row.GetCell(8)?.ToString();
			}
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, name);
			Set(row, 2, description);
			Set(row, 3, needMoney);
			Set(row, 4, upgradeValue);
			Set(row, 5, maxUpgradeCount);
			Set(row, 6, iconId);
			Set(row, 7, upgradeType);
			Set(row, 8, statType);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty);
			else if (value is int i) cell.SetCellValue(i);
			else if (value is float f) cell.SetCellValue(f);
			else cell.SetCellValue(value.ToString());
		}
	}
}
#endif