#if UNITY_EDITOR
namespace TrainDefense.Editor.DataImport
{
	public interface IExcelSheetImporter
	{
		string ExcelFileName { get; }
		string SheetName { get; }
		string ButtonLabel { get; }
		string[] Headers { get; }
		int Import(TrainDefense.Game.Datas.DB db, string excelPath);
	}
}
#endif


