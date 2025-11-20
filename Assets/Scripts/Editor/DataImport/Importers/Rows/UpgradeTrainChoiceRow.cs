#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class UpgradeTrainChoiceRow : IExcelRow
	{
		public string id;
		public string targetTrainId;
		public string weightedUpgrades; // 업그레이드 ID (C열)
		public int weight; // 선택 항목 가중치 (D열)
		public float upgradeWeight; // 업그레이드 가중치 (E열)

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			targetTrainId = row.GetCell(1)?.ToString();
			weightedUpgrades = row.GetCell(2)?.ToString();
			int.TryParse(row.GetCell(3)?.ToString(), out weight);
			float.TryParse(row.GetCell(4)?.ToString(), out upgradeWeight);
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, targetTrainId);
			Set(row, 2, weightedUpgrades);
			Set(row, 3, weight);
			Set(row, 4, upgradeWeight);
		}

		private static void Set(IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value is null) cell.SetCellValue(string.Empty);
			else if (value is int i) cell.SetCellValue(i);
			else cell.SetCellValue(value.ToString());
		}
	}
}
#endif

