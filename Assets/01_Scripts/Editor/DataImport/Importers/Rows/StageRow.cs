#if UNITY_EDITOR
using NPOI.SS.UserModel;
using Unity.VisualScripting;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
   public class StageRow : IExcelRow
   {
      public string id;
      public string stageInspectionTime;
      public float stageEndTime;
      public float spawnInterval;
      public string spawnMonsters;
      public string spawnMonstersProbability;
      public string spawnMonstersLevel;

      public void FromExcelRow(IRow row)
      {
         id = row.GetCell(0)?.ToString();
         stageInspectionTime = row.GetCell(1)?.ToString();
         float.TryParse(row.GetCell(2)?.ToString(), out stageEndTime);
         float.TryParse(row.GetCell(3)?.ToString(), out spawnInterval);
         spawnMonsters = row.GetCell(4)?.ToString();
         spawnMonstersProbability = row.GetCell(5)?.ToString();
         spawnMonstersLevel = row.GetCell(6)?.ToString();
      }

      public void ToExcelRow(IRow row)
      {
         Set(row, 0, id);
         Set(row, 1, stageInspectionTime);
         Set(row, 2, stageEndTime);
         Set(row, 3, spawnInterval);
         Set(row, 4, spawnMonsters);
         Set(row, 5, spawnMonstersProbability);
         Set(row, 6, spawnMonstersLevel);
      }

      private static void Set(IRow row, int idx, object value)
      {
         var cell = row.GetCell(idx) ?? row.CreateCell(idx);
         if (value is null)
            cell.SetCellValue(string.Empty);
         else if (value is float f)
            cell.SetCellValue(f);
         else
            cell.SetCellValue(value.ToString());
      }
   }
}
#endif