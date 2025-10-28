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
			SetPrivateField(t, target, "trainName", r.trainName);
			SetPrivateField(t, target, "description", r.description);
			SetPrivateField(t, target, "isMainTrain", r.isMainTrain);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif


