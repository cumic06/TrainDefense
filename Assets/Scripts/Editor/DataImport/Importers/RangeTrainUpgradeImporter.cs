#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class RangeTrainUpgradeImporter : IExcelSheetImporter
	{
		public string SheetName => "range_train_upgrade_data";
		public string ButtonLabel => "RangeTrainUpgrade 데이터 가져오기";
		public string[] Headers => new[] { "id", "upgrade_name", "description", "max_hp", "icon_id", "attack_range", "attack_damage", "attack_count", "attack_interval" };

		public int Import(DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new RangeTrainUpgradeRow();
				r.FromExcelRow(row);
			if (string.IsNullOrEmpty(r.id) && string.IsNullOrEmpty(r.name)) continue;
			var list = db.rangeTrainUpgradeDataList;
			RangeTrainUpgradeData existing = null;
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
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "name", r.name);
			SetPrivateField(t, target, "description", r.description);
			SetPrivateField(t, target, "iconId", r.iconId);
			
			var statusUpgrade = new TrainStatusData { MaxHp = r.maxHp };
			SetPrivateField(t, target, "statusUpgrade", statusUpgrade);
			
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

