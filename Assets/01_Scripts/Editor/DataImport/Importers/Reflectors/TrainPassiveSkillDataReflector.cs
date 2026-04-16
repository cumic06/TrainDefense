#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers.Reflectors
{
	public class TrainPassiveSkillDataReflector
	{
		public TrainPassiveSkillData Create(TrainPassiveSkillDataRow r)
		{
			var obj = new TrainPassiveSkillData();
			Copy(r, obj);
			return obj;
		}

		public void Copy(TrainPassiveSkillDataRow r, TrainPassiveSkillData target)
		{
			var t = typeof(TrainPassiveSkillData);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "passiveType", r.passiveType);
			SetPrivateField(t, target, "param1", r.param1);
			SetPrivateField(t, target, "param2", r.param2);
			SetPrivateField(t, target, "param3", r.param3);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif
