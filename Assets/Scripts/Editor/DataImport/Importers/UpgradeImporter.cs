#if UNITY_EDITOR
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class UpgradeImporter : IExcelSheetImporter
	{
		public string SheetName => "upgrade_data";
		public string ButtonLabel => "Upgrade 데이터 가져오기";
		public string[] Headers => new[] { "id", "upgrade_name", "description", "need_money", "upgrade_value", "max_upgrade_count" };
		public IExcelRow[] ExampleRows => new IExcelRow[] { new UpgradeRow { id = "up_hp", upgradeName = "HP+", description = "more hp", needMoney = 100, upgradeValue = 10f, maxUpgradeCount = 5 } };

		public int Import(TrainDefense.Game.Datas.DB db, string excelPath)
		{
			var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
			int imported = 0;
			foreach (var row in rows)
			{
				var r = new UpgradeRow();
				r.FromExcelRow(row);
				if (string.IsNullOrEmpty(r.id)) continue;
				var list = db.upgradeDataList;
				var existing = list.Find(u => u.Id == r.id);
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
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "upgradeName", r.upgradeName);
			SetPrivateField(t, target, "description", r.description);
			SetPrivateField(t, target, "needMoney", r.needMoney);
			SetPrivateField(t, target, "upgradeValue", r.upgradeValue);
			SetPrivateField(t, target, "maxUpgradeCount", r.maxUpgradeCount);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}

	}
}
#endif


