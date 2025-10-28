#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Editor.DataImport.Importers.Reflectors;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class RangeTrainImporter : IExcelSheetImporter
	{
		public string SheetName => "range_train_data";
		public string ButtonLabel => "RangeTrain 데이터 가져오기";
		public string[] Headers => new[] { "id", "train_name", "description", "is_main_train" };
		public IExcelRow[] ExampleRows => new IExcelRow[] { new TrainRow { id = "range_basic", trainName = "Range", description = "desc", isMainTrain = false } };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			var refl = new TrainDefense.Editor.DataImport.Importers.Reflectors.RangeTrainDataReflector();
			foreach (var row in rows)
			{
				var r = new TrainRow();
				r.FromExcelRow(row);
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


