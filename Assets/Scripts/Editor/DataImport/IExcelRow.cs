using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport
{
	/// <summary>
	/// Row adapter for Excel read/write operations.
	/// </summary>
	public interface IExcelRow
	{
		void FromExcelRow(IRow row);
		void ToExcelRow(IRow row);
	}
}


