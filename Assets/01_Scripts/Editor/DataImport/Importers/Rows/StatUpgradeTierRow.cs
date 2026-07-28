#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class StatUpgradeTierRow : IExcelRow
	{
		public string id;
		public int grade;                  // 표시용 등급 번호
		public float valueMultiplier;      // 증가량에 곱하는 배수
		public float costMultiplier;       // 가격에 곱하는 배수
		public int firstShopVisit;         // 등장 시작 (누적 상점 방문 수)
		public int lastShopVisit;          // 등장 종료. 0 이하면 끝까지
		public int weight;                 // 상점 추첨 가중치. 높을수록 자주 (고등급 = 희귀)

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			grade = map.GetInt(row, "grade");
			valueMultiplier = map.GetFloat(row, "value_multiplier");
			costMultiplier = map.GetFloat(row, "cost_multiplier");
			firstShopVisit = map.GetInt(row, "first_shop_visit");
			lastShopVisit = map.GetInt(row, "last_shop_visit");
			weight = map.GetInt(row, "weight");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "grade", grade);
			map.SetCell(row, "value_multiplier", valueMultiplier);
			map.SetCell(row, "cost_multiplier", costMultiplier);
			map.SetCell(row, "first_shop_visit", firstShopVisit);
			map.SetCell(row, "last_shop_visit", lastShopVisit);
			map.SetCell(row, "weight", weight);
		}
	}
}
#endif
