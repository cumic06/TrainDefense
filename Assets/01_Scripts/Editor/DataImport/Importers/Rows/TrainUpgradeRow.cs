#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TrainUpgradeRow : IExcelRow
	{
		public string id;
		public string name;
		public string description;
		public float maxHp;
		public string iconId;

		public virtual void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			name = map.GetString(row, "upgrade_name");
			description = map.GetString(row, "description");
			maxHp = map.GetFloat(row, "max_hp");
			iconId = map.GetString(row, "icon_id");
		}

		public virtual void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "upgrade_name", name);
			map.SetCell(row, "description", description);
			map.SetCell(row, "max_hp", maxHp);
			map.SetCell(row, "icon_id", iconId);
		}
	}
}
#endif
