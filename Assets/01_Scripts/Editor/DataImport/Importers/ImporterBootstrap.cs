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
			ExcelImporterRegistry.Register(new TrainSkillDataImporter());
			ExcelImporterRegistry.Register(new TrainPassiveSkillDataImporter());
			ExcelImporterRegistry.Register(new TrainImporter());
			ExcelImporterRegistry.Register(new RangeTrainImporter());
			ExcelImporterRegistry.Register(new TurretTrainImporter());
			ExcelImporterRegistry.Register(new StageImporter());
			ExcelImporterRegistry.Register(new UpgradeDataImporter());
			ExcelImporterRegistry.Register(new PermanentUpgradeImporter());
			ExcelImporterRegistry.Register(new AddTrainChoiceImporter());
			ExcelImporterRegistry.Register(new UpgradeTrainChoiceImporter());
			ExcelImporterRegistry.Register(new RewardChoiceImporter());
			ExcelImporterRegistry.Register(new StatUpgradeTierImporter());
			ExcelImporterRegistry.Register(new StatUpgradeStatImporter());
			ExcelImporterRegistry.Register(new TrainStatUpgradeRuleImporter());
		}
	}
}
#endif


