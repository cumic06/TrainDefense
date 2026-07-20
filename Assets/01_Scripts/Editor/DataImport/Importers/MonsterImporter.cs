#if UNITY_EDITOR
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Editor.DataImport.Importers.Reflectors;

namespace TrainDefense.Editor.DataImport.Importers
{
    public class MonsterImporter : IExcelSheetImporter
    {
        public string ExcelFileName => "MonsterData.xlsx";
        public string SheetName => "monster_data";
        public string ButtonLabel => "Monster 데이터 가져오기";
        public string[] Headers => new[] { "id", "monster_name", "description", "max_hp", "damage", "move_speed", "attack_delay", "drop_exp_min", "drop_exp_max", "drop_money_min", "drop_money_max", "attack_range", "attack_type", "prefab_id" };

        public int Import(DB db, string excelPath)
        {
            var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
            var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
            int imported = 0;
            foreach (var row in rows)
            {
                var mr = new MonsterRow();
                mr.FromExcelRow(row, map);
                if (string.IsNullOrEmpty(mr.id)) continue;
                var existing = db.monsterDataList.Find(m => m.Id == mr.id);
                if (existing == null)
                {
                    var newData = new MonsterDataReflector().Create(mr);
                    db.monsterDataList.Add(newData);
                }
                else
                {
                    new MonsterDataReflector().Copy(mr, existing);
                }
                imported++;
            }

            return imported;
        }
    }
}
#endif