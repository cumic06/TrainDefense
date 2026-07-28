#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	/// <summary>
	/// 상점 스탯 강화 등급을 StatUpgradeData.xlsx의 stat_upgrade_tier_data 시트에서 임포트한다.
	/// 고등급 = 낮은 weight(희귀) + 낮은 1원당 가격 — 뜨면 이득인 희귀 상품이다.
	/// </summary>
	public class StatUpgradeTierImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "StatUpgradeData.xlsx";
		public string SheetName => "stat_upgrade_tier_data";
		public string ButtonLabel => "StatUpgradeTier 데이터 가져오기";
		public string[] Headers => new[] { "id", "grade", "value_multiplier", "cost_multiplier", "first_shop_visit", "last_shop_visit", "weight" };

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;

			foreach (var row in rows)
			{
				var r = new StatUpgradeTierRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.id)) continue;

				var existing = db.statUpgradeTierDataList.Find(tier => tier != null && tier.Id == r.id);

				if (existing == null)
				{
					existing = (StatUpgradeTierData)FormatterServices.GetUninitializedObject(typeof(StatUpgradeTierData));
					db.statUpgradeTierDataList.Add(existing);
				}

				StatUpgradeImportUtil.SetPrivateField(existing, "id", r.id);
				StatUpgradeImportUtil.SetPrivateField(existing, "grade", r.grade);
				StatUpgradeImportUtil.SetPrivateField(existing, "valueMultiplier", r.valueMultiplier);
				StatUpgradeImportUtil.SetPrivateField(existing, "costMultiplier", r.costMultiplier);
				StatUpgradeImportUtil.SetPrivateField(existing, "firstShopVisit", r.firstShopVisit);
				StatUpgradeImportUtil.SetPrivateField(existing, "lastShopVisit", r.lastShopVisit);
				// GetUninitializedObject가 생성자 초기화(1)를 우회하므로 항상 주입한다. 빈 셀(0)은 1로 보정.
				StatUpgradeImportUtil.SetPrivateField(existing, "weight", r.weight > 0 ? r.weight : 1);
				imported++;
			}

			return imported;
		}
	}

	/// <summary>
	/// 스탯별 최소 등급을 StatUpgradeData.xlsx의 stat_upgrade_stat_data 시트에서 임포트한다.
	/// 강력한 정수 스탯(대상 수·공격 횟수)을 고등급으로 밀어 비싸게 만드는 장치다.
	/// </summary>
	public class StatUpgradeStatImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "StatUpgradeData.xlsx";
		public string SheetName => "stat_upgrade_stat_data";
		public string ButtonLabel => "StatUpgradeStat 데이터 가져오기";
		public string[] Headers => new[] { "stat_type", "min_grade", "cost_multiplier" };

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;

			foreach (var row in rows)
			{
				string name = map.GetString(row, "stat_type");
				if (string.IsNullOrEmpty(name)) continue;

				if (!StatUpgradeImportUtil.TryParseStatType(name, out var statType))
				{
					UnityEngine.Debug.LogWarning($"StatUpgradeStatImporter: 알 수 없는 stat_type '{name}' — 건너뜀");
					continue;
				}

				var existing = db.statUpgradeStatDataList.Find(s => s != null && s.StatType == statType);

				if (existing == null)
				{
					existing = (StatUpgradeStatData)FormatterServices.GetUninitializedObject(typeof(StatUpgradeStatData));
					db.statUpgradeStatDataList.Add(existing);
				}

				StatUpgradeImportUtil.SetPrivateField(existing, "id", $"stat_upgrade_stat_{name}");
				StatUpgradeImportUtil.SetPrivateField(existing, "statType", statType);
				StatUpgradeImportUtil.SetPrivateField(existing, "minGrade", map.GetInt(row, "min_grade"));

				// GetUninitializedObject가 생성자 초기화(1f)를 우회하므로 항상 주입한다. 빈 셀(0)은 프리미엄 없음 = 1.
				float costMultiplier = map.GetFloat(row, "cost_multiplier");
				StatUpgradeImportUtil.SetPrivateField(existing, "costMultiplier", costMultiplier > 0f ? costMultiplier : 1f);
				imported++;
			}

			return imported;
		}
	}

	/// <summary>
	/// 포탑별 "강화 가능한 스탯과 증가율"을 StatUpgradeData.xlsx의 train_stat_upgrade_data 시트에서 임포트한다.
	/// 시트는 포탑 1행 × 스탯 컬럼의 가로 배치이고, 빈칸인 스탯은 그 포탑의 강화 카드로 뜨지 않는다.
	/// 엘리트(31xxx·41xxx)는 행을 두지 않고 DatabaseManager가 base 포탑 규칙으로 폴백한다.
	/// </summary>
	public class TrainStatUpgradeRuleImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "StatUpgradeData.xlsx";
		public string SheetName => "train_stat_upgrade_data";
		public string ButtonLabel => "TrainStatUpgrade 데이터 가져오기";

		public string[] Headers =>
			new[] { "train_data_id" }
				.Concat(TrainStatUpgradeRuleRow.StatColumns.Select(c => c.column))
				.ToArray();

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;

			foreach (var row in rows)
			{
				var r = new TrainStatUpgradeRuleRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.trainDataId)) continue;

				foreach (var pair in r.increaseRates)
				{
					// id는 시트에 두지 않고 포탑 + 스탯으로 만든다(사람이 채울 필요 없음).
					string id = StatUpgradeImportUtil.BuildRuleId(r.trainDataId, pair.Key);
					var existing = db.trainStatUpgradeRuleDataList.Find(rule => rule != null && rule.Id == id);

					if (existing == null)
					{
						existing = (TrainStatUpgradeRuleData)FormatterServices.GetUninitializedObject(typeof(TrainStatUpgradeRuleData));
						db.trainStatUpgradeRuleDataList.Add(existing);
					}

					StatUpgradeImportUtil.SetPrivateField(existing, "id", id);
					StatUpgradeImportUtil.SetPrivateField(existing, "trainDataId", r.trainDataId);
					StatUpgradeImportUtil.SetPrivateField(existing, "statType", pair.Key);
					StatUpgradeImportUtil.SetPrivateField(existing, "increaseRate", pair.Value);
					imported++;
				}
			}

			return imported;
		}
	}

	internal static class StatUpgradeImportUtil
	{
		public static bool TryParseStatType(string name, out StatType statType)
		{
			string key = (name ?? string.Empty).Trim().ToLowerInvariant();

			foreach (var (column, type) in TrainStatUpgradeRuleRow.StatColumns)
			{
				if (column == key)
				{
					statType = type;
					return true;
				}
			}

			statType = default;
			return false;
		}

		public static string BuildRuleId(string trainDataId, StatType statType)
		{
			string column = TrainStatUpgradeRuleRow.StatColumns.First(c => c.statType == statType).column;

			return $"stat_upgrade_{trainDataId}_{column}";
		}

		public static void SetPrivateField(object instance, string field, object value)
		{
			var fi = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif
