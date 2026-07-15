#if UNITY_EDITOR
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Game.Datas;

namespace TrainDefense.Editor.DataImport.Importers
{
   public class StageImporter : IExcelSheetImporter
   {
      public string ExcelFileName => "StageData.xlsx";
      public string SheetName => "stage_data";
      public string ButtonLabel => "Stage 데이터 가져오기";
      public string[] Headers => new[] { "id", "name_key", "base_inspection_time", "station_count", "stage_end_time", "spawn_interval", "spawn_monsters", "spawn_monsters_probability", "spawn_monsters_level", "spawn_monsters_elite_chance", "spawn_monsters_elite_ramp" };

      public int Import(DB db, string excelPath)
      {
         var map = ExcelReadUtil.ReadHeaderMap(excelPath, SheetName);
         var rows = ExcelReadUtil.ReadRows(excelPath, SheetName);
         int imported = 0;
         foreach (var row in rows)
         {
            var r = new StageRow();
            r.FromExcelRow(row, map);
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
         SetPrivateField(t, target, "nameKey", r.nameKey);
         SetPrivateField(t, target, "baseInspectionTime", r.baseInspectionTime);
         SetPrivateField(t, target, "stationCount", r.stationCount);
         SetPrivateField(t, target, "stageEndTime", r.stageEndTime);
         SetPrivateField(t, target, "spawnInterval", r.spawnInterval);
         SetPrivateField(t, target, "spawnDatas", ParseSpawnData(r.spawnMonsters, r.spawnMonstersProbability, r.spawnMonstersLevel, r.spawnMonstersEliteChance, r.spawnMonstersEliteRamp));
      }

      private static StageSpawnData[] ParseSpawnData(string idsCsv, string probsCsv, string levelCsv, string eliteChanceCsv, string eliteRampCsv)
      {
         if (string.IsNullOrEmpty(idsCsv))
            return System.Array.Empty<StageSpawnData>();

         var idsParts = idsCsv.Split(';');
         var probsParts = string.IsNullOrEmpty(probsCsv) ? System.Array.Empty<string>() : probsCsv.Split(';');
         var levelParts = string.IsNullOrEmpty(levelCsv) ? System.Array.Empty<string>() : levelCsv.Split(';');
         var eliteChanceParts = string.IsNullOrEmpty(eliteChanceCsv) ? System.Array.Empty<string>() : eliteChanceCsv.Split(';');
         var eliteRampParts = string.IsNullOrEmpty(eliteRampCsv) ? System.Array.Empty<string>() : eliteRampCsv.Split(';');

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

               // 비어 있으면 기본 1배(기존 동작 유지). 0이면 해당 몬스터는 엘리트로 등장하지 않음.
               float eliteChanceMultiplier = 1f;
               if (i < eliteChanceParts.Length && float.TryParse(eliteChanceParts[i].Trim(), out float e))
               {
                  eliteChanceMultiplier = e;
               }

               // 비어 있으면 기본 false(약한 몬스터: 램프 없이 바로 적용). true/1이면 한 판 동안 점점 증가(강한 몬스터).
               bool eliteChanceRamp = false;
               if (i < eliteRampParts.Length)
               {
                  string ramp = eliteRampParts[i].Trim();
                  eliteChanceRamp = ramp == "1" || ramp.Equals("true", System.StringComparison.OrdinalIgnoreCase);
               }

               list.Add(new StageSpawnData(id, prob, level, eliteChanceMultiplier, eliteChanceRamp));
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