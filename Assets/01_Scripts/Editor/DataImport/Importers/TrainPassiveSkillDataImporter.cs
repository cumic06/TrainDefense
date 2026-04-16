#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Editor.DataImport.Importers.Reflectors;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class TrainPassiveSkillDataImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "TrainSkillData.xlsx";
		public string SheetName => "passive_skill_data";
		public string ButtonLabel => "패시브 스킬 데이터 가져오기";
		public string[] Headers => new[]
		{
			"id",
			"name",
			"passive_type",
			"param1",
			"param2",
			"param3",
			"description",
		};

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			var refl = new TrainPassiveSkillDataReflector();
			foreach (var row in rows)
			{
				var r = new TrainPassiveSkillDataRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.id)) continue;
				var existing = db.trainPassiveSkillDataList.Find(s => s != null && s.Id == r.id);
				if (existing == null)
				{
					var obj = refl.Create(r);
					db.trainPassiveSkillDataList.Add(obj);
				}
				else
				{
					refl.Copy(r, existing);
				}
				imported++;
			}
			return imported;
		}
	}
}
#endif
