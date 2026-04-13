using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport
{
	/// <summary>
	/// Row adapter for Excel read/write operations.
	/// HeaderMap을 통해 컬럼 이름 기반으로 데이터를 읽고 씁니다.
	/// </summary>
	public interface IExcelRow
	{
		void FromExcelRow(IRow row, HeaderMap map);
		void ToExcelRow(IRow row, HeaderMap map);
	}
}


