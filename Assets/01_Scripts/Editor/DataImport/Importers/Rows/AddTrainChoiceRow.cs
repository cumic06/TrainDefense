#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class AddTrainChoiceRow : IExcelRow
	{
		public string id;
		public string trainDataId;
		public int weight;
		public int tier;
		public string replaceTrainId;

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			trainDataId = map.GetString(row, "train_data_id");
			weight = map.GetInt(row, "weight");
			tier = map.GetInt(row, "tier");
			replaceTrainId = map.GetString(row, "replace_train_id");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "train_data_id", trainDataId);
			map.SetCell(row, "weight", weight);
			map.SetCell(row, "tier", tier);
			map.SetCell(row, "replace_train_id", replaceTrainId);
		}
	}
}
#endif
