#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

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
			foreach (var row in rows)
			{
				var r = new TrainUpgradeRow();
				r.FromExcelRow(row);
			if (string.IsNullOrEmpty(r.name)) continue;
			var list = db.trainUpgradeDataList;
			var existing = list.Find(u => u.Name == r.name);
				if (existing == null)
				{
					var obj = (TrainUpgradeData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrainUpgradeData));
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

		private static void Copy(TrainUpgradeRow r, TrainUpgradeData target)
		{
			var t = typeof(TrainUpgradeData);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "name", r.name);
			SetPrivateField(t, target, "description", r.description);
			SetPrivateField(t, target, "iconId", r.iconId);
			
			var statusUpgrade = new TrainStatusData { MaxHp = r.maxHp };
			SetPrivateField(t, target, "statusUpgrade", statusUpgrade);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}

	}
}
#endif


