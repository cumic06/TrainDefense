#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using TrainDefense.Editor.DataImport;
using TrainDefense.Game.Datas;

namespace TrainDefense.Editor
{
	public class DatabaseGeneratorWindow : EditorWindow
	{
		private string _excelPath;
		private string _databaseAssetPath = "Assets/Resources/Data/DB.asset";
		private bool _debugMode = false;

		[MenuItem("Tools/Database Generator")]
		public static void Open()
		{
			var win = GetWindow<DatabaseGeneratorWindow>(true, "Database 생성기");
			win.minSize = new Vector2(520, 220);
			win.Show();
		}

		private void OnGUI()
		{
			GUILayout.Label("엑셀 → Database 생성/덮어쓰기", EditorStyles.boldLabel);

			EditorGUILayout.Space(4);
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("엑셀 파일(.xlsx)", GUILayout.Width(120));
				EditorGUILayout.SelectableLabel(_excelPath ?? string.Empty, GUILayout.Height(18));
				if (GUILayout.Button("선택", GUILayout.Width(80)))
				{
					string p = EditorUtility.OpenFilePanel("데이터베이스 엑셀(.xlsx) 선택", Application.dataPath, "xlsx");
					if (!string.IsNullOrEmpty(p)) _excelPath = p;
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("DB 에셋 경로", GUILayout.Width(120));
				_databaseAssetPath = EditorGUILayout.TextField(_databaseAssetPath);
				if (GUILayout.Button("선택", GUILayout.Width(80)))
				{
					string p = EditorUtility.SaveFilePanelInProject(
						"Database 저장 경로",
						"DB",
						"asset",
						"저장할 경로를 선택하세요.",
						System.IO.Path.GetDirectoryName(_databaseAssetPath));
					if (!string.IsNullOrEmpty(p))
					{
						_databaseAssetPath = p.Replace('\\', '/');
					}
				}
			}

			EditorGUILayout.HelpBox(
				"시트별로 불러옵니다. 스프라이트/프리팹 등 에셋 참조는 건너뜁니다.",
				MessageType.Info);

			EditorGUILayout.Space(8);
			bool disabled = string.IsNullOrEmpty(_excelPath) || string.IsNullOrEmpty(_databaseAssetPath);
			using (new EditorGUI.DisabledScope(disabled))
			{
				foreach (var importer in ExcelImporterRegistry.Importers)
				{
					if (GUILayout.Button(importer.ButtonLabel, GUILayout.Height(28)))
					{
						RunImport(importer);
					}
					EditorGUILayout.Space(4);
				}
			}

			EditorGUILayout.Space(10);
			_debugMode = EditorGUILayout.ToggleLeft("개발자 모드 (숨김 기능 표시)", _debugMode);
			if (_debugMode)
			{
				EditorGUILayout.Space(6);
				EditorGUILayout.LabelField("엑셀 ← 현재 데이터 덮어쓰기", EditorStyles.boldLabel);
				using (new EditorGUI.DisabledScope(disabled))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button("stage_data 덮어쓰기", GUILayout.Height(24))) WriteStageSheetFromDb();
						if (GUILayout.Button("monster_data 덮어쓰기", GUILayout.Height(24))) WriteMonsterSheetFromDb();
					}
					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button("train_data 덮어쓰기", GUILayout.Height(24))) WriteTrainSheetFromDb();
						if (GUILayout.Button("range_train_data 덮어쓰기", GUILayout.Height(24))) WriteRangeTrainSheetFromDb();
					}
					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button("turret_train_data 덮어쓰기", GUILayout.Height(24))) WriteTurretTrainSheetFromDb();
						if (GUILayout.Button("train_upgrade_data 덮어쓰기", GUILayout.Height(24))) WriteTrainUpgradeSheetFromDb();
					}
					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button("upgrade_data 덮어쓰기", GUILayout.Height(24))) WriteUpgradeSheetFromDb();
					}
				}
			}
		}

		private void RunImport(IExcelSheetImporter importer)
		{
			try
			{
				EnsureFolderForAsset(_databaseAssetPath);

				bool existed = ExcelTemplate.SheetExists(_excelPath, importer.SheetName);
				ExcelTemplate.EnsureSheetWithHeaders(_excelPath, importer.SheetName, importer.Headers, null);
				bool hasData = ExcelTemplate.SheetHasData(_excelPath, importer.SheetName);

				if (!existed || !hasData)
				{
					string msg = !existed
						? $"엑셀에 '{importer.SheetName}' 시트가 없어 템플릿을 생성했습니다.\n지금 빈 시트를 기준으로 Database를 덮어쓸까요?"
						: $"'{importer.SheetName}' 시트가 비어있습니다.\n지금 빈 시트를 기준으로 Database를 덮어쓸까요?";
					bool proceed = EditorUtility.DisplayDialog("확인 필요", msg, "덮어쓰기", "취소");
					if (!proceed)
					{
						EditorUtility.DisplayDialog("중단", "사용자에 의해 작업이 취소되었습니다.", "확인");
						return;
					}
				}

				var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
				if (db == null)
				{
					db = ScriptableObject.CreateInstance<DB>();
					AssetDatabase.CreateAsset(db, _databaseAssetPath);
					// Ensure the newly created asset is persisted immediately
					EditorUtility.SetDirty(db);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}

				int count = importer.Import(db, _excelPath);

				// Persist DB changes reliably
				EditorUtility.SetDirty(db);
				AssetDatabase.SaveAssets();
				AssetDatabase.ImportAsset(_databaseAssetPath);
				AssetDatabase.Refresh();

				EditorUtility.DisplayDialog("완료",
					$"{importer.SheetName} 데이터 생성/갱신 완료\n행 수: {count}개\n저장 경로: {_databaseAssetPath}",
					"확인");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[DatabaseGeneratorWindow] {importer.SheetName} 데이터 생성 실패: {ex.Message}\n{ex.StackTrace}");
				EditorUtility.DisplayDialog("오류", $"{importer.SheetName} 데이터 생성 중 오류가 발생했습니다. 콘솔을 확인하세요.", "확인");
			}
		}

		private static void EnsureFolderForAsset(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath)) return;
			string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? "Assets";
			EnsureFolder(dir);
		}

		private static void EnsureFolder(string folderPath)
		{
			if (AssetDatabase.IsValidFolder(folderPath)) return;
			string[] parts = folderPath.Split('/');
			string current = parts[0];
			for (int i = 1; i < parts.Length; i++)
			{
				string next = current + "/" + parts[i];
				if (!AssetDatabase.IsValidFolder(next))
				{
					AssetDatabase.CreateFolder(current, parts[i]);
				}
				current = next;
			}
		}

		// ---------- DEBUG WRITE-BACK (initial subset; extended in importer phase) ----------
		private void WriteStageSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null || db.stageDataList == null)
			{
				EditorUtility.DisplayDialog("오류", "Database 또는 Stage 데이터가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<StageRow>(db.stageDataList.Count);
			foreach (var s in db.stageDataList)
			{
				rows.Add(new StageRow
				{
					id = s.Id,
					stageInspectionTime = s.StageInspectionTime != null ? string.Join(",", s.StageInspectionTime) : string.Empty,
					stageEndTime = s.StageEndTime
				});
			}
			ExcelTemplate.EnsureSheetWithHeaders(_excelPath, "stage_data", new[] { "id", "stage_inspection_time", "stage_end_time" });
			ExcelWriter.WriteToSheet(_excelPath, "stage_data", rows);
			EditorUtility.DisplayDialog("완료", "stage_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteMonsterSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null || db.monsterDataList == null)
			{
				EditorUtility.DisplayDialog("오류", "Database 또는 Monster 데이터가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<MonsterRow>(db.monsterDataList.Count);
			foreach (var m in db.monsterDataList)
			{
				rows.Add(new MonsterRow { id = m.Id, monsterName = m.MonsterName, description = m.Description });
			}
			ExcelTemplate.EnsureSheetWithHeaders(_excelPath, "monster_data", new[] { "id", "monster_name", "description" });
			ExcelWriter.WriteToSheet(_excelPath, "monster_data", rows);
			EditorUtility.DisplayDialog("완료", "monster_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteTrainSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null)
			{
				EditorUtility.DisplayDialog("오류", "Database가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<TrainRow>(db.trainDataList.Count);
			foreach (var t in db.trainDataList)
			{
				rows.Add(new TrainRow { id = t.Id, trainName = t.TrainName, description = t.Description, isMainTrain = t.IsMainTrain });
			}
			ExcelTemplate.EnsureSheetWithHeaders(_excelPath, "train_data", new[] { "id", "train_name", "description", "is_main_train" });
			ExcelWriter.WriteToSheet(_excelPath, "train_data", rows);
			EditorUtility.DisplayDialog("완료", "train_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteRangeTrainSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null)
			{
				EditorUtility.DisplayDialog("오류", "Database가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<TrainRow>(db.rangeTrainDataList.Count);
			foreach (var t in db.rangeTrainDataList)
			{
				rows.Add(new TrainRow { id = t.Id, trainName = t.TrainName, description = t.Description, isMainTrain = t.IsMainTrain });
			}
			ExcelTemplate.EnsureSheetWithHeaders(_excelPath, "range_train_data", new[] { "id", "train_name", "description", "is_main_train" });
			ExcelWriter.WriteToSheet(_excelPath, "range_train_data", rows);
			EditorUtility.DisplayDialog("완료", "range_train_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteTurretTrainSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null)
			{
				EditorUtility.DisplayDialog("오류", "Database가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<TrainRow>(db.turretTrainDataList.Count);
			foreach (var t in db.turretTrainDataList)
			{
				rows.Add(new TrainRow { id = t.Id, trainName = t.TrainName, description = t.Description, isMainTrain = t.IsMainTrain });
			}
			ExcelTemplate.EnsureSheetWithHeaders(_excelPath, "turret_train_data", new[] { "id", "train_name", "description", "is_main_train" });
			ExcelWriter.WriteToSheet(_excelPath, "turret_train_data", rows);
			EditorUtility.DisplayDialog("완료", "turret_train_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteTrainUpgradeSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null)
			{
				EditorUtility.DisplayDialog("오류", "Database가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<TrainUpgradeRow>(db.trainUpgradeDataList.Count);
			foreach (var u in db.trainUpgradeDataList)
			{
				rows.Add(new TrainUpgradeRow { upgradeName = u.UpgradeName, description = u.Description });
			}
			ExcelTemplate.EnsureSheetWithHeaders(_excelPath, "train_upgrade_data", new[] { "upgrade_name", "description" });
			ExcelWriter.WriteToSheet(_excelPath, "train_upgrade_data", rows);
			EditorUtility.DisplayDialog("완료", "train_upgrade_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteUpgradeSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null)
			{
				EditorUtility.DisplayDialog("오류", "Database가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<UpgradeRow>(db.upgradeDataList.Count);
			foreach (var u in db.upgradeDataList)
			{
				rows.Add(new UpgradeRow
				{
					id = u.Id,
					upgradeName = u.UpgradeName,
					description = u.Description,
					needMoney = u.NeedMoney,
					upgradeValue = u.UpgradeValue,
					maxUpgradeCount = u.MaxUpgradeCount
				});
			}
			ExcelTemplate.EnsureSheetWithHeaders(_excelPath, "upgrade_data", new[] { "id", "upgrade_name", "description", "need_money", "upgrade_value", "max_upgrade_count" });
			ExcelWriter.WriteToSheet(_excelPath, "upgrade_data", rows);
			EditorUtility.DisplayDialog("완료", "upgrade_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}


		// ---------- Write-back row adapters ----------
		private class MonsterRow : IExcelRow
		{
			public string id; public string monsterName; public string description;
			public void FromExcelRow(NPOI.SS.UserModel.IRow row) { }
			public void ToExcelRow(NPOI.SS.UserModel.IRow row) { Set(row, 0, id); Set(row, 1, monsterName); Set(row, 2, description); }
		}

		private class TrainRow : IExcelRow
		{
			public string id; public string trainName; public string description; public bool isMainTrain;
			public void FromExcelRow(NPOI.SS.UserModel.IRow row) { }
			public void ToExcelRow(NPOI.SS.UserModel.IRow row) { Set(row, 0, id); Set(row, 1, trainName); Set(row, 2, description); Set(row, 3, isMainTrain); }
		}

		private class TrainUpgradeRow : IExcelRow
		{
			public string upgradeName; public string description;
			public void FromExcelRow(NPOI.SS.UserModel.IRow row) { }
			public void ToExcelRow(NPOI.SS.UserModel.IRow row) { Set(row, 0, upgradeName); Set(row, 1, description); }
		}

		private class UpgradeRow : IExcelRow
		{
			public string id; public string upgradeName; public string description; public int needMoney; public float upgradeValue; public int maxUpgradeCount;
			public void FromExcelRow(NPOI.SS.UserModel.IRow row) { }
			public void ToExcelRow(NPOI.SS.UserModel.IRow row) { Set(row, 0, id); Set(row, 1, upgradeName); Set(row, 2, description); Set(row, 3, needMoney); Set(row, 4, upgradeValue); Set(row, 5, maxUpgradeCount); }
		}

		private class StageRow : IExcelRow
		{
			public string id; public string stageInspectionTime; public float stageEndTime;
			public void FromExcelRow(NPOI.SS.UserModel.IRow row) { }
			public void ToExcelRow(NPOI.SS.UserModel.IRow row) { Set(row, 0, id); Set(row, 1, stageInspectionTime); Set(row, 2, stageEndTime); }
		}

		private static void Set(NPOI.SS.UserModel.IRow row, int idx, object value)
		{
			var cell = row.GetCell(idx) ?? row.CreateCell(idx);
			if (value == null) { cell.SetCellValue(string.Empty); return; }
			switch (value)
			{
				case int i: cell.SetCellValue(i); break;
				case float f: cell.SetCellValue(f); break;
				case double d: cell.SetCellValue(d); break;
				case bool b: cell.SetCellValue(b); break;
				default: cell.SetCellValue(value.ToString()); break;
			}
		}
	}
}
#endif


