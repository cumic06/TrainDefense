#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TrainDefense.Editor.DataImport;
using TrainDefense.Editor.DataImport.Importers.Rows;
using TrainDefense.Game.Datas;

namespace TrainDefense.Editor
{
	public class DatabaseGeneratorWindow : EditorWindow
	{
		private string _excelDir = "Assets/02_Resources/Data";
		private string _databaseAssetPath = "Assets/Resources/Data/DB.asset";
		private bool _debugMode = false;
		private int _selectedTab = 0;
		private Vector2 _scrollPos;

		private static readonly string[] TabNames = { "전체", "몬스터", "기차", "업그레이드", "스테이지", "선택" };

		private static readonly Dictionary<string, int> SheetTabMap = new()
		{
			{ "monster_data", 1 },
			{ "train_data", 2 },
			{ "range_train_data", 2 },
			{ "turret_train_data", 2 },
			{ "active_skill_data", 2 },
			{ "passive_skill_data", 2 },
			{ "upgrade_data", 3 },
			{ "permanent_upgrade_data", 3 },
			{ "stage_data", 4 },
			{ "add_train_choice_data", 5 },
			{ "upgrade_train_choice_data", 5 },
		};

		private string ExcelAbsDir => ToAbsolutePath(_excelDir);

		private string GetExcelAbsPath(IExcelSheetImporter importer)
		{
			return Path.Combine(ExcelAbsDir, importer.ExcelFileName).Replace('\\', '/');
		}

		private string GetExcelAbsPath(string excelFileName)
		{
			return Path.Combine(ExcelAbsDir, excelFileName).Replace('\\', '/');
		}

		private static string ToAbsolutePath(string path)
		{
			if (string.IsNullOrEmpty(path)) return path;
			path = path.Replace('\\', '/');
			if (path.StartsWith("Assets/"))
			{
				string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/') ?? string.Empty;
				return $"{projectRoot}/{path}";
			}
			return path;
		}

		private static string ToProjectRelativePath(string absolutePath)
		{
			if (string.IsNullOrEmpty(absolutePath)) return absolutePath;
			string normalized = absolutePath.Replace('\\', '/');
			string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/') ?? string.Empty;
			if (!string.IsNullOrEmpty(projectRoot) && normalized.StartsWith(projectRoot + "/"))
			{
				return normalized[(projectRoot.Length + 1)..];
			}
			return normalized;
		}

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
				EditorGUILayout.LabelField("엑셀 폴더", GUILayout.Width(120));
				_excelDir = EditorGUILayout.TextField(_excelDir ?? string.Empty);
				if (GUILayout.Button("선택", GUILayout.Width(80)))
				{
					string abs = ExcelAbsDir;
					string initDir = !string.IsNullOrEmpty(abs) && Directory.Exists(abs)
						? abs
						: Path.Combine(Application.dataPath, "02_Resources/Data");
					string p = EditorUtility.OpenFolderPanel("엑셀 데이터 폴더 선택", initDir, "");
					if (!string.IsNullOrEmpty(p)) _excelDir = ToProjectRelativePath(p);
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
				"엑셀 폴더 내 파일별로 시트를 불러옵니다. 스프라이트/프리팹 등 에셋 참조는 건너뜁니다.",
				MessageType.Info);

			EditorGUILayout.Space(8);
			bool disabled = string.IsNullOrEmpty(_excelDir) || string.IsNullOrEmpty(_databaseAssetPath);
			using (new EditorGUI.DisabledScope(disabled))
			{
				// 모든 데이터 불러오기 버튼
				var originalColor = GUI.backgroundColor;
				GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
				if (GUILayout.Button("모든 데이터 불러오기", GUILayout.Height(32)))
				{
					RunAllImports();
				}
				GUI.backgroundColor = originalColor;
				EditorGUILayout.Space(8);
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

				// 탭 선택
				_selectedTab = GUILayout.Toolbar(_selectedTab, TabNames);
				EditorGUILayout.Space(4);

				// 탭에 해당하는 임포터만 표시
				_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
				foreach (var importer in ExcelImporterRegistry.Importers)
				{
					if (_selectedTab != 0)
					{
						if (!SheetTabMap.TryGetValue(importer.SheetName, out int tab) || tab != _selectedTab)
							continue;
					}

					if (GUILayout.Button(importer.ButtonLabel, GUILayout.Height(28)))
					{
						RunImport(importer);
					}
					EditorGUILayout.Space(4);
				}
				EditorGUILayout.EndScrollView();
			}

			EditorGUILayout.Space(10);
			_debugMode = EditorGUILayout.ToggleLeft("개발자 모드 (숨김 기능 표시)", _debugMode);
			if (_debugMode)
			{
				EditorGUILayout.Space(6);
				EditorGUILayout.LabelField("Localization 키 엑셀 적용", EditorStyles.boldLabel);
				using (new EditorGUI.DisabledScope(disabled))
				{
					var green = GUI.backgroundColor;
					GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
					if (GUILayout.Button("모든 엑셀에 Localization 키 적용", GUILayout.Height(28)))
						ApplyLocalizationKeysToAll();
					GUI.backgroundColor = green;
				}

				EditorGUILayout.Space(6);
				EditorGUILayout.LabelField("엑셀 ← 현재 데이터 덮어쓰기", EditorStyles.boldLabel);
				using (new EditorGUI.DisabledScope(disabled))
				{
					// 몬스터 탭
					if (_selectedTab == 0 || _selectedTab == 1)
					{
						if (GUILayout.Button("monster_data 덮어쓰기", GUILayout.Height(24))) WriteMonsterSheetFromDb();
					}
					// 기차 탭
					if (_selectedTab == 0 || _selectedTab == 2)
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							if (GUILayout.Button("train_data 덮어쓰기", GUILayout.Height(24))) WriteTrainSheetFromDb();
							if (GUILayout.Button("range_train_data 덮어쓰기", GUILayout.Height(24))) WriteRangeTrainSheetFromDb();
						}
						if (GUILayout.Button("turret_train_data 덮어쓰기", GUILayout.Height(24))) WriteTurretTrainSheetFromDb();
					}
					// 업그레이드 탭
					if (_selectedTab == 0 || _selectedTab == 3)
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							if (GUILayout.Button("upgrade_data 덮어쓰기", GUILayout.Height(24))) WriteUpgradeSheetFromDb();
						}
						using (new EditorGUILayout.HorizontalScope())
						{
						}
					}
					// 스테이지 탭
					if (_selectedTab == 0 || _selectedTab == 4)
					{
						if (GUILayout.Button("stage_data 덮어쓰기", GUILayout.Height(24))) WriteStageSheetFromDb();
					}
				}
			}
		}

		private void RunAllImports()
		{
			bool proceed = EditorUtility.DisplayDialog("확인 필요",
				$"모든 데이터를 불러오시겠습니까?\n\n총 {ExcelImporterRegistry.Importers.Count}개의 시트를 불러옵니다.",
				"불러오기", "취소");
			if (!proceed) return;

			EnsureFolderForAsset(_databaseAssetPath);

			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null)
			{
				db = ScriptableObject.CreateInstance<DB>();
				AssetDatabase.CreateAsset(db, _databaseAssetPath);
				EditorUtility.SetDirty(db);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			int successCount = 0;
			int failCount = 0;
			int totalRows = 0;
			var failedSheets = new System.Collections.Generic.List<string>();

			foreach (var importer in ExcelImporterRegistry.Importers)
			{
				try
				{
					string excelPath = GetExcelAbsPath(importer);
					bool existed = ExcelTemplate.SheetExists(excelPath, importer.SheetName);
					ExcelTemplate.EnsureSheetWithHeaders(excelPath, importer.SheetName, importer.Headers, null);
					bool hasData = ExcelTemplate.SheetHasData(excelPath, importer.SheetName);

					if (!existed || !hasData)
					{
						// 전체 불러오기 모드에서는 빈 시트를 건너뛰거나 자동으로 처리
						Debug.LogWarning($"[DatabaseGeneratorWindow] '{importer.SheetName}' 시트가 비어있거나 없습니다. 건너뜁니다.");
						continue;
					}

					int count = importer.Import(db, excelPath);
					totalRows += count;
					successCount++;
					Debug.Log($"[DatabaseGeneratorWindow] {importer.SheetName} 불러오기 완료 ({count}개 행)");
				}
				catch (System.Exception ex)
				{
					failCount++;
					failedSheets.Add(importer.SheetName);
					Debug.LogError($"[DatabaseGeneratorWindow] {importer.SheetName} 데이터 생성 실패: {ex.Message}\n{ex.StackTrace}");
				}
			}

			// Persist DB changes
			EditorUtility.SetDirty(db);
			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(_databaseAssetPath);
			AssetDatabase.Refresh();

			// 결과 메시지 표시
			string resultMsg = $"모든 데이터 불러오기 완료\n\n";
			resultMsg += $"성공: {successCount}개 시트\n";
			resultMsg += $"실패: {failCount}개 시트\n";
			resultMsg += $"총 행 수: {totalRows}개\n";
			if (failedSheets.Count > 0)
			{
				resultMsg += $"\n실패한 시트:\n{string.Join("\n", failedSheets)}";
			}
			resultMsg += $"\n저장 경로: {_databaseAssetPath}";

			EditorUtility.DisplayDialog("완료", resultMsg, "확인");
		}

		private void RunImport(IExcelSheetImporter importer)
		{
			try
			{
				EnsureFolderForAsset(_databaseAssetPath);

				string excelPath = GetExcelAbsPath(importer);
				bool existed = ExcelTemplate.SheetExists(excelPath, importer.SheetName);
				ExcelTemplate.EnsureSheetWithHeaders(excelPath, importer.SheetName, importer.Headers, null);
				bool hasData = ExcelTemplate.SheetHasData(excelPath, importer.SheetName);

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

				int count = importer.Import(db, excelPath);

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
				var spawnIds   = string.Join(";", System.Array.ConvertAll(s.SpawnDatas, d => d.MonsterId)) + ";";
				var spawnProbs = string.Join(";", System.Array.ConvertAll(s.SpawnDatas, d => d.Probability.ToString(System.Globalization.CultureInfo.InvariantCulture))) + ";";
				var spawnLevels = string.Join(";", System.Array.ConvertAll(s.SpawnDatas, d => d.SpawnLevel.ToString())) + ";";
				rows.Add(new StageRow
				{
					id = s.Id,
					baseInspectionTime = s.BaseInspectionTime,
					stationCount = s.StationCount,
					stageEndTime = s.StageEndTime,
					spawnInterval = s.SpawnInterval,
					spawnMonsters = spawnIds,
					spawnMonstersProbability = spawnProbs,
					spawnMonstersLevel = spawnLevels,
				});
			}
			string excelPath = GetExcelAbsPath("StageData.xlsx");
			ExcelTemplate.EnsureSheetWithHeaders(excelPath, "stage_data", new[] { "id", "base_inspection_time", "station_count", "stage_end_time", "spawn_interval", "spawn_monsters", "spawn_monsters_probability", "spawn_monsters_level" });
			ExcelWriter.WriteToSheet(excelPath, "stage_data", rows);
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
				rows.Add(new MonsterRow { id = m.Id, name = m.Name, description = m.Description });
			}
			string excelPath = GetExcelAbsPath("MonsterData.xlsx");
			ExcelTemplate.EnsureSheetWithHeaders(excelPath, "monster_data", new[] { "id", "monster_name", "description" });
			ExcelWriter.WriteToSheet(excelPath, "monster_data", rows);
			EditorUtility.DisplayDialog("완료", "monster_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteTrainSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null || db.trainDataList == null)
			{
				EditorUtility.DisplayDialog("오류", "Database 또는 Train 데이터가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<TrainRow>(db.trainDataList.Count);
			foreach (var t in db.trainDataList)
			{
				rows.Add(new TrainRow { id = t.Id, name = t.Name, description = t.Description, maxHp = t.TrainStatusData.MaxHp, isMainTrain = t.IsMainTrain });
			}
			string excelPath = GetExcelAbsPath("TrainData.xlsx");
			ExcelTemplate.EnsureSheetWithHeaders(excelPath, "train_data", new[] { "id", "train_name", "description", "max_hp", "is_main_train" });
			ExcelWriter.WriteToSheet(excelPath, "train_data", rows);
			EditorUtility.DisplayDialog("완료", "train_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteRangeTrainSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null || db.rangeTrainDataList == null)
			{
				EditorUtility.DisplayDialog("오류", "Database 또는 RangeTrain 데이터가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<RangeTrainRow>(db.rangeTrainDataList.Count);
			foreach (var t in db.rangeTrainDataList)
			{
				var s = t.RangeTrainStatus;
				rows.Add(new RangeTrainRow
				{
					id = t.Id, name = t.Name, description = t.Description,
					maxHp = t.TrainStatusData.MaxHp, isMainTrain = t.IsMainTrain,
					attackRange = s.AttackRange, attackArea = s.AttackArea,
					attackDamage = s.AttackDamage, attackCount = s.AttackCount,
					attackInterval = s.AttackInterval,
					criticalChance = s.CriticalChance, criticalDamage = s.CriticalDamage
				});
			}
			string excelPath = GetExcelAbsPath("TrainData.xlsx");
			ExcelTemplate.EnsureSheetWithHeaders(excelPath, "range_train_data", new[] { "id", "train_name", "description", "max_hp", "is_main_train", "attack_range", "attack_area", "attack_damage", "attack_count", "attack_interval", "critical_chance", "critical_damage" });
			ExcelWriter.WriteToSheet(excelPath, "range_train_data", rows);
			EditorUtility.DisplayDialog("완료", "range_train_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		private void WriteTurretTrainSheetFromDb()
		{
			var db = AssetDatabase.LoadAssetAtPath<DB>(_databaseAssetPath);
			if (db == null || db.turretTrainDataList == null)
			{
				EditorUtility.DisplayDialog("오류", "Database 또는 TurretTrain 데이터가 없습니다.", "확인");
				return;
			}
			var rows = new System.Collections.Generic.List<TurretTrainRow>(db.turretTrainDataList.Count);
			foreach (var t in db.turretTrainDataList)
			{
				var s = t.TurretTrainStatus;
				rows.Add(new TurretTrainRow
				{
					id = t.Id, name = t.Name, description = t.Description,
					maxHp = t.TrainStatusData.MaxHp, isMainTrain = t.IsMainTrain,
					attackRange = s.AttackRange, attackArea = s.AttackArea,
					attackDamage = s.AttackDamage, attackCount = s.AttackCount,
					attackInterval = s.AttackInterval, targetCount = s.TargetCount,
					criticalChance = s.CriticalChance, criticalDamage = s.CriticalDamage
				});
			}
			string excelPath = GetExcelAbsPath("TrainData.xlsx");
			ExcelTemplate.EnsureSheetWithHeaders(excelPath, "turret_train_data", new[] { "id", "train_name", "description", "max_hp", "is_main_train", "attack_range", "attack_area", "attack_damage", "attack_count", "attack_interval", "target_count", "critical_chance", "critical_damage" });
			ExcelWriter.WriteToSheet(excelPath, "turret_train_data", rows);
			EditorUtility.DisplayDialog("완료", "turret_train_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
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
				var row = new UpgradeRow
				{
					id = u.Id,
					name = u.Name,
					description = u.Description,
					needMoney = u.NeedMoney,
					upgradeValue = u.UpgradeValue,
					maxUpgradeCount = u.MaxUpgradeCount,
					iconId = u.IconId
				};

				row.upgradeType = u.UpgradeDataType.ToString();

				if (u.UpgradeDataType == UpgradeDataType.TrainUpgrade && u.Stats != null && u.Stats.Length > 0 && u.Stats[0] != null)
				{
					row.statType = u.Stats[0].Type.ToString();
				}
				else
				{
					row.statType = string.Empty;
				}

				rows.Add(row);
			}
			string excelPath = GetExcelAbsPath("UpgradeData.xlsx");
			ExcelTemplate.EnsureSheetWithHeaders(
				excelPath,
				"upgrade_data",
				new[] { "id", "upgrade_name", "description", "need_money", "upgrade_value", "max_upgrade_count", "icon_id", "upgrade_type", "stat_type" });
			ExcelWriter.WriteToSheet(excelPath, "upgrade_data", rows);
			EditorUtility.DisplayDialog("완료", "upgrade_data 시트를 현재 데이터로 덮어썼습니다.", "확인");
		}

		// ---------- LOCALIZATION KEY APPLICATION ----------

		private static readonly (string file, string sheet, string idCol, string nameCol, string descCol, string prefix)[] LocalizeSheetMappings =
		{
			("MonsterData.xlsx",    "monster_data",              "id", "monster_name", "description", "Monster"),
			("TrainData.xlsx",      "train_data",                "id", "train_name",   "description", "Train"),
			("TrainData.xlsx",      "range_train_data",          "id", "train_name",   "description", "Train"),
			("TrainData.xlsx",      "turret_train_data",         "id", "train_name",   "description", "Train"),
			("TrainSkillData.xlsx", "active_skill_data",         "id", "name",         "description", "Skill"),
			("TrainSkillData.xlsx", "passive_skill_data",        "id", "name",         "description", "Passive"),
			("UpgradeData.xlsx",    "upgrade_data",              "id", "upgrade_name", "description", "Upgrade"),
		};

		private void ApplyLocalizationKeysToAll()
		{
			bool ok = EditorUtility.DisplayDialog("확인",
				"각 엑셀 시트의 이름/설명 컬럼을 Localization 키 값으로 덮어씁니다.\n예) Monster_10001_Name, Monster_10001_Desc\n\n계속할까요?",
				"적용", "취소");
			if (!ok) return;

			int success = 0, fail = 0;
			foreach (var m in LocalizeSheetMappings)
			{
				string path = GetExcelAbsPath(m.file);
				if (!File.Exists(path))
				{
					Debug.LogWarning($"[LocalizeKey] 파일 없음: {path}");
					continue;
				}
				try
				{
					ApplyLocalizationKeysToSheet(path, m.sheet, m.idCol, m.nameCol, m.descCol, m.prefix);
					success++;
				}
				catch (System.Exception ex)
				{
					Debug.LogError($"[LocalizeKey] {m.file}/{m.sheet} 처리 실패: {ex.Message}");
					fail++;
				}
			}

			EditorUtility.DisplayDialog("완료", $"Localization 키 적용 완료\n성공: {success}개 시트 / 실패: {fail}개 시트", "확인");
		}

		private static void ApplyLocalizationKeysToSheet(string excelPath, string sheetName, string idCol, string nameCol, string descCol, string prefix)
		{
			IWorkbook wb;
			using (var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				wb = excelPath.EndsWith(".xlsx") ? (IWorkbook)new XSSFWorkbook(fs) : new HSSFWorkbook(fs);

			ISheet sheet = null;
			for (int s = 0; s < wb.NumberOfSheets; s++)
			{
				if (string.Equals(wb.GetSheetName(s), sheetName, System.StringComparison.OrdinalIgnoreCase))
				{
					sheet = wb.GetSheetAt(s);
					break;
				}
			}
			if (sheet == null)
			{
				Debug.LogWarning($"[LocalizeKey] 시트 없음: {sheetName} in {excelPath}");
				return;
			}

			var headers = new List<string>();
			var headerRow = sheet.GetRow(0);
			if (headerRow != null)
				for (int i = 0; i < headerRow.LastCellNum; i++)
					headers.Add(headerRow.GetCell(i)?.ToString() ?? string.Empty);
			var map = new HeaderMap(headers);

			int changed = 0;
			for (int i = 1; i <= sheet.LastRowNum; i++)
			{
				var row = sheet.GetRow(i);
				if (row == null) continue;
				string id = map.GetString(row, idCol)?.Trim();
				if (string.IsNullOrEmpty(id)) continue;

				map.SetCell(row, nameCol, $"{prefix}_{id}_Name");
				if (map.IndexOf(descCol) >= 0)
					map.SetCell(row, descCol, $"{prefix}_{id}_Desc");
				changed++;
			}

			using var ms = new System.IO.MemoryStream();
			wb.Write(ms);
			File.WriteAllBytes(excelPath, ms.ToArray());

			Debug.Log($"<color=cyan>[LocalizeKey] {sheetName}: {changed}행 키 적용 완료</color>");
		}

	}
}
#endif