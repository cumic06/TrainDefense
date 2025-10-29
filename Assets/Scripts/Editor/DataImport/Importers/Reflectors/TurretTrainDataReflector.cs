using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers.Reflectors
{
	public class TurretTrainDataReflector
	{
		public TurretTrainData Create(TurretTrainRow r)
		{
			var obj = (TurretTrainData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(TurretTrainData));
			Copy(r, obj);
			return obj;
		}

		public void Copy(TurretTrainRow r, TurretTrainData target)
		{
			var t = typeof(TrainData);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "trainName", r.trainName);
			SetPrivateField(t, target, "description", r.description);
			SetPrivateField(t, target, "isMainTrain", r.isMainTrain);
			
			var trainStatusData = new TrainStatusData { MaxHp = r.maxHp };
			SetPrivateField(t, target, "trainStatusData", trainStatusData);

			var turretTrainType = typeof(TurretTrainData);
			var turretTrainStatus = new TurretTrainStatus
			{
				AttackRange = r.attackRange,
				AttackDamage = r.attackDamage,
				AttackCount = r.attackCount,
				AttackDelay = r.attackDelay
			};
			SetPrivateField(turretTrainType, target, "turretTrainStatus", turretTrainStatus);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}

