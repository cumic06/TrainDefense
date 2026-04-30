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
		public string activeSkillDataId;

		public virtual void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			name = map.GetString(row, "train_name");
			description = map.GetString(row, "description");
			maxHp = map.GetInt(row, "max_hp");
			isMainTrain = map.GetBool(row, "is_main_train");
			prefabId = map.GetString(row, "prefab_id");
			iconId = map.GetString(row, "icon_id");
			activeSkillDataId = map.GetString(row, "train_skill_data_id")?.Trim();
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
			map.SetCell(row, "train_skill_data_id", activeSkillDataId ?? "");
		}

		protected static string[] ParseIds(string raw)
		{
			if (string.IsNullOrEmpty(raw)) return System.Array.Empty<string>();
			var parts = raw.Split(';');
			var list = new System.Collections.Generic.List<string>();
			foreach (var p in parts)
			{
				var t = p.Trim();
				if (!string.IsNullOrEmpty(t)) list.Add(t);
			}
			return list.ToArray();
		}
	}
}
#endif
