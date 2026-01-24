#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers.Reflectors
{
    public class MonsterDataReflector
    {
        public MonsterData Create(MonsterRow r)
        {
            var obj = (MonsterData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(MonsterData));
            Copy(r, obj);
            return obj;
        }

        public void Copy(MonsterRow r, MonsterData target)
        {
            var t = typeof(MonsterData);
            SetPrivateField(t, target, "id", r.id);
            SetPrivateField(t, target, "name", r.name);
            SetPrivateField(t, target, "description", r.description);
            SetPrivateField(t, target, "prefabId", r.prefabId);
            SetPrivateField(t, target, "monsterStatusData", new MonsterStatusInfo
            {
                MaxHp = r.maxHp,
                Damage = r.damage,
                MoveSpeed = r.moveSpeed,
                AttackDelay = r.attackDelay,
                DropExpMin = r.dropExpMin,
                DropExpMax = r.dropExpMax,
                DropMoneyMin = r.dropMoneyMin,
                DropMoneyMax = r.dropMoneyMax,
                AttackRange = r.attackRange,
                AttackType = r.attackType,
            });
        }

        private static void SetPrivateField(System.Type type, object instance, string field, object value)
        {
            var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (fi != null)
            {
                fi.SetValue(instance, value);
            }
        }
    }
}
#endif
