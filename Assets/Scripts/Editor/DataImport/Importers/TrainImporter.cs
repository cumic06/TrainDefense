#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Editor.DataImport.Importers.Reflectors;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class TrainImporter : IExcelSheetImporter
	{
		public string SheetName => "train_data";
		public string ButtonLabel => "Train 데이터 가져오기";
		public string[] Headers => new[] { "id", "train_name", "description", "is_main_train" };
		public IExcelRow[] ExampleRows => new IExcelRow[] { new TrainRow { id = "train_basic", trainName = "Basic", description = "desc", isMainTrain = false } };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			var refl = new TrainDataReflector();
			foreach (var row in rows)
			{
				var tr = new TrainRow();
				tr.FromExcelRow(row);
				if (string.IsNullOrEmpty(tr.id)) continue;
				var existing = db.trainDataList.Find(t => t.Id == tr.id);
				if (existing == null)
				{
					var newData = refl.Create(tr);
					db.trainDataList.Add(newData);
				}
				else
				{
					refl.Copy(tr, existing);
				}
				imported++;
			}
			return imported;
		}

	}
}
#endif


