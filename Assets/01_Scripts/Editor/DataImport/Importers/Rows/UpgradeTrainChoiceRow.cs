#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class UpgradeTrainChoiceRow : IExcelRow
	{
		public string id;
		public string targetTrainId;
		public string weightedUpgrades;
		public int weight;
		public float upgradeWeight;

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			targetTrainId = map.GetString(row, "target_train_id");
			weightedUpgrades = map.GetString(row, "weighted_upgrades");
			weight = map.GetInt(row, "weight");
			upgradeWeight = map.GetFloat(row, "upgrade_weight");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "target_train_id", targetTrainId);
			map.SetCell(row, "weighted_upgrades", weightedUpgrades);
			map.SetCell(row, "weight", weight);
			map.SetCell(row, "upgrade_weight", upgradeWeight);
		}
	}
}
#endif
