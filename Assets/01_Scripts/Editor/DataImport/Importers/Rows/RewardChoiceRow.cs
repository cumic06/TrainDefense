#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
	public class RewardChoiceRow : IExcelRow
	{
		public string id;
		public string rewardType;          // emergency_repair / gold / elite_currency
		public string nameKey;
		public string descriptionKey;
		public string iconId;
		public int weight;
		public int amount;                 // 골드량 / 엘리트 재화량 (긴급 수리는 미사용)
		public float aliveHealRatio;       // 긴급 수리: 살아있던 기차 회복 비율 (0~1)
		public float revivedHpRatio;       // 긴급 수리: 부활 기차 HP 비율 (0~1)

		public void FromExcelRow(IRow row, HeaderMap map)
		{
			id = map.GetString(row, "id");
			rewardType = map.GetString(row, "reward_type");
			nameKey = map.GetString(row, "name_key");
			descriptionKey = map.GetString(row, "description_key");
			iconId = map.GetString(row, "icon_id");
			weight = map.GetInt(row, "weight");
			amount = map.GetInt(row, "amount");
			aliveHealRatio = map.GetFloat(row, "alive_heal_ratio");
			revivedHpRatio = map.GetFloat(row, "revived_hp_ratio");
		}

		public void ToExcelRow(IRow row, HeaderMap map)
		{
			map.SetCell(row, "id", id);
			map.SetCell(row, "reward_type", rewardType);
			map.SetCell(row, "name_key", nameKey);
			map.SetCell(row, "description_key", descriptionKey);
			map.SetCell(row, "icon_id", iconId);
			map.SetCell(row, "weight", weight);
			map.SetCell(row, "amount", amount);
			map.SetCell(row, "alive_heal_ratio", aliveHealRatio);
			map.SetCell(row, "revived_hp_ratio", revivedHpRatio);
		}
	}
}
#endif
