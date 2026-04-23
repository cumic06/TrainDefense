#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Editor.DataImport.Importers.Reflectors;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class RangeTrainImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "TrainData.xlsx";
		public string SheetName => "range_train_data";
		public string ButtonLabel => "RangeTrain 데이터 가져오기";
		public string[] Headers => new[] { "id", "train_name", "description", "max_hp", "is_main_train", "prefab_id", "icon_id", "train_skill_data_id", "attack_range", "attack_area", "attack_damage", "attack_count", "attack_interval", "range_projectile_prefab_id", "critical_chance", "critical_damage", "passive_skill_data_id" };

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			var refl = new RangeTrainDataReflector();
			foreach (var row in rows)
			{
				var r = new RangeTrainRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.id)) continue;
				var existing = db.rangeTrainDataList.Find(t => t.Id == r.id);
				if (existing == null)
				{
					var obj = refl.Create(r);
					db.rangeTrainDataList.Add(obj);
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