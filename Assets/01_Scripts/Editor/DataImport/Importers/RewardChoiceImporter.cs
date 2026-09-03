#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	/// <summary>
	/// 만렙 보상 선택지(골드 / 엘리트 재화)를 ChoiceData.xlsx의 reward_choice_data 시트에서 임포트한다.
	/// reward_type 컬럼으로 구체 IChoiceOption(GoldRewardChoice / EliteCurrencyRewardChoice)을 분기 생성한다.
	/// </summary>
	public class RewardChoiceImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "ChoiceData.xlsx";
		public string SheetName => "reward_choice_data";
		public string ButtonLabel => "RewardChoice 데이터 가져오기";
		public string[] Headers => new[] { "id", "reward_type", "name_key", "description_key", "icon_id", "weight", "amount", "alive_heal_ratio", "revived_hp_ratio" };

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new RewardChoiceRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.id)) continue;

				var list = db.TriChoiceDB.RewardChoices;
				var existing = FindChoiceEntry(list, r.id);

				if (existing == null)
				{
					var choice = CreateChoice(r);
					if (choice == null) continue;

					var entry = new ChoiceEntry
					{
						Option = choice,
						Weight = r.weight
					};
					AddChoiceEntry(db, entry);
				}
				else
				{
					var choice = CreateChoice(r);
					if (choice == null) continue;

					existing.Option = choice;
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
			var field = typeof(TriChoiceDB).GetField("rewardChoices", BindingFlags.Instance | BindingFlags.NonPublic);
			var list = field?.GetValue(db.TriChoiceDB) as List<ChoiceEntry>;
			list?.Add(entry);
		}

		private static IChoiceOption CreateChoice(RewardChoiceRow r)
		{
			switch (r.rewardType?.Trim().ToLowerInvariant())
			{
				case "gold":
				{
					var choice = (GoldRewardChoice)FormatterServices.GetUninitializedObject(typeof(GoldRewardChoice));
					CopyBase(r, choice);
					SetPrivateField(typeof(GoldRewardChoice), choice, "goldAmount", r.amount);
					return choice;
				}
				case "elite_currency":
				{
					var choice = (EliteCurrencyRewardChoice)FormatterServices.GetUninitializedObject(typeof(EliteCurrencyRewardChoice));
					CopyBase(r, choice);
					SetPrivateField(typeof(EliteCurrencyRewardChoice), choice, "eliteCurrencyAmount", r.amount);
					return choice;
				}
				default:
					UnityEngine.Debug.LogWarning($"RewardChoiceImporter: unknown reward_type '{r.rewardType}' for id '{r.id}'");
					return null;
			}
		}

		// RewardChoiceBase의 공통 필드(id/nameKey/descriptionKey/iconId)를 채운다. icon(Sprite)은 런타임 lazy 로드라 심지 않는다.
		private static void CopyBase(RewardChoiceRow r, RewardChoiceBase target)
		{
			var t = typeof(RewardChoiceBase);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "nameKey", r.nameKey);
			SetPrivateField(t, target, "descriptionKey", r.descriptionKey);
			SetPrivateField(t, target, "iconId", r.iconId);
		}

		private void SetChoiceEntryWeight(ChoiceEntry entry, int weight)
		{
			var field = typeof(ChoiceEntry).GetField("Weight", BindingFlags.Instance | BindingFlags.Public);
			field?.SetValue(entry, weight);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif
