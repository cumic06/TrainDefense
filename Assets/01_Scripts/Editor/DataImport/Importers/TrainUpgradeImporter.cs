#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;
using System.Collections.Generic;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class TrainUpgradeImporter : IExcelSheetImporter
	{
		public string SheetName => "train_upgrade_data";
		public string ButtonLabel => "TrainUpgrade 데이터 가져오기";
		public string[] Headers => new[] { "id", "upgrade_name", "description", "max_hp", "icon_id" };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			var list = db.trainUpgradeDataList;

			// 같은 ID를 가진 행들을 그룹화
			var groupedRows = new Dictionary<string, List<TrainUpgradeRow>>();

			foreach (var row in rows)
			{
				var r = new TrainUpgradeRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.name)) continue;

				string key = r.id ?? r.name;
				if (!groupedRows.ContainsKey(key))
				{
					groupedRows[key] = new List<TrainUpgradeRow>();
				}
				groupedRows[key].Add(r);
			}

			// 각 그룹에 대해 업그레이드 데이터 생성
			foreach (var group in groupedRows)
			{
				var rowsForId = group.Value;
				if (rowsForId.Count == 0) continue;

				string baseId = rowsForId[0].id ?? rowsForId[0].name;
				TrainUpgradeData existing = list.Find(u => u.Id == baseId);

				if (existing == null && !string.IsNullOrEmpty(rowsForId[0].name))
				{
					existing = list.Find(u => u.Name == rowsForId[0].name);
				}

				if (existing == null)
				{
					var obj = (TrainUpgradeData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrainUpgradeData));
					Copy(rowsForId, obj);
					list.Add(obj);
					imported++;
				}
				else
				{
					Copy(rowsForId, existing);
					imported++;
				}
			}

			return imported;
		}

		private static void Copy(List<TrainUpgradeRow> rows, TrainUpgradeData target)
		{
			if (rows == null || rows.Count == 0) return;

			var t = typeof(TrainUpgradeData);
			var firstRow = rows[0];

			SetPrivateField(t, target, "id", firstRow.id);
			SetPrivateField(t, target, "name", firstRow.name);
			SetPrivateField(t, target, "description", firstRow.description);
			SetPrivateField(t, target, "iconId", firstRow.iconId);

			// upgradeStats 배열 생성 (인덱스 = 레벨 - 1)
			var upgradeStatsArray = new TrainUpgradeStats[rows.Count];
			var statsType = typeof(TrainUpgradeStats);

			for (int i = 0; i < rows.Count; i++)
			{
				var r = rows[i];
				var upgradeStats = new TrainUpgradeStats();

				var statusUpgrade = new TrainStatusData { MaxHp = r.maxHp };
				SetPrivateField(statsType, upgradeStats, "statusUpgrade", statusUpgrade);

				upgradeStatsArray[i] = upgradeStats;
			}

			SetPrivateField(t, target, "upgradeStats", upgradeStatsArray);
			SetPrivateField(t, target, "level", 1); // 기본 레벨은 1
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}

	}
}
#endif


