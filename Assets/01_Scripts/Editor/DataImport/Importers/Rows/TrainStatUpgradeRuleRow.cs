#if UNITY_EDITOR
using System.Collections.Generic;
using NPOI.SS.UserModel;
using TrainDefense.Game.Stats;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	/// <summary>
	/// 포탑 한 대의 강화 규칙 한 줄 — 스탯이 컬럼으로 펼쳐진 가로 배치.
	/// 값이 비어 있는 스탯은 그 포탑의 강화 카드로 뜨지 않는다.
	/// </summary>
	public class TrainStatUpgradeRuleRow : IExcelRow
	{
		// 컬럼 이름 ↔ 스탯. 컬럼을 추가하려면 여기에만 넣으면 된다.
		public static readonly (string column, StatType statType)[] StatColumns =
		{
			("attack_damage", StatType.AttackDamage),
			("attack_interval", StatType.AttackInterval),
			("attack_area", StatType.AttackArea),
			("target_count", StatType.TargetCount),
			("attack_count", StatType.AttackCount),
		};

		public string trainDataId;

		// 값이 들어 있는 스탯만 담긴다(빈칸 = 카드 없음).
		public readonly Dictionary<StatType, float> increaseRates = new();

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			trainDataId = map.GetString(row, "train_data_id");
			increaseRates.Clear();

			foreach (var (column, statType) in StatColumns)
			{
				float value = map.GetFloat(row, column);

				if (value != 0f)
					increaseRates[statType] = value;
			}
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "train_data_id", trainDataId);

			foreach (var (column, statType) in StatColumns)
				map.SetCell(row, column, increaseRates.TryGetValue(statType, out var value) ? value : 0f);
		}
	}
}
#endif
