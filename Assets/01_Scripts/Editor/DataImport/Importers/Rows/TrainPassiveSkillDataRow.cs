#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TrainPassiveSkillDataRow : IExcelRow
	{
		public string id;
		public string passiveType;
		public string param1;
		public string param2;
		public string param3;

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			passiveType = map.GetString(row, "passive_type");
			param1 = map.GetString(row, "param1");
			param2 = map.GetString(row, "param2");
			param3 = map.GetString(row, "param3");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "passive_type", passiveType);
			map.SetCell(row, "param1", param1);
			map.SetCell(row, "param2", param2);
			map.SetCell(row, "param3", param3);
		}
	}
}
#endif
