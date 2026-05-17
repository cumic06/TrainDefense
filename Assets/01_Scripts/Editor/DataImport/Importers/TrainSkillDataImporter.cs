#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Editor.DataImport.Importers.Reflectors;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class TrainSkillDataImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "TrainSkillData.xlsx";
		public string SheetName => "active_skill_data";
		public string ButtonLabel => "액티브 스킬 데이터 가져오기";
		public string[] Headers => new[]
		{
			"id",
			"name",
			"description",
			"skill_type",
			"skill_cooldown",
			"skill_icon_id",
			"skill_projectile_prefab_id",
			"skill_projectile_damage",
			"skill_projectile_range",
			"skill_projectile_count",
			"skill_buff_duration",
			"skill_buffs",
		};

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			var refl = new TrainSkillDataReflector();
			foreach (var row in rows)
			{
				var r = new TrainSkillDataRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.id)) continue;
				var existing = db.TrainSkillDataDB.trainActiveSkillDataList.Find(s => s != null && s.Id == r.id);
				if (existing == null)
				{
					var obj = refl.Create(r);
					db.TrainSkillDataDB.trainActiveSkillDataList.Add(obj);
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
