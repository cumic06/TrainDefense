#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class StageRow : IExcelRow
	{
		public string id;
		public float baseInspectionTime;
		public int stationCount;
		public float stageEndTime;
		public float spawnInterval;
		public string spawnMonsters;
		public string spawnMonstersProbability;
		public string spawnMonstersLevel;

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			baseInspectionTime = map.GetFloat(row, "base_inspection_time");
			stationCount = map.GetInt(row, "station_count");
			stageEndTime = map.GetFloat(row, "stage_end_time");
			spawnInterval = map.GetFloat(row, "spawn_interval");
			spawnMonsters = map.GetString(row, "spawn_monsters");
			spawnMonstersProbability = map.GetString(row, "spawn_monsters_probability");
			spawnMonstersLevel = map.GetString(row, "spawn_monsters_level");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "base_inspection_time", baseInspectionTime);
			map.SetCell(row, "station_count", stationCount);
			map.SetCell(row, "stage_end_time", stageEndTime);
			map.SetCell(row, "spawn_interval", spawnInterval);
			map.SetCell(row, "spawn_monsters", spawnMonsters);
			map.SetCell(row, "spawn_monsters_probability", spawnMonstersProbability);
			map.SetCell(row, "spawn_monsters_level", spawnMonstersLevel);
		}
	}
}
#endif
