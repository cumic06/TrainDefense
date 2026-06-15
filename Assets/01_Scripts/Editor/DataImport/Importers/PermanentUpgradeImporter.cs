#if UNITY_EDITOR
using System;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class PermanentUpgradeImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "PermanentUpgradeData.xlsx";
		public string SheetName => "permanent_upgrade_data";
		public string ButtonLabel => "영구 업그레이드 데이터 가져오기";
		// Excel 헤더 안내 (H: category[TurretStat/Passive], I: passive_type, K: stat_type)
		public string[] Headers => new[]
		{
			"id", "name", "description", "icon_id", "need_money",
			"max_upgrade_count", "growth_rate", "category",
			"passive_type", "passive_value_per_level", "stat_type", "stat_value"
		};

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new PermanentUpgradeRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.id) && string.IsNullOrEmpty(r.name)) continue;

				var list = db.permanentUpgradeDataList;
				PermanentUpgradeData existing = null;
				if (!string.IsNullOrEmpty(r.id))
				{
					existing = list.Find(u => u.Id == r.id);
				}

				if (existing == null)
				{
					var obj = (PermanentUpgradeData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(PermanentUpgradeData));
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

		private static void Copy(PermanentUpgradeRow r, PermanentUpgradeData target)
		{
			var t = typeof(PermanentUpgradeData);

			// 기본 필드 매핑
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "name", r.name);
			SetPrivateField(t, target, "description", r.description);
			SetPrivateField(t, target, "iconId", r.iconId);
			SetPrivateField(t, target, "needMoney", r.needMoney);
			SetPrivateField(t, target, "maxUpgradeCount", r.maxUpgradeCount);

			// growthRate: 시트 값이 유효하지 않으면 기본값 1.065f 사용
			float growthRate = r.growthRate > 0f ? r.growthRate : 1.065f;
			SetPrivateField(t, target, "growthRate", growthRate);

			// 카테고리 결정
			var category = ParseCategory(r.category);
			SetPrivateField(t, target, "category", category);

			if (category == PermanentUpgradeCategory.TurretStat)
			{
				// TurretStat: stat_type + stat_value로 SimpleStat 하나 구성
				if (!string.IsNullOrEmpty(r.statType) && Enum.TryParse<StatType>(r.statType, ignoreCase: true, out var statType))
				{
					var stats = new SimpleStat[]
					{
						new SimpleStat
						{
							Type = statType,
							Value = r.statValue
						}
					};
					SetPrivateField(t, target, "stats", stats);
				}
				else
				{
					SetPrivateField(t, target, "stats", Array.Empty<SimpleStat>());
				}

				// Passive 필드는 기본값으로
				SetPrivateField(t, target, "passiveType", default(PermanentUpgradeType));
				SetPrivateField(t, target, "passiveValuePerLevel", 0f);
			}
			else
			{
				// Passive: passive_type + passive_value_per_level
				SetPrivateField(t, target, "stats", Array.Empty<SimpleStat>());
				SetPrivateField(t, target, "passiveType", ParsePassiveType(r.passiveType));
				SetPrivateField(t, target, "passiveValuePerLevel", r.passiveValuePerLevel);
			}
		}

		private static PermanentUpgradeCategory ParseCategory(string raw)
		{
			if (!string.IsNullOrEmpty(raw))
			{
				if (Enum.TryParse<PermanentUpgradeCategory>(raw, ignoreCase: true, out var parsed))
				{
					return parsed;
				}
				if (int.TryParse(raw, out var intValue) && Enum.IsDefined(typeof(PermanentUpgradeCategory), intValue))
				{
					return (PermanentUpgradeCategory)intValue;
				}
			}
			// fallback: 기본은 Passive
			return PermanentUpgradeCategory.Passive;
		}

		private static PermanentUpgradeType ParsePassiveType(string raw)
		{
			if (!string.IsNullOrEmpty(raw))
			{
				if (Enum.TryParse<PermanentUpgradeType>(raw, ignoreCase: true, out var parsed))
				{
					return parsed;
				}
				if (int.TryParse(raw, out var intValue) && Enum.IsDefined(typeof(PermanentUpgradeType), intValue))
				{
					return (PermanentUpgradeType)intValue;
				}
			}
			return default;
		}

		private static void SetPrivateField(Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif
