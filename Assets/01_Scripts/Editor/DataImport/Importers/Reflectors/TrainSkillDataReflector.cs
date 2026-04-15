#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers.Reflectors
{
	public class TrainSkillDataReflector
	{
		public TrainSkillData Create(TrainSkillDataRow r)
		{
			var obj = new TrainSkillData();
			Copy(r, obj);
			return obj;
		}

		public void Copy(TrainSkillDataRow r, TrainSkillData target)
		{
			var t = typeof(TrainSkillData);
			SetPrivateField(t, target, "id", r.id);
			SetPrivateField(t, target, "skillType", r.skillType);
			SetPrivateField(t, target, "skillCooldown", r.skillCooldown);
			SetPrivateField(t, target, "skillIconId", r.skillIconId);

			var projectileData = new TrainSkillProjectileData();
			var projectileType = typeof(TrainSkillProjectileData);
			SetPrivateField(projectileType, projectileData, "projectilePrefabId", r.skillProjectilePrefabId);
			SetPrivateField(projectileType, projectileData, "damage", r.skillProjectileDamage);
			SetPrivateField(projectileType, projectileData, "range", r.skillProjectileRange);
			SetPrivateField(projectileType, projectileData, "projectileCount", r.skillProjectileCount);
			SetPrivateField(t, target, "projectileData", projectileData);

			SetPrivateField(t, target, "buffDuration", r.skillBuffDuration);
			SetPrivateField(t, target, "buffStatType", r.skillBuffStatType);
			SetPrivateField(t, target, "buffPercent", r.skillBuffPercent);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif
