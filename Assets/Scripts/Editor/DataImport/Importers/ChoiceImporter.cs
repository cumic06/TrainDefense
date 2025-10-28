#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class ChoiceImporter : IExcelSheetImporter
	{
		public string SheetName => "choice_option";
		public string ButtonLabel => "Choice 데이터 가져오기";
		public string[] Headers => new[] { "id", "choice_type", "target_train_id" };
		public IExcelRow[] ExampleRows => new IExcelRow[] { new ChoiceRow { id = "ch_add_basic", choiceType = "AddTrain", targetTrainId = "train_basic" } };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new ChoiceRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.id)) continue;
				var list = db.choiceOptionList;
				var existing = list.Find(c => c.Id == r.id);
				if (existing == null)
				{
					var obj = (ChoiceOption)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ChoiceOption));
					Copy(r, obj);
					list.Add(obj);
				}
				else
				{
					Copy(r, existing);
				}
				imported++;
			}
			return imported;
		}

		private static void Copy(ChoiceRow r, ChoiceOption target)
		{
			var t = typeof(ChoiceOption);
			SetPrivateField(t, target, "id", r.id);
			// choiceType and references are runtime; we only set targetTrainId for now
			SetPrivateField(t, target, "targetTrainId", r.targetTrainId);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}

	}
}
#endif


