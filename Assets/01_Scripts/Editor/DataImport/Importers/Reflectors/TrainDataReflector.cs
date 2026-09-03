#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers.Reflectors
{
	public class TrainDataReflector
	{
		public TrainData Create(TrainRow r)
		{
			var obj = (TrainData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TrainData));
			Copy(r, obj);
			return obj;
		}

		public void Copy(TrainRow r, TrainData target)
		{
			var t = typeof(TrainData);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "name", r.name);
			SetPrivateField(t, target, "description", r.description);
			SetPrivateField(t, target, "isMainTrain", r.isMainTrain);
			SetPrivateField(t, target, "prefabId", r.prefabId);
			SetPrivateField(t, target, "iconId", r.iconId);
			
			var trainStatusData = new TrainStatusData { MaxHp = r.maxHp };
			SetPrivateField(t, target, "trainStatusData", trainStatusData);

			SetPrivateField(t, target, "passiveSkillDataIds", r.passiveSkillDataIds);
			SetPrivateField(t, target, "passiveSkillDatasResolved", false);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif
