#if UNITY_EDITOR
using System.Collections.Generic;
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class TurretTrainUpgradeImporter : IExcelSheetImporter
	{
		public string ExcelFileName => "TrainUpgradeData.xlsx";
		public string SheetName => "turret_train_upgrade_data";
		public string ButtonLabel => "TurretTrainUpgrade 데이터 가져오기";
		// RangeTrainUpgrade와 동일하게 attack_interval 명칭 사용
		public string[] Headers => new[] { "id", "upgrade_name", "description", "max_hp", "icon_id", "attack_damage", "attack_range", "attack_count", "attack_interval", "critical_chance", "critical_damage" };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			var list = db.turretTrainUpgradeDataList;

			// 같은 ID를 가진 행들을 그룹화
			var groupedRows = new Dictionary<string, List<TurretTrainUpgradeRow>>();

			var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
			foreach (var row in rows)
			{
				var r = new TurretTrainUpgradeRow();
				r.FromExcelRow(row, map);
				if (string.IsNullOrEmpty(r.id) && string.IsNullOrEmpty(r.name)) continue;

				string key = r.id ?? r.name;
				if (!groupedRows.ContainsKey(key))
				{
					groupedRows[key] = new List<TurretTrainUpgradeRow>();
				}
				groupedRows[key].Add(r);
			}

			// 각 그룹에 대해 업그레이드 데이터 생성
			foreach (var group in groupedRows)
			{
				var rowsForId = group.Value;
				if (rowsForId.Count == 0) continue;

				string baseId = rowsForId[0].id ?? rowsForId[0].name;
				TurretTrainUpgradeData existing = list.Find(u => u.Id == baseId);

				if (existing == null)
				{
					var obj = (TurretTrainUpgradeData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TurretTrainUpgradeData));
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

		private static void Copy(List<TurretTrainUpgradeRow> rows, TurretTrainUpgradeData target)
		{
			if (rows == null || rows.Count == 0) return;

			var t = typeof(TurretTrainUpgradeData);
			var firstRow = rows[0];

			SetPrivateField(t, target, "id", firstRow.id);
			SetPrivateField(t, target, "name", firstRow.name);
			SetPrivateField(t, target, "description", firstRow.description);
			SetPrivateField(t, target, "iconId", firstRow.iconId);

			// upgradeStats 배열 생성 (인덱스 = 레벨 - 1)
			var upgradeStatsArray = new TurretTrainUpgradeStats[rows.Count];
			var statsType = typeof(TurretTrainUpgradeStats);

			for (int i = 0; i < rows.Count; i++)
			{
				var r = rows[i];
				var upgradeStats = new TurretTrainUpgradeStats();

				var statusUpgrade = new TrainStatusData { MaxHp = r.maxHp };
				SetPrivateField(statsType, upgradeStats, "statusUpgrade", statusUpgrade);

				var turretStatus = new TurretTrainStatus
				{
					AttackDamage = r.attackDamage,
					AttackRange = r.attackRange,
					AttackCount = r.attackCount,
					AttackInterval = r.attackInterval,
					CriticalChance = r.criticalChance,
					CriticalDamage = r.criticalDamage
				};
				SetPrivateField(statsType, upgradeStats, "turretStatusUpgrade", turretStatus);

				upgradeStatsArray[i] = upgradeStats;
			}

			SetPrivateField(t, target, "upgradeStats", upgradeStatsArray);
			SetPrivateField(t, target, "level", 1); // 기본 레벨은 1
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif

