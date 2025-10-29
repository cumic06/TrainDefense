#if UNITY_EDITOR
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Editor.DataImport.Importers.Rows;

namespace TrainDefense.Editor.DataImport.Importers
{
	public class ChoiceImporter : IExcelSheetImporter
	{
		public string SheetName => "choice_option";
		public string ButtonLabel => "Choice 데이터 가져오기";
		public string[] Headers => new[] { "id", "choice_type", "target_train_id" };

		public int Import(DB db, string excelPath)
		{
			// Choice 데이터는 더 이상 엑셀에서 가져오지 않음
			// TriChoiceDB에서 직접 관리하도록 변경됨
			Debug.LogWarning("Choice 데이터 가져오기는 더 이상 지원되지 않습니다. TriChoiceDB에서 직접 관리하세요.");
			return 0;
		}

		private static void Copy(ChoiceRow r, ChoiceOption target)
		{
			var t = typeof(ChoiceOption);
			SetPrivateField(t, target, "id", r.id);
			// choiceType and references are runtime; we only set targetTrainId for now
			SetPrivateField(t, target, "targetTrainId", r.targetTrainId);
		}

		private static void SetPrivateField(System.Type type, object instance, string field, object value)
		{
			var fi = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (fi != null) fi.SetValue(instance, value);
		}

	}
}
#endif


