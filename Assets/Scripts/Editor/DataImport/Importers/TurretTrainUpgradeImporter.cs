#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class TurretTrainUpgradeImporter : IExcelSheetImporter
	{
		public string SheetName => "turret_train_upgrade_data";
		public string ButtonLabel => "TurretTrainUpgrade 데이터 가져오기";
		public string[] Headers => new[] { "upgrade_name", "description", "attack_range", "attack_damage", "attack_count", "attack_delay" };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new TurretTrainUpgradeRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.upgradeName)) continue;
				var list = db.turretTrainUpgradeDataList;
				var existing = list.Find(u => u.UpgradeName == r.upgradeName);
				if (existing == null)
				{
					var obj = (TurretTrainUpgradeData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TurretTrainUpgradeData));
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

		private static void Copy(TurretTrainUpgradeRow r, TurretTrainUpgradeData target)
		{
			var t = typeof(TurretTrainUpgradeData);
			SetPrivateField(t, target, "upgradeName", r.upgradeName);
			SetPrivateField(t, target, "description", r.description);
			
			var turretStatus = new TurretTrainStatus
			{
				AttackRange = r.attackRange,
				AttackDamage = r.attackDamage,
				AttackCount = r.attackCount,
				AttackDelay = r.attackDelay
			};
			SetPrivateField(t, target, "turretStatusUpgrade", turretStatus);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif

