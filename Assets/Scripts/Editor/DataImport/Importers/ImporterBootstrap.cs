#if UNITY_EDITOR
using UnityEditor;

namespace TrainDefense.Editor.DataImport.Importers
{
	/// <summary>
	/// Registers sheet importers on load.
	/// </summary>
	[InitializeOnLoad]
	public static class ImporterBootstrap
	{
		static ImporterBootstrap()
		{
			ExcelImporterRegistry.Clear();
			ExcelImporterRegistry.Register(new MonsterImporter());
			ExcelImporterRegistry.Register(new TrainImporter());
			ExcelImporterRegistry.Register(new RangeTrainImporter());
			ExcelImporterRegistry.Register(new TurretTrainImporter());
			ExcelImporterRegistry.Register(new TrainUpgradeImporter());
			ExcelImporterRegistry.Register(new TurretTrainUpgradeImporter());
			ExcelImporterRegistry.Register(new RangeTrainUpgradeImporter());
			ExcelImporterRegistry.Register(new StageImporter());
			ExcelImporterRegistry.Register(new UpgradeImporter());
			ExcelImporterRegistry.Register(new AddTrainChoiceImporter());
			ExcelImporterRegistry.Register(new UpgradeTrainChoiceImporter());
		}
	}
}
#endif


