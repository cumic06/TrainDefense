#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class RangeTrainUpgradeImporter : IExcelSheetImporter
	{
		public string SheetName => "range_train_upgrade_data";
		public string ButtonLabel => "RangeTrainUpgrade 데이터 가져오기";
		public string[] Headers => new[] { "upgrade_name", "description", "attack_range", "attack_damage", "attack_count", "attack_interval" };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new RangeTrainUpgradeRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.upgradeName)) continue;
				var list = db.rangeTrainUpgradeDataList;
				var existing = list.Find(u => u.UpgradeName == r.upgradeName);
				if (existing == null)
				{
					var obj = (RangeTrainUpgradeData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(RangeTrainUpgradeData));
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

		private static void Copy(RangeTrainUpgradeRow r, RangeTrainUpgradeData target)
		{
			var t = typeof(RangeTrainUpgradeData);
			SetPrivateField(t, target, "upgradeName", r.upgradeName);
			SetPrivateField(t, target, "description", r.description);
			
			var rangeStatus = new RangeAttackTrainStatus
			{
				AttackRange = r.attackRange,
				AttackDamage = r.attackDamage,
				AttackCount = r.attackCount,
				AttackInterval = r.attackInterval
			};
			SetPrivateField(t, target, "rangeStatusUpgrade", rangeStatus);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif

