#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class UpgradeTrainChoiceImporter : IExcelSheetImporter
	{
		public string SheetName => "upgrade_train_choice_data";
		public string ButtonLabel => "UpgradeTrainChoice 데이터 가져오기";
		public string[] Headers => new[] { "id", "target_train_id", "weighted_upgrades", "weight" };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new UpgradeTrainChoiceRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.id)) continue;

				var list = db.TriChoiceDB.UpgradeTrainChoices;
				var existing = FindChoiceEntry(list, r.id);
				
				if (existing == null)
				{
					var choice = CreateUpgradeTrainChoice(r);
					var entry = new ChoiceEntry
					{
						Option = choice,
						Weight = r.weight
					};
					AddChoiceEntry(db, entry);
				}
				else
				{
					UpdateUpgradeTrainChoice(existing.Option as UpgradeTrainChoice, r);
					SetChoiceEntryWeight(existing, r.weight);
				}
				imported++;
			}
			return imported;
		}

		private ChoiceEntry FindChoiceEntry(IReadOnlyList<ChoiceEntry> list, string id)
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
			var field = typeof(TriChoiceDB).GetField("upgradeTrainChoices", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			var list = field?.GetValue(db.TriChoiceDB) as List<ChoiceEntry>;
			list?.Add(entry);
		}

		private UpgradeTrainChoice CreateUpgradeTrainChoice(UpgradeTrainChoiceRow r)
		{
			var obj = (UpgradeTrainChoice)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UpgradeTrainChoice));
			Copy(r, obj);
			return obj;
		}

		private void UpdateUpgradeTrainChoice(UpgradeTrainChoice target, UpgradeTrainChoiceRow r)
		{
			if (target == null) return;
			Copy(r, target);
		}

		private void SetChoiceEntryWeight(ChoiceEntry entry, int weight)
		{
			var field = typeof(ChoiceEntry).GetField("Weight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			field?.SetValue(entry, weight);
		}

		private static void Copy(UpgradeTrainChoiceRow r, UpgradeTrainChoice target)
		{
			var t = typeof(UpgradeTrainChoice);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "targetTrainId", r.targetTrainId);
			SetPrivateField(t, target, "weightedUpgrades", ParseWeightedUpgrades(r.weightedUpgrades));
		}

		private static WeightedUpgradeData[] ParseWeightedUpgrades(string weightedUpgradesStr)
		{
			if (string.IsNullOrEmpty(weightedUpgradesStr)) return System.Array.Empty<WeightedUpgradeData>();
			
			var parts = weightedUpgradesStr.Split(';');
			var list = new List<WeightedUpgradeData>();
			
			foreach (var part in parts)
			{
				var trimmed = part.Trim();
				if (string.IsNullOrEmpty(trimmed)) continue;
				
				var colonIndex = trimmed.IndexOf(':');
				if (colonIndex < 0) continue;
				
				var upgradeId = trimmed.Substring(0, colonIndex).Trim();
				var weightStr = trimmed.Substring(colonIndex + 1).Trim();
				
				if (string.IsNullOrEmpty(upgradeId)) continue;
				
				float.TryParse(weightStr, out float weight);
				
				var weightedUpgrade = (WeightedUpgradeData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(WeightedUpgradeData));
				var t = typeof(WeightedUpgradeData);
				SetPrivateField(t, weightedUpgrade, "upgradeDataId", upgradeId);
				SetPrivateField(t, weightedUpgrade, "upgradeDataWeight", weight);
				
				list.Add(weightedUpgrade);
			}
			
			return list.ToArray();
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif

