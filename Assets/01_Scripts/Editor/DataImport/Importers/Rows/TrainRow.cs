#if UNITY_EDITOR
using NPOI.SS.UserModel;

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
		public string trainSkillDataId;

		public virtual void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			name = map.GetString(row, "train_name");
			description = map.GetString(row, "description");
			maxHp = map.GetInt(row, "max_hp");
			isMainTrain = map.GetBool(row, "is_main_train");
			prefabId = map.GetString(row, "prefab_id");
			iconId = map.GetString(row, "icon_id");
			trainSkillDataId = map.GetString(row, "train_skill_data_id");
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
			map.SetCell(row, "train_skill_data_id", trainSkillDataId);
		}
	}
}
#endif
