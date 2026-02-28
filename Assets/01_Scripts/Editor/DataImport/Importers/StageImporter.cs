#if UNITY_EDITOR
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Game.Datas;

namespace TrainDefense.Editor.DataImport.Importers
{
   public class StageImporter : IExcelSheetImporter
   {
      public string SheetName => "stage_data";
      public string ButtonLabel => "Stage 데이터 가져오기";
      public string[] Headers => new[] { "id", "stage_inspection_time", "stage_end_time" };

      public int Import(DB db, string excelPath)
      {
         var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
         int imported = 0;
         foreach (var row in rows)
         {
            var r = new StageRow();
            r.FromExcelRow(row);
            if (string.IsNullOrEmpty(r.id))
               continue;
            var existing = db.stageDataList.Find(s => s.Id == r.id);
            if (existing == null)
            {
               var obj = (StageData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(StageData));
               Copy(r, obj);
               db.stageDataList.Add(obj);
            }
            else
            {
               Copy(r, existing);
            }
            imported++;
         }
         return imported;
      }

      private static void Copy(StageRow r, StageData target)
      {
         var t = typeof(StageData);
         SetPrivateField(t, target, "id", r.id);
         SetPrivateField(t, target, "stageInspectionTime", ParseFloatArray(r.stageInspectionTime));
         SetPrivateField(t, target, "stageEndTime", r.stageEndTime);
         SetPrivateField(t, target, "spawnInterval", r.spawnInterval);
         SetPrivateField(t, target, "spawnDatas", ParseSpawnData(r.spawnMonsters, r.spawnMonstersProbability, r.spawnMonstersLevel));
      }

      private static float[] ParseFloatArray(string csv)
      {
         if (string.IsNullOrEmpty(csv))
            return System.Array.Empty<float>();
         var parts = csv.Split(';');
         var list = new System.Collections.Generic.List<float>();
         foreach (var part in parts)
         {
            if (string.IsNullOrWhiteSpace(part))
               continue;
            if (float.TryParse(part.Trim(), out float val))
            {
               list.Add(val);
            }
         }
         return list.ToArray();
      }

      private static StageSpawnData[] ParseSpawnData(string idsCsv, string probsCsv, string levelCsv)
      {
         if (string.IsNullOrEmpty(idsCsv))
            return System.Array.Empty<StageSpawnData>();

         var idsParts = idsCsv.Split(';');
         var probsParts = string.IsNullOrEmpty(probsCsv) ? System.Array.Empty<string>() : probsCsv.Split(';');
         var levelParts = string.IsNullOrEmpty(levelCsv) ? System.Array.Empty<string>() : levelCsv.Split(';');

         var list = new System.Collections.Generic.List<StageSpawnData>();

         for (int i = 0; i < idsParts.Length; i++)
         {
            if (string.IsNullOrWhiteSpace(idsParts[i]))
               continue;

            string id = idsParts[i].Trim();
            if (!string.IsNullOrEmpty(id))
            {
               float prob = 0f;
               if (i < probsParts.Length && float.TryParse(probsParts[i].Trim(), out float p))
               {
                  prob = p;
               }

               int level = 1;
               if (i < levelParts.Length && int.TryParse(levelParts[i].Trim(), out int l))
               {
                  level = l;
               }

               list.Add(new StageSpawnData(id, prob, level));
            }
         }
         return list.ToArray();
      }

      private static void SetPrivateField(System.Type type, object instance, string field, object value)
      {
         var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
         if (fi != null)
            fi.SetValue(instance, value);
      }

   }
}
#endif