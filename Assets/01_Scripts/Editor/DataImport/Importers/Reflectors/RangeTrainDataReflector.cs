using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers.Reflectors
{
	public class RangeTrainDataReflector
	{
		public RangeTrainData Create(RangeTrainRow r)
		{
			var obj = (RangeTrainData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(RangeTrainData));
			Copy(r, obj);
			return obj;
		}

		public void Copy(RangeTrainRow r, RangeTrainData target)
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

			var trainSkillData = (object)new TrainSkillData();
			var skillType = typeof(TrainSkillData);
			SetPrivateField(skillType, trainSkillData, "skillType", r.skillType);
			SetPrivateField(skillType, trainSkillData, "skillCooldown", r.skillCooldown);
			SetPrivateField(skillType, trainSkillData, "skillIconId", r.skillIconId);
			var projectileData = new TrainSkillProjectileData();
			var projectileType = typeof(TrainSkillProjectileData);
			SetPrivateField(projectileType, projectileData, "projectilePrefabId", r.skillProjectilePrefabId);
			SetPrivateField(projectileType, projectileData, "damage", r.skillProjectileDamage);
			SetPrivateField(projectileType, projectileData, "range", r.skillProjectileRange);
			SetPrivateField(projectileType, projectileData, "projectileCount", r.skillProjectileCount);
			SetPrivateField(skillType, trainSkillData, "projectileData", projectileData);
			SetPrivateField(t, target, "trainSkillData", trainSkillData);

			var rangeTrainType = typeof(RangeTrainData);
			var rangeTrainStatus = new RangeTrainStatus
			{
				AttackRange = r.attackRange,
				AttackDamage = r.attackDamage,
				AttackCount = r.attackCount,
				AttackInterval = r.attackInterval,
				CriticalChance = r.criticalChance,
				CriticalDamage = r.criticalDamage
			};
			SetPrivateField(rangeTrainType, target, "rangeTrainStatus", rangeTrainStatus);
			SetPrivateField(rangeTrainType, target, "rangeProjectilePrefabId", r.rangeProjectilePrefabId);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
