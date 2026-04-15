#if UNITY_EDITOR
using System.Collections.Generic;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
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
			SetPrivateField(t, target, "buffs", ParseBuffs(r.skillBuffsRaw));
		}

		private static List<TrainSkillBuffEntry> ParseBuffs(string raw)
		{
			var list = new List<TrainSkillBuffEntry>();
			if (string.IsNullOrWhiteSpace(raw)) return list;

			var entries = raw.Split('|');
			foreach (var e in entries)
			{
				if (string.IsNullOrWhiteSpace(e)) continue;
				var parts = e.Split(':');
				if (parts.Length < 2) continue;
				if (!System.Enum.TryParse<StatType>(parts[0].Trim(), true, out var stat)) continue;
				if (!float.TryParse(parts[1].Trim(), out var percent)) continue;
				list.Add(new TrainSkillBuffEntry(stat, percent));
			}
			return list;
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}
	}
}
#endif
