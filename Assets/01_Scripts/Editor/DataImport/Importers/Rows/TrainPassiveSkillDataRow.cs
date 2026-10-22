#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class TrainPassiveSkillDataRow : IExcelRow
	{
		public string id;
		public string name;
		public string passiveType;
		public string param1;
		public string param2;
		public string param3;
		public string description;
		// 개조 해금 조건 원문("AttackDamage>=100;Kills>=300"). RemodelUnlockCondition.ParseList가 해석한다.
		public string unlockCondition;

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			name = map.GetString(row, "name");
			passiveType = map.GetString(row, "passive_type");
			param1 = map.GetString(row, "param1");
			param2 = map.GetString(row, "param2");
			param3 = map.GetString(row, "param3");
			description = map.GetString(row, "description");
			unlockCondition = map.GetString(row, "unlock_condition");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "name", name);
			map.SetCell(row, "passive_type", passiveType);
			map.SetCell(row, "param1", param1);
			map.SetCell(row, "param2", param2);
			map.SetCell(row, "param3", param3);
			map.SetCell(row, "description", description);
			map.SetCell(row, "unlock_condition", unlockCondition ?? "");
		}
	}
}
#endif
