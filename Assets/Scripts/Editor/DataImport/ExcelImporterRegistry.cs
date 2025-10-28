#if UNITY_EDITOR
using System.Collections.Generic;

namespace TrainDefense.Editor.DataImport
{
	public static class ExcelImporterRegistry
	{
		private static readonly List<IExcelSheetImporter> _importers = new List<IExcelSheetImporter>();

		public static IReadOnlyList<IExcelSheetImporter> Importers => _importers;

		public static void Register(IExcelSheetImporter importer)
		{
			if (importer == null) return;
			if (!_importers.Contains(importer))
			{
				_importers.Add(importer);
			}
		}

		public static void Clear()
		{
			_importers.Clear();
		}
	}
}
#endif


