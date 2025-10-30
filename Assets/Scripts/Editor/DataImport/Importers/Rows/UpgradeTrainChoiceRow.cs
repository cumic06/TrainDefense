#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class UpgradeTrainChoiceRow : IExcelRow
	{
		public string id;
		public string targetTrainId;
		public string weightedUpgrades; // 형식: "upgradeId1:weight1;upgradeId2:weight2"
		public int weight;

		public void FromExcelRow(IRow row)
		{
			id = row.GetCell(0)?.ToString();
			targetTrainId = row.GetCell(1)?.ToString();
			weightedUpgrades = row.GetCell(2)?.ToString();
			int.TryParse(row.GetCell(3)?.ToString(), out weight);
		}

		public void ToExcelRow(IRow row)
		{
			Set(row, 0, id);
			Set(row, 1, targetTrainId);
			Set(row, 2, weightedUpgrades);
			Set(row, 3, weight);
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

