#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
    public class MonsterRow : IExcelRow
    {
        public string id;
        public string name;
        public string description;
        public float maxHp;
        public float damage;
        public float moveSpeed;
        public float attackDelay;
        public int dropExpMin;
        public int dropExpMax;
        public int dropMoneyMin;
        public int dropMoneyMax;
        public float attackRange;
        public MonsterAttackType attackType;
        public string prefabId;

        public void FromExcelRow(IRow row, HeaderMap map)
        {
            id = map.GetString(row, "id");
            name = map.GetString(row, "monster_name");
            description = map.GetString(row, "description");
            maxHp = map.GetFloat(row, "max_hp");
            damage = map.GetFloat(row, "damage");
            moveSpeed = map.GetFloat(row, "move_speed");
            attackDelay = map.GetFloat(row, "attack_delay");
            dropExpMin = map.GetInt(row, "drop_exp_min");
            dropExpMax = map.GetInt(row, "drop_exp_max");
            dropMoneyMin = map.GetInt(row, "drop_money_min");
            dropMoneyMax = map.GetInt(row, "drop_money_max");
            attackRange = map.GetFloat(row, "attack_range");
            attackType = map.GetEnum<MonsterAttackType>(row, "attack_type");
            prefabId = map.GetString(row, "prefab_id");
        }

        public void ToExcelRow(IRow row, HeaderMap map)
        {
            map.SetCell(row, "id", id);
            map.SetCell(row, "monster_name", name);
            map.SetCell(row, "description", description);
            map.SetCell(row, "max_hp", maxHp);
            map.SetCell(row, "damage", damage);
            map.SetCell(row, "move_speed", moveSpeed);
            map.SetCell(row, "attack_delay", attackDelay);
            map.SetCell(row, "drop_exp_min", dropExpMin);
            map.SetCell(row, "drop_exp_max", dropExpMax);
            map.SetCell(row, "drop_money_min", dropMoneyMin);
            map.SetCell(row, "drop_money_max", dropMoneyMax);
            map.SetCell(row, "attack_range", attackRange);
            map.SetCell(row, "attack_type", attackType);
            map.SetCell(row, "prefab_id", prefabId);
        }
    }
}
#endif
