#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class AddTrainChoiceImporter : IExcelSheetImporter
	{
		public string SheetName => "add_train_choice_data";
		public string ButtonLabel => "AddTrainChoice 데이터 가져오기";
		public string[] Headers => new[] { "id", "train_data_id", "weight", "tier", "replace_train_id" };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new AddTrainChoiceRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.id)) continue;

				var list = db.TriChoiceDB.AddTrainChoices;
				var existing = FindChoiceEntry(list, r.id);

				if (existing == null)
				{
					var choice = CreateAddTrainChoice(r);
					var entry = new ChoiceEntry
					{
						Option = choice,
						Weight = r.weight,
						Tier = r.tier
					};
					AddChoiceEntry(db, entry);
				}
				else
				{
					UpdateAddTrainChoice(existing.Option as AddTrainChoice, r);
					SetChoiceEntryWeight(existing, r.weight);
					SetChoiceEntryTier(existing, r.tier);
				}
				imported++;
			}
			return imported;
		}

		private ChoiceEntry FindChoiceEntry(System.Collections.Generic.IReadOnlyList<ChoiceEntry> list, string id)
		{
			foreach (var entry in list)
			{
				if (entry.Option != null && entry.Option.Id == id)
				{
					return entry;
				}
			}
			return null;
		}

		private void AddChoiceEntry(DB db, ChoiceEntry entry)
		{
			var field = typeof(TriChoiceDB).GetField("addTrainChoices", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			var list = field?.GetValue(db.TriChoiceDB) as System.Collections.Generic.List<ChoiceEntry>;
			list?.Add(entry);
		}

		private AddTrainChoice CreateAddTrainChoice(AddTrainChoiceRow r)
		{
			var obj = (AddTrainChoice)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(AddTrainChoice));
			Copy(r, obj);
			return obj;
		}

		private void UpdateAddTrainChoice(AddTrainChoice target, AddTrainChoiceRow r)
		{
			if (target == null) return;
			Copy(r, target);
		}

		private void SetChoiceEntryWeight(ChoiceEntry entry, int weight)
		{
			var field = typeof(ChoiceEntry).GetField("Weight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			field?.SetValue(entry, weight);
		}

		private void SetChoiceEntryTier(ChoiceEntry entry, int tier)
		{
			var field = typeof(ChoiceEntry).GetField("Tier", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			field?.SetValue(entry, tier);
		}

		private static void Copy(AddTrainChoiceRow r, AddTrainChoice target)
		{
			var t = typeof(AddTrainChoice);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "trainDataId", r.trainDataId);
			SetPrivateField(t, target, "replaceTrainId", r.replaceTrainId);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif
