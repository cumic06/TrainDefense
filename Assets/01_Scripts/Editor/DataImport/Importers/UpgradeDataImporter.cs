#if UNITY_EDITOR
using System;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class UpgradeDataImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "UpgradeData.xlsx";
		public string SheetName => "upgrade_data";
		public string ButtonLabel => "Upgrade 데이터 가져오기";
		// Excel 헤더 안내 (H: upgrade_type, I: stat_type)
		public string[] Headers => new[]
		{
			"id", "upgrade_name", "description", "need_money",
			"upgrade_value", "max_upgrade_count", "icon_id",
			"upgrade_type", "stat_type", "growth_rate"
		};

		public int Import(DB db, string excelPath)
		{
			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new UpgradeRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.id) && string.IsNullOrEmpty(r.name)) continue;

				var list = db.upgradeDataList;
				UpgradeData existing = null;
				if (!string.IsNullOrEmpty(r.id))
				{
					existing = list.Find(u => u.Id == r.id);
				}
				if (existing == null && !string.IsNullOrEmpty(r.name))
				{
					existing = list.Find(u => u.Name == r.name);
				}

				if (existing == null)
				{
					var obj = (UpgradeData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UpgradeData));
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

		private static void Copy(UpgradeRow r, UpgradeData target)
		{
			var t = typeof(UpgradeData);

			// 기본 필드 매핑
			SetPrivateField(t, target, "id", r.id);

            string formattedName = r.name;
            try
            {
                // 사용자 요청: B(name)의 {1}에는 (upgradeValue * maxUpgradeCount) 값을 넣고, {0}은 그대로 둠 (ShopItemUI에서 처리)
                // string.Format에서 literal "{0}"을 첫 번째 인자로 넘겨서 {0} 자리는 그대로 유지되게 함
                float maxTotalValue = r.upgradeValue * r.maxUpgradeCount;
                formattedName = string.Format(r.name, "{0}", maxTotalValue);
            }
            catch { }
            SetPrivateField(t, target, "name", formattedName);
			SetPrivateField(t, target, "description", r.description);
			SetPrivateField(t, target, "needMoney", r.needMoney);
			SetPrivateField(t, target, "upgradeValue", r.upgradeValue);
			SetPrivateField(t, target, "maxUpgradeCount", r.maxUpgradeCount);
			SetPrivateField(t, target, "iconId", r.iconId);

			// growthRate: 시트 값이 유효하지 않으면 기본값 1.065f 사용
			float growthRate = r.growthRate > 0f ? r.growthRate : 1.065f;
			SetPrivateField(t, target, "growthRate", growthRate);

			// Upgrade 타입 결정
			var upgradeType = ParseUpgradeType(r.upgradeType, r.statType);
			SetPrivateField(t, target, "upgradeDataType", upgradeType);

			// TrainUpgrade인 경우: stats 배열 채우기 (값은 upgradeValue 사용)
			if (upgradeType == UpgradeDataType.TrainUpgrade && !string.IsNullOrEmpty(r.statType))
			{
				if (Enum.TryParse<StatType>(r.statType, ignoreCase: true, out var statType))
				{
					var stats = new SimpleStat[]
					{
						new SimpleStat
						{
							Type = statType,
							Value = r.upgradeValue
						}
					};

					SetPrivateField(t, target, "stats", stats);
				}
				else
				{
					// 잘못된 statType이면 stats를 비워둠
					SetPrivateField(t, target, "stats", Array.Empty<SimpleStat>());
				}
			}
			else
			{
				// NonTrainUpgrade 등: 스탯 배열은 비움
				SetPrivateField(t, target, "stats", Array.Empty<SimpleStat>());
			}
		}

		private static UpgradeDataType ParseUpgradeType(string rawType, string statType)
		{
			if (!string.IsNullOrEmpty(rawType))
			{
				// 문자열 이름으로 우선 파싱
				if (Enum.TryParse<UpgradeDataType>(rawType, ignoreCase: true, out var parsed))
				{
					return parsed;
				}

				// 숫자(0,1)로 들어온 경우 처리
				if (int.TryParse(rawType, out var intValue))
				{
					if (Enum.IsDefined(typeof(UpgradeDataType), intValue))
					{
						return (UpgradeDataType)intValue;
					}
				}
			}

			// fallback: statType이 있으면 TrainUpgrade, 아니면 NonTrainUpgrade
			return string.IsNullOrEmpty(statType)
				? UpgradeDataType.NonTrainUpgrade
				: UpgradeDataType.TrainUpgrade;
		}

		private static void SetPrivateField(Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
			if (fi != null) fi.SetValue(instance, value);
		}

	}
}
#endif