#if UNITY_EDITOR
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class StageImporter : IExcelSheetImporter
	{
		public string SheetName => "stage_data";
		public string ButtonLabel => "Stage 데이터 가져오기";
		public string[] Headers => new[] { "id", "stage_inspection_time", "stage_end_time" };
		public IExcelRow[] ExampleRows => new IExcelRow[] { new StageRow { id = "stage_1", stageInspectionTime = "10,20,30", stageEndTime = 180 } };

		public int Import(TrainDefense.Game.Datas.DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new StageRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.id)) continue;
				var existing = db.stageDataList.Find(s => s.Id == r.id);
				if (existing == null)
				{
					var obj = (StageData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(StageData));
					Copy(r, obj);
					db.stageDataList.Add(obj);
				}
				else
				{
					Copy(r, existing);
				}
				imported++;
			}
			return imported;
		}

		private static void Copy(StageRow r, StageData target)
		{
			var t = typeof(StageData);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "stageEndTime", r.stageEndTime);
			SetPrivateField(t, target, "stageInspectionTime", ParseFloatArray(r.stageInspectionTime));
		}

		private static float[] ParseFloatArray(string csv)
		{
			if (string.IsNullOrEmpty(csv)) return System.Array.Empty<float>();
			var parts = csv.Split(',');
			var arr = new float[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				float.TryParse(parts[i], out arr[i]);
			}
			return arr;
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}

	}
}
#endif


