using System.Collections.Generic;

namespace Cumic.Excel
{
    /// <summary>
    /// Excel에서 로드 가능한 데이터 타입을 위한 인터페이스
    /// ScriptableObject가 Excel 데이터를 직접 로드할 수 있도록 지원
    /// </summary>
    public interface IExcelLoadable
    {
        /// <summary>
        /// Excel 행 데이터를 사용하여 객체를 초기화합니다
        /// </summary>
        /// <param name="rowData">Excel 행 데이터 (컬럼명을 키로 하는 딕셔너리)</param>
        void LoadFromExcelRow(Dictionary<string, object> rowData);

        /// <summary>
        /// Excel 컬럼명과 매핑되는 필드명을 반환합니다
        /// </summary>
        /// <returns>Excel 컬럼명을 키로 하는 딕셔너리 (값은 ScriptableObject의 필드명)</returns>
        Dictionary<string, string> GetColumnMapping();

        /// <summary>
        /// 필수 컬럼이 모두 존재하는지 검증합니다
        /// </summary>
        /// <param name="rowData">검증할 행 데이터</param>
        /// <returns>필수 컬럼이 모두 존재하면 true</returns>
        bool ValidateRequiredColumns(Dictionary<string, object> rowData);
    }
}
