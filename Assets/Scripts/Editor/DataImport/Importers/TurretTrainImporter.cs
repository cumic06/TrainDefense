#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Editor.DataImport.Importers.Reflectors;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class TurretTrainImporter : IExcelSheetImporter
	{
		public string SheetName => "turret_train_data";
		public string ButtonLabel => "TurretTrain 데이터 가져오기";
		public string[] Headers => new[] { "id", "train_name", "description", "is_main_train" };
		public IExcelRow[] ExampleRows => new IExcelRow[] { new TrainRow { id = "turret_basic", trainName = "Turret", description = "desc", isMainTrain = false } };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			var refl = new TurretTrainDataReflector();
			foreach (var row in rows)
			{
				var r = new TrainRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.id)) continue;
				var existing = db.turretTrainDataList.Find(t => t.Id == r.id);
				if (existing == null)
				{
					var obj = refl.Create(r);
					db.turretTrainDataList.Add(obj);
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


