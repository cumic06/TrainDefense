#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
    public class StageRow : IExcelRow
    {
        public string id;
        public string stageInspectionTime;
        public float stageEndTime;
        public string spawnMonsters;
        public string spawnMonstersProbability;

        public void FromExcelRow(IRow row)
        {
            id = row.GetCell(0)?.ToString();
            stageInspectionTime = row.GetCell(1)?.ToString();
            float.TryParse(row.GetCell(2)?.ToString(), out stageEndTime);
            spawnMonsters = row.GetCell(3)?.ToString();
            spawnMonstersProbability = row.GetCell(4)?.ToString();
        }

        public void ToExcelRow(IRow row)
        {
            Set(row, 0, id);
            Set(row, 1, stageInspectionTime);
            Set(row, 2, stageEndTime);
            Set(row, 3, spawnMonsters);
            Set(row, 4, spawnMonstersProbability);
        }

        private static void Set(IRow row, int idx, object value)
        {
            var cell = row.GetCell(idx) ?? row.CreateCell(idx);
            if (value is null) cell.SetCellValue(string.Empty);
            else if (value is float f) cell.SetCellValue(f);
            else cell.SetCellValue(value.ToString());
        }
    }
}
#endif