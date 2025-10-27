using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Excel/CSV 파일에서 게임 데이터를 가져오는 도구
    /// </summary>
    public static class ExcelImporter
    {
        #region MenuItems
        
        [MenuItem("Tools/ExcelImporter/Open Excel Manager")]
        private static void OpenExcelManager()
        {
            TrainDefense.Game.Datas.ExcelImportView.ShowWindow();
        }
        
        #endregion

        #region Template Creation
        
        public static void CreateMonsterDataTemplate(string folderPath)
        {
            string template = "ID,Name,Description,MaxHp,Damage,MoveSpeed,AttackDelay,DropExpMin,DropExpMax,DropMoneyMin,DropMoneyMax,AttackRange\n" +
                             "monster_01,Goblin,작은 고블린,100,20,2.0,1.5,10,15,5,8,1.5\n" +
                             "monster_02,Orc,강한 오크,200,35,1.5,2.0,20,30,10,15,2.0";
            
            File.WriteAllText(Path.Combine(folderPath, "MonsterData_Template.csv"), template, System.Text.Encoding.UTF8);
        }
        
        public static void CreateTrainDataTemplate(string folderPath)
        {
            string template = "ID,Name,Description,MaxHp,IsMainTrain\n" +
                             "train_01,Basic Train,기본 기차,500,true";
            
            File.WriteAllText(Path.Combine(folderPath, "TrainData_Template.csv"), template, System.Text.Encoding.UTF8);
        }
        
        public static void CreateRangeTrainDataTemplate(string folderPath)
        {
            string template = "ID,Name,Description,MaxHp,AttackRange,AttackDamage,AttackCount,AttackInterval\n" +
                             "range_01,Range Train,원거리 기차,300,5.0,50,3,1.5";
            
            File.WriteAllText(Path.Combine(folderPath, "RangeTrainData_Template.csv"), template);
        }
        
        public static void CreateTurretTrainDataTemplate(string folderPath)
        {
            string template = "ID,Name,Description,MaxHp,AttackRange,AttackDamage,AttackCount,AttackDelay\n" +
                             "turret_01,Turret Train,터렛 기차,400,3.0,75,2,2.0";
            
            File.WriteAllText(Path.Combine(folderPath, "TurretTrainData_Template.csv"), template);
        }
        
        public static void CreateStageDataTemplate(string folderPath)
        {
            string template = "ID,StageEndTime,InspectionTimes\n" +
                             "stage_01,300.0,60.0;120.0;180.0;240.0\n" +
                             "stage_02,600.0,120.0;240.0;360.0;480.0";
            
            File.WriteAllText(Path.Combine(folderPath, "StageData_Template.csv"), template);
        }
        
        #endregion

        #region Data Export
        
        private static void ExportAllDataToExcel(string folderPath)
        {
            try
            {
                DB db = FindDBInstance();
                if (db == null) return;
                
                ExportMonsterData(Path.Combine(folderPath, "MonsterData.csv"), db);
                ExportTrainData(Path.Combine(folderPath, "TrainData.csv"), db);
                ExportRangeTrainData(Path.Combine(folderPath, "RangeTrainData.csv"), db);
                ExportTurretTrainData(Path.Combine(folderPath, "TurretTrainData.csv"), db);
                ExportStageData(Path.Combine(folderPath, "StageData.csv"), db);
                
                Debug.Log($"Successfully exported all data to {folderPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export all data: {e.Message}");
            }
        }
        
        public static void ExportMonsterData(string filePath)
        {
            DB db = FindDBInstance();
            if (db == null) return;
            ExportMonsterData(filePath, db);
        }
        
        private static void ExportMonsterData(string filePath, DB db)
        {
            try
            {
                List<string> lines = new List<string>();
                
                // 헤더
                lines.Add("ID,Name,Description,MaxHp,Damage,MoveSpeed,AttackDelay,DropExpMin,DropExpMax,DropMoneyMin,DropMoneyMax,AttackRange");
                
                // 데이터
                foreach (var monster in db.monsterDataList)
                {
                    var status = monster.MonsterStatusData;
                    string line = $"{monster.Id},{monster.MonsterName},{monster.Description}," +
                                 $"{status.MaxHp},{status.Damage},{status.MoveSpeed},{status.AttackDelay}," +
                                 $"{status.DropExpMin},{status.DropExpMax},{status.DropMoneyMin},{status.DropMoneyMax},{status.AttackRange}";
                    lines.Add(line);
                }
                
                File.WriteAllLines(filePath, lines);
                Debug.Log($"Successfully exported {db.monsterDataList.Count} monster data entries to {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export monster data: {e.Message}");
            }
        }
        
        public static void ExportTrainData(string filePath)
        {
            DB db = FindDBInstance();
            if (db == null) return;
            ExportTrainData(filePath, db);
        }
        
        private static void ExportTrainData(string filePath, DB db)
        {
            try
            {
                List<string> lines = new List<string>();
                
                // 헤더 (새로운 컬럼 구조에 맞게)
                lines.Add("ID,Name,Description,MaxHp,TrainType");
                
                // 모든 TrainData 타입의 데이터 내보내기
                var allTrainData = db.GetAllTrainData();
                foreach (var train in allTrainData)
                {
                    var status = train.TrainStatusData;
                    string trainType = train.GetType().Name.Replace("TrainData", "");
                    string line = $"{train.Id},{train.TrainName},{train.Description}," +
                                 $"{status.MaxHp},{trainType}";
                    lines.Add(line);
                }
                
                File.WriteAllLines(filePath, lines);
                Debug.Log($"Successfully exported {allTrainData.Count} train data entries to {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export train data: {e.Message}");
            }
        }
        
        public static void ExportRangeTrainData(string filePath)
        {
            DB db = FindDBInstance();
            if (db == null) return;
            ExportRangeTrainData(filePath, db);
        }
        
        private static void ExportRangeTrainData(string filePath, DB db)
        {
            try
            {
                List<string> lines = new List<string>();
                
                // 헤더 (새로운 컬럼 구조에 맞게)
                lines.Add("ID,Name,Description,MaxHp,AttackRange,AttackDamage,AttackCount,AttackInterval");
                
                // RangeTrainData 내보내기
                foreach (var train in db.rangeTrainDataList)
                {
                    var status = train.TrainStatusData;
                    var rangeStatus = train.RangeTrainStatus;
                    string line = $"{train.Id},{train.TrainName},{train.Description}," +
                                 $"{status.MaxHp},{rangeStatus.AttackRange},{rangeStatus.AttackDamage}," +
                                 $"{rangeStatus.AttackCount},{rangeStatus.AttackInterval}";
                    lines.Add(line);
                }
                
                File.WriteAllLines(filePath, lines);
                Debug.Log($"Successfully exported {db.rangeTrainDataList.Count} range train data entries to {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export range train data: {e.Message}");
            }
        }
        
        public static void ExportTurretTrainData(string filePath)
        {
            DB db = FindDBInstance();
            if (db == null) return;
            ExportTurretTrainData(filePath, db);
        }
        
        private static void ExportTurretTrainData(string filePath, DB db)
        {
            try
            {
                List<string> lines = new List<string>();
                
                // 헤더 (새로운 컬럼 구조에 맞게)
                lines.Add("ID,Name,Description,MaxHp,AttackRange,AttackDamage,AttackCount,AttackDelay");
                
                // TurretTrainData 내보내기
                foreach (var train in db.turretTrainDataList)
                {
                    var status = train.TrainStatusData;
                    var turretStatus = train.TurretTrainStatus;
                    string line = $"{train.Id},{train.TrainName},{train.Description}," +
                                 $"{status.MaxHp},{turretStatus.AttackRange},{turretStatus.AttackDamage}," +
                                 $"{turretStatus.AttackCount},{turretStatus.AttackDelay}";
                    lines.Add(line);
                }
                
                File.WriteAllLines(filePath, lines);
                Debug.Log($"Successfully exported {db.turretTrainDataList.Count} turret train data entries to {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export turret train data: {e.Message}");
            }
        }
        
        public static void ExportStageData(string filePath)
        {
            DB db = FindDBInstance();
            if (db == null) return;
            ExportStageData(filePath, db);
        }
        
        private static void ExportStageData(string filePath, DB db)
        {
            try
            {
                List<string> lines = new List<string>();
                
                // 헤더
                lines.Add("ID,StageEndTime,InspectionTimes");
                
                // 데이터
                foreach (var stage in db.stageDataList)
                {
                    string inspectionTimes = string.Join(";", stage.StageInspectionTime);
                    string line = $"{stage.Id},{stage.StageEndTime},{inspectionTimes}";
                    lines.Add(line);
                }
                
                File.WriteAllLines(filePath, lines);
                Debug.Log($"Successfully exported {db.stageDataList.Count} stage data entries to {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export stage data: {e.Message}");
            }
        }
        
        #endregion

        #region Train Data Type Detection
        
        /// <summary>
        /// 시트명을 기반으로 TrainData 타입을 결정
        /// </summary>
        public static Type GetTrainDataTypeFromSheetName(string sheetName)
        {
            string lowerSheetName = sheetName.ToLower();
            
            if (lowerSheetName.Contains("rangetrain") || lowerSheetName.Contains("range_train"))
            {
                return typeof(RangeTrainData);
            }
            else if (lowerSheetName.Contains("turrettrain") || lowerSheetName.Contains("turret_train"))
            {
                return typeof(TurretTrainData);
            }
            else if (lowerSheetName.Contains("train"))
            {
                return typeof(TrainData);
            }
            else
            {
                return typeof(TrainData); // 기본값
            }
        }
        
        /// <summary>
        /// 파일명을 기반으로 TrainData 타입을 결정
        /// </summary>
        public static Type GetTrainDataTypeFromFileName(string fileName)
        {
            string lowerFileName = fileName.ToLower();
            
            if (lowerFileName.Contains("rangetrain") || lowerFileName.Contains("range_train"))
            {
                return typeof(RangeTrainData);
            }
            else if (lowerFileName.Contains("turrettrain") || lowerFileName.Contains("turret_train"))
            {
                return typeof(TurretTrainData);
            }
            else
            {
                return typeof(TrainData);
            }
        }
        
        /// <summary>
        /// TrainData 객체를 생성하고 기본 필드 설정
        /// </summary>
        private static TrainData CreateTrainDataInstance(Type trainDataType, string[] values)
        {
            TrainData trainData = (TrainData)Activator.CreateInstance(trainDataType);
            
            // 기본 TrainData 필드 설정
            var idField = typeof(TrainData).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
            var nameField = typeof(TrainData).GetField("trainName", BindingFlags.NonPublic | BindingFlags.Instance);
            var descField = typeof(TrainData).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance);
            var statusField = typeof(TrainData).GetField("trainStatusData", BindingFlags.NonPublic | BindingFlags.Instance);
            var isMainField = typeof(TrainData).GetField("isMainTrain", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (idField != null) idField.SetValue(trainData, values[0]);
            if (nameField != null) nameField.SetValue(trainData, values[1]);
            if (descField != null) descField.SetValue(trainData, values[2]);
            
            // IsMainTrain은 기본값으로 false 설정 (새로운 컬럼 구조에는 없음)
            if (isMainField != null) isMainField.SetValue(trainData, false);
            
            // TrainStatusData 설정
            TrainStatusData statusData = new TrainStatusData();
            if (int.TryParse(values[3], out int maxHp)) statusData.MaxHp = maxHp;
            if (statusField != null) statusField.SetValue(trainData, statusData);
            
            return trainData;
        }
        
        /// <summary>
        /// RangeTrainData의 추가 필드 설정
        /// </summary>
        private static void SetRangeTrainDataFields(RangeTrainData rangeTrainData, string[] values)
        {
            if (values.Length < 8) return; // 최소 컬럼 수 확인 (ID, Name, Description, MaxHp, AttackRange, AttackDamage, AttackCount, AttackInterval)
            
            var statusField = typeof(RangeTrainData).GetField("rangeTrainStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (statusField != null)
            {
                RangeAttackTrainStatus rangeStatus = new RangeAttackTrainStatus();
                
                // 새로운 컬럼 순서: ID, Name, Description, MaxHp, AttackRange, AttackDamage, AttackCount, AttackInterval
                if (float.TryParse(values[4], out float attackRange)) rangeStatus.AttackRange = attackRange;
                if (int.TryParse(values[5], out int attackDamage)) rangeStatus.AttackDamage = attackDamage;
                if (int.TryParse(values[6], out int attackCount)) rangeStatus.AttackCount = attackCount;
                if (float.TryParse(values[7], out float attackInterval)) rangeStatus.AttackInterval = attackInterval;
                
                statusField.SetValue(rangeTrainData, rangeStatus);
            }
        }
        
        /// <summary>
        /// TurretTrainData의 추가 필드 설정
        /// </summary>
        private static void SetTurretTrainDataFields(TurretTrainData turretTrainData, string[] values)
        {
            if (values.Length < 8) return; // 최소 컬럼 수 확인 (ID, Name, Description, MaxHp, AttackRange, AttackDamage, AttackCount, AttackDelay)
            
            var statusField = typeof(TurretTrainData).GetField("turretTrainStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (statusField != null)
            {
                TurretTrainStatus turretStatus = new TurretTrainStatus();
                
                // 새로운 컬럼 순서: ID, Name, Description, MaxHp, AttackRange, AttackDamage, AttackCount, AttackDelay
                if (float.TryParse(values[4], out float attackRange)) turretStatus.AttackRange = attackRange;
                if (int.TryParse(values[5], out int attackDamage)) turretStatus.AttackDamage = attackDamage;
                if (int.TryParse(values[6], out int attackCount)) turretStatus.AttackCount = attackCount;
                if (float.TryParse(values[7], out float attackDelay)) turretStatus.AttackDelay = attackDelay;
                
                statusField.SetValue(turretTrainData, turretStatus);
            }
        }
        
        /// <summary>
        /// TrainData 객체를 생성하고 기본 필드 설정 (헤더 기반)
        /// </summary>
        private static TrainData CreateTrainDataInstanceWithHeaders(Type trainDataType, string[] values, Dictionary<string, int> columnIndexMap)
        {
            TrainData trainData = (TrainData)Activator.CreateInstance(trainDataType);
            
            // 기본 TrainData 필드 설정
            var idField = typeof(TrainData).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
            var nameField = typeof(TrainData).GetField("trainName", BindingFlags.NonPublic | BindingFlags.Instance);
            var descField = typeof(TrainData).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance);
            var statusField = typeof(TrainData).GetField("trainStatusData", BindingFlags.NonPublic | BindingFlags.Instance);
            var isMainField = typeof(TrainData).GetField("isMainTrain", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (idField != null) idField.SetValue(trainData, GetValue(values, columnIndexMap, "ID"));
            if (nameField != null) nameField.SetValue(trainData, GetValue(values, columnIndexMap, "NAME"));
            if (descField != null) descField.SetValue(trainData, GetValue(values, columnIndexMap, "DESCRIPTION"));
            
            // IsMainTrain은 기본값으로 false 설정 (새로운 컬럼 구조에는 없음)
            if (isMainField != null) isMainField.SetValue(trainData, false);
            
            // TrainStatusData 설정
            TrainStatusData statusData = new TrainStatusData();
            if (int.TryParse(GetValue(values, columnIndexMap, "MAXHP"), out int maxHp)) statusData.MaxHp = maxHp;
            if (statusField != null) statusField.SetValue(trainData, statusData);
            
            return trainData;
        }
        
        /// <summary>
        /// RangeTrainData의 추가 필드 설정 (헤더 기반)
        /// </summary>
        private static void SetRangeTrainDataFieldsWithHeaders(RangeTrainData rangeTrainData, string[] values, Dictionary<string, int> columnIndexMap)
        {
            var statusField = typeof(RangeTrainData).GetField("rangeTrainStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (statusField != null)
            {
                RangeAttackTrainStatus rangeStatus = new RangeAttackTrainStatus();
                
                if (float.TryParse(GetValue(values, columnIndexMap, "ATTACKRANGE"), out float attackRange)) rangeStatus.AttackRange = attackRange;
                if (int.TryParse(GetValue(values, columnIndexMap, "ATTACKDAMAGE"), out int attackDamage)) rangeStatus.AttackDamage = attackDamage;
                if (int.TryParse(GetValue(values, columnIndexMap, "ATTACKCOUNT"), out int attackCount)) rangeStatus.AttackCount = attackCount;
                if (float.TryParse(GetValue(values, columnIndexMap, "ATTACKINTERVAL"), out float attackInterval)) rangeStatus.AttackInterval = attackInterval;
                
                statusField.SetValue(rangeTrainData, rangeStatus);
            }
        }
        
        /// <summary>
        /// TurretTrainData의 추가 필드 설정 (헤더 기반)
        /// </summary>
        private static void SetTurretTrainDataFieldsWithHeaders(TurretTrainData turretTrainData, string[] values, Dictionary<string, int> columnIndexMap)
        {
            var statusField = typeof(TurretTrainData).GetField("turretTrainStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (statusField != null)
            {
                TurretTrainStatus turretStatus = new TurretTrainStatus();
                
                if (float.TryParse(GetValue(values, columnIndexMap, "ATTACKRANGE"), out float attackRange)) turretStatus.AttackRange = attackRange;
                if (int.TryParse(GetValue(values, columnIndexMap, "ATTACKDAMAGE"), out int attackDamage)) turretStatus.AttackDamage = attackDamage;
                if (int.TryParse(GetValue(values, columnIndexMap, "ATTACKCOUNT"), out int attackCount)) turretStatus.AttackCount = attackCount;
                if (float.TryParse(GetValue(values, columnIndexMap, "ATTACKDELAY"), out float attackDelay)) turretStatus.AttackDelay = attackDelay;
                
                statusField.SetValue(turretTrainData, turretStatus);
            }
        }
        
        #endregion

        #region Data Import
        
        public static void ImportMonsterData(string filePath)
        {
            try
            {
                // 파일 접근 가능 여부 확인
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }

                // 파일이 다른 프로그램에서 사용 중인지 확인
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // 파일 접근 가능
                    }
                }
                catch (System.IO.IOException ex)
                {
                    Debug.LogError($"File is being used by another program. Please close Excel or any other program that might be using the file: {filePath}\nError: {ex.Message}");
                    return;
                }

                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length < 2)
                {
                    Debug.LogError("Excel file must have at least a header row and one data row");
                    return;
                }
                
                // 헤더 파싱
                string[] headers = ParseCSVLine(lines[0]);
                Dictionary<string, int> columnIndexMap = CreateColumnIndexMap(headers);
                
                Debug.Log($"Headers found: {string.Join(", ", headers)}");
                
                // 필수 컬럼 확인
                if (!ValidateMonsterHeaders(columnIndexMap))
                {
                    Debug.LogError("Missing required headers in the CSV file");
                    return;
                }
                
                // DB 인스턴스 찾기
                DB db = FindDBInstance();
                if (db == null) return;
                
                // 기존 데이터 클리어
                db.monsterDataList.Clear();
                
                // 데이터 파싱
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length < columnIndexMap.Count)
                    {
                        Debug.LogWarning($"Row {i} has fewer columns than expected. Skipping.");
                        continue;
                    }
                    
                    // MonsterData 클래스 생성
                    MonsterData monsterData = new MonsterData();
                    
                    // 리플렉션을 사용하여 private 필드 설정
                    var idField = typeof(MonsterData).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
                    var nameField = typeof(MonsterData).GetField("monsterName", BindingFlags.NonPublic | BindingFlags.Instance);
                    var descField = typeof(MonsterData).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance);
                    var statusField = typeof(MonsterData).GetField("monsterStatusData", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (idField != null) idField.SetValue(monsterData, GetValue(values, columnIndexMap, "ID"));
                    if (nameField != null) nameField.SetValue(monsterData, GetValue(values, columnIndexMap, "NAME"));
                    if (descField != null) descField.SetValue(monsterData, GetValue(values, columnIndexMap, "DESCRIPTION"));
                    
                    // MonsterStatusInfo 구조체 설정
                    MonsterStatusInfo statusInfo = new MonsterStatusInfo();
                    if (int.TryParse(GetValue(values, columnIndexMap, "MAXHP"), out int maxHp)) statusInfo.MaxHp = maxHp;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DAMAGE"), out int damage)) statusInfo.Damage = damage;
                    if (float.TryParse(GetValue(values, columnIndexMap, "MOVESPEED"), out float moveSpeed)) statusInfo.MoveSpeed = moveSpeed;
                    if (float.TryParse(GetValue(values, columnIndexMap, "ATTACKDELAY"), out float attackDelay)) statusInfo.AttackDelay = attackDelay;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DROPEXPMIN"), out int dropExpMin)) statusInfo.DropExpMin = dropExpMin;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DROPEXPMAX"), out int dropExpMax)) statusInfo.DropExpMax = dropExpMax;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DROPMONEYMIN"), out int dropMoneyMin)) statusInfo.DropMoneyMin = dropMoneyMin;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DROPMONEYMAX"), out int dropMoneyMax)) statusInfo.DropMoneyMax = dropMoneyMax;
                    if (float.TryParse(GetValue(values, columnIndexMap, "ATTACKRANGE"), out float attackRange)) statusInfo.AttackRange = attackRange;
                    
                    if (statusField != null) statusField.SetValue(monsterData, statusInfo);
                    
                    // DB에 추가
                    db.monsterDataList.Add(monsterData);
                    
                    Debug.Log($"Imported monster data: {GetValue(values, columnIndexMap, "ID")} - {GetValue(values, columnIndexMap, "NAME")}");
                }
                
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully imported {lines.Length - 1} monster data entries from Excel");
                Debug.Log($"DB now contains {db.monsterDataList.Count} monster data entries");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import monster data: {e.Message}");
            }
        }
        
        public static void ImportTrainData(string filePath)
        {
            try
            {
                // 파일 접근 가능 여부 확인
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }

                // 파일이 다른 프로그램에서 사용 중인지 확인
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // 파일 접근 가능
                    }
                }
                catch (System.IO.IOException ex)
                {
                    Debug.LogError($"File is being used by another program. Please close Excel or any other program that might be using the file: {filePath}\nError: {ex.Message}");
                    return;
                }

                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length < 2)
                {
                    Debug.LogError("Excel file must have at least a header row and one data row");
                    return;
                }
                
                // 헤더 파싱
                string[] headers = ParseCSVLine(lines[0]);
                Dictionary<string, int> columnIndexMap = CreateColumnIndexMap(headers);
                
                Debug.Log($"Headers found: {string.Join(", ", headers)}");
                
                DB db = FindDBInstance();
                if (db == null) return;
                
                // 파일명에서 TrainData 타입 결정
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                Type trainDataType = GetTrainDataTypeFromFileName(fileName);
                
                Debug.Log($"Detected train data type: {trainDataType.Name} from file: {fileName}");
                
                // 타입별 리스트 클리어
                if (trainDataType == typeof(RangeTrainData))
                {
                    db.rangeTrainDataList.Clear();
                    if (!ValidateRangeTrainHeaders(columnIndexMap))
                    {
                        Debug.LogError("Missing required headers for RangeTrainData");
                        return;
                    }
                }
                else if (trainDataType == typeof(TurretTrainData))
                {
                    db.turretTrainDataList.Clear();
                    if (!ValidateTurretTrainHeaders(columnIndexMap))
                    {
                        Debug.LogError("Missing required headers for TurretTrainData");
                        return;
                    }
                }
                else
                {
                    db.trainDataList.Clear();
                    if (!ValidateTrainHeaders(columnIndexMap))
                    {
                        Debug.LogError("Missing required headers for TrainData");
                        return;
                    }
                }
                
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length < columnIndexMap.Count)
                    {
                        Debug.LogWarning($"Row {i} has fewer columns than expected. Skipping.");
                        continue;
                    }
                    
                    // 적절한 TrainData 타입으로 객체 생성
                    TrainData trainData = CreateTrainDataInstanceWithHeaders(trainDataType, values, columnIndexMap);
                    
                    // 타입별 추가 필드 설정
                    if (trainData is RangeTrainData rangeTrainData)
                    {
                        SetRangeTrainDataFieldsWithHeaders(rangeTrainData, values, columnIndexMap);
                        db.rangeTrainDataList.Add(rangeTrainData);
                    }
                    else if (trainData is TurretTrainData turretTrainData)
                    {
                        SetTurretTrainDataFieldsWithHeaders(turretTrainData, values, columnIndexMap);
                        db.turretTrainDataList.Add(turretTrainData);
                    }
                    else
                    {
                        db.trainDataList.Add(trainData);
                    }
                    
                    Debug.Log($"Imported {trainDataType.Name}: {GetValue(values, columnIndexMap, "ID")} - {GetValue(values, columnIndexMap, "NAME")}");
                }
                
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully imported {lines.Length - 1} {trainDataType.Name} entries from Excel");
                Debug.Log($"DB now contains - Basic: {db.trainDataList.Count}, Range: {db.rangeTrainDataList.Count}, Turret: {db.turretTrainDataList.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import train data: {e.Message}");
            }
        }
        
        public static void ImportStageData(string filePath)
        {
            try
            {
                // 파일 접근 가능 여부 확인
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }

                // 파일이 다른 프로그램에서 사용 중인지 확인
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // 파일 접근 가능
                    }
                }
                catch (System.IO.IOException ex)
                {
                    Debug.LogError($"File is being used by another program. Please close Excel or any other program that might be using the file: {filePath}\nError: {ex.Message}");
                    return;
                }

                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length < 2)
                {
                    Debug.LogError("Excel file must have at least a header row and one data row");
                    return;
                }
                
                // 헤더 파싱
                string[] headers = ParseCSVLine(lines[0]);
                Dictionary<string, int> columnIndexMap = CreateColumnIndexMap(headers);
                
                Debug.Log($"Headers found: {string.Join(", ", headers)}");
                
                // 필수 컬럼 확인
                if (!ValidateStageHeaders(columnIndexMap))
                {
                    Debug.LogError("Missing required headers in the CSV file");
                    return;
                }
                
                DB db = FindDBInstance();
                if (db == null) return;
                
                db.stageDataList.Clear();
                
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length < columnIndexMap.Count)
                    {
                        Debug.LogWarning($"Row {i} has fewer columns than expected. Skipping.");
                        continue;
                    }
                    
                    // StageData 클래스 생성
                    StageData stageData = new StageData();
                    
                    // 리플렉션을 사용하여 private 필드 설정
                    var idField = typeof(StageData).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
                    var endTimeField = typeof(StageData).GetField("stageEndTime", BindingFlags.NonPublic | BindingFlags.Instance);
                    var inspectionTimeField = typeof(StageData).GetField("stageInspectionTime", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (idField != null) idField.SetValue(stageData, GetValue(values, columnIndexMap, "ID"));
                    if (endTimeField != null && float.TryParse(GetValue(values, columnIndexMap, "STAGEENDTIME"), out float endTime)) 
                        endTimeField.SetValue(stageData, endTime);
                    
                    // Inspection Times 파싱 (세미콜론으로 구분된 값들)
                    if (inspectionTimeField != null)
                    {
                        string inspectionTimesValue = GetValue(values, columnIndexMap, "INSPECTIONTIMES");
                        if (!string.IsNullOrEmpty(inspectionTimesValue))
                        {
                            string[] inspectionTimes = inspectionTimesValue.Split(';');
                            float[] times = new float[inspectionTimes.Length];
                            for (int j = 0; j < inspectionTimes.Length; j++)
                            {
                                if (float.TryParse(inspectionTimes[j].Trim(), out float time))
                                {
                                    times[j] = time;
                                }
                            }
                            inspectionTimeField.SetValue(stageData, times);
                        }
                    }
                    
                    // DB에 추가
                    db.stageDataList.Add(stageData);
                    
                    Debug.Log($"Imported stage data: {GetValue(values, columnIndexMap, "ID")}");
                }
                
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully imported {lines.Length - 1} stage data entries from Excel");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import stage data: {e.Message}");
            }
        }
        
        public static void ImportAllData(string folderPath, DB targetDB = null)
        {
            try
            {
                DB db = targetDB ?? FindDBInstance();
                if (db == null) return;
                
                // 폴더 내 모든 CSV 파일 찾기
                string[] csvFiles = Directory.GetFiles(folderPath, "*.csv");
                
                foreach (string file in csvFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                    
                    if (fileName.Contains("monster"))
                    {
                        ImportMonsterDataToDB(file, db, true);
                    }
                    else if (fileName.Contains("rangetrain") || fileName.Contains("range_train"))
                    {
                        ImportTrainDataToDB(file, db, true);
                    }
                    else if (fileName.Contains("turrettrain") || fileName.Contains("turret_train"))
                    {
                        ImportTrainDataToDB(file, db, true);
                    }
                    else if (fileName.Contains("train"))
                    {
                        ImportTrainDataToDB(file, db, true);
                    }
                    else if (fileName.Contains("stage"))
                    {
                        ImportStageDataToDB(file, db, true);
                    }
                    else if (fileName.Contains("upgrade"))
                    {
                        // ImportUpgradeData(file);
                    }
                    else if (fileName.Contains("choice"))
                    {
                        // ImportChoiceData(file);
                    }
                }
                
                Debug.Log($"Successfully imported all data from {csvFiles.Length} Excel files");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import all data: {e.Message}");
            }
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// 헤더를 정규화 (대소문자 구별 없이, 언더바 제거)
        /// </summary>
        private static string NormalizeHeader(string header)
        {
            return header.Trim().Replace("_", "").Replace("-", "").ToUpper();
        }
        
        /// <summary>
        /// 헤더 배열로부터 컬럼 인덱스 맵 생성
        /// </summary>
        private static Dictionary<string, int> CreateColumnIndexMap(string[] headers)
        {
            Dictionary<string, int> map = new Dictionary<string, int>();
            for (int i = 0; i < headers.Length; i++)
            {
                string normalizedHeader = NormalizeHeader(headers[i]);
                map[normalizedHeader] = i;
            }
            return map;
        }
        
        /// <summary>
        /// 컬럼 인덱스 맵에서 값을 가져오기
        /// </summary>
        private static string GetValue(string[] values, Dictionary<string, int> columnIndexMap, string columnName)
        {
            string normalizedColumnName = NormalizeHeader(columnName);
            if (columnIndexMap.ContainsKey(normalizedColumnName))
            {
                int index = columnIndexMap[normalizedColumnName];
                if (index >= 0 && index < values.Length)
                {
                    return values[index];
                }
            }
            return "";
        }
        
        /// <summary>
        /// MonsterData 헤더 검증
        /// </summary>
        private static bool ValidateMonsterHeaders(Dictionary<string, int> columnIndexMap)
        {
            string[] requiredHeaders = { "ID", "NAME", "DESCRIPTION", "MAXHP", "DAMAGE", "MOVESPEED", "ATTACKDELAY", 
                                         "DROPEXPMIN", "DROPEXPMAX", "DROPMONEYMIN", "DROPMONEYMAX", "ATTACKRANGE" };
            
            foreach (string header in requiredHeaders)
            {
                string normalizedHeader = NormalizeHeader(header);
                if (!columnIndexMap.ContainsKey(normalizedHeader))
                {
                    Debug.LogError($"Missing required header: {header}");
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// TrainData 헤더 검증
        /// </summary>
        private static bool ValidateTrainHeaders(Dictionary<string, int> columnIndexMap)
        {
            string[] requiredHeaders = { "ID", "NAME", "DESCRIPTION", "MAXHP" };
            
            foreach (string header in requiredHeaders)
            {
                string normalizedHeader = NormalizeHeader(header);
                if (!columnIndexMap.ContainsKey(normalizedHeader))
                {
                    Debug.LogError($"Missing required header: {header}");
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// RangeTrainData 헤더 검증
        /// </summary>
        private static bool ValidateRangeTrainHeaders(Dictionary<string, int> columnIndexMap)
        {
            string[] requiredHeaders = { "ID", "NAME", "DESCRIPTION", "MAXHP", "ATTACKRANGE", "ATTACKDAMAGE", "ATTACKCOUNT", "ATTACKINTERVAL" };
            
            foreach (string header in requiredHeaders)
            {
                string normalizedHeader = NormalizeHeader(header);
                if (!columnIndexMap.ContainsKey(normalizedHeader))
                {
                    Debug.LogError($"Missing required header: {header}");
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// TurretTrainData 헤더 검증
        /// </summary>
        private static bool ValidateTurretTrainHeaders(Dictionary<string, int> columnIndexMap)
        {
            string[] requiredHeaders = { "ID", "NAME", "DESCRIPTION", "MAXHP", "ATTACKRANGE", "ATTACKDAMAGE", "ATTACKCOUNT", "ATTACKDELAY" };
            
            foreach (string header in requiredHeaders)
            {
                string normalizedHeader = NormalizeHeader(header);
                if (!columnIndexMap.ContainsKey(normalizedHeader))
                {
                    Debug.LogError($"Missing required header: {header}");
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// StageData 헤더 검증
        /// </summary>
        private static bool ValidateStageHeaders(Dictionary<string, int> columnIndexMap)
        {
            string[] requiredHeaders = { "ID", "STAGEENDTIME", "INSPECTIONTIMES" };
            
            foreach (string header in requiredHeaders)
            {
                string normalizedHeader = NormalizeHeader(header);
                if (!columnIndexMap.ContainsKey(normalizedHeader))
                {
                    Debug.LogError($"Missing required header: {header}");
                    return false;
                }
            }
            return true;
        }
        
        private static string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.Trim());
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField.Trim());
            return result.ToArray();
        }
        
        private static DB FindDBInstance()
        {
            // Resources 폴더에서 DB 찾기
            DB db = Resources.Load<DB>("DB");
            if (db == null)
            {
                // Assets 폴더에서 DB 찾기
                string[] guids = AssetDatabase.FindAssets("t:DB");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    db = AssetDatabase.LoadAssetAtPath<DB>(path);
                }
            }
            
            if (db == null)
            {
                Debug.LogError("DB asset not found. Please create a DB asset first.");
                Debug.LogError("You can create a DB asset by: Right-click in Project -> Create -> Data -> DB");
            }
            else
            {
                Debug.Log($"Found DB asset: {AssetDatabase.GetAssetPath(db)}");
            }
            
            return db;
        }
        
        #endregion
        
        #region Import to Specific DB
        
        public static void ImportMonsterDataToDB(string filePath, DB targetDB, bool clearExisting)
        {
            try
            {
                // 파일 접근 가능 여부 확인
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }

                // 파일이 다른 프로그램에서 사용 중인지 확인
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // 파일 접근 가능
                    }
                }
                catch (System.IO.IOException ex)
                {
                    Debug.LogError($"File is being used by another program. Please close Excel or any other program that might be using the file: {filePath}\nError: {ex.Message}");
                    return;
                }

                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length < 2)
                {
                    Debug.LogError("Excel file must have at least a header row and one data row");
                    return;
                }
                
                // 헤더 파싱
                string[] headers = ParseCSVLine(lines[0]);
                Dictionary<string, int> columnIndexMap = CreateColumnIndexMap(headers);
                
                Debug.Log($"Headers found: {string.Join(", ", headers)}");
                
                // 필수 컬럼 확인
                if (!ValidateMonsterHeaders(columnIndexMap))
                {
                    Debug.LogError("Missing required headers in the CSV file");
                    return;
                }
                
                if (clearExisting)
                {
                    targetDB.monsterDataList.Clear();
                }
                
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length < columnIndexMap.Count)
                    {
                        Debug.LogWarning($"Row {i} has fewer columns than expected. Skipping.");
                        continue;
                    }
                    
                    MonsterData monsterData = new MonsterData();
                    
                    var idField = typeof(MonsterData).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
                    var nameField = typeof(MonsterData).GetField("monsterName", BindingFlags.NonPublic | BindingFlags.Instance);
                    var descField = typeof(MonsterData).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance);
                    var statusField = typeof(MonsterData).GetField("monsterStatusData", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (idField != null) idField.SetValue(monsterData, GetValue(values, columnIndexMap, "ID"));
                    if (nameField != null) nameField.SetValue(monsterData, GetValue(values, columnIndexMap, "NAME"));
                    if (descField != null) descField.SetValue(monsterData, GetValue(values, columnIndexMap, "DESCRIPTION"));
                    
                    MonsterStatusInfo statusInfo = new MonsterStatusInfo();
                    if (int.TryParse(GetValue(values, columnIndexMap, "MAXHP"), out int maxHp)) statusInfo.MaxHp = maxHp;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DAMAGE"), out int damage)) statusInfo.Damage = damage;
                    if (float.TryParse(GetValue(values, columnIndexMap, "MOVESPEED"), out float moveSpeed)) statusInfo.MoveSpeed = moveSpeed;
                    if (float.TryParse(GetValue(values, columnIndexMap, "ATTACKDELAY"), out float attackDelay)) statusInfo.AttackDelay = attackDelay;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DROPEXPMIN"), out int dropExpMin)) statusInfo.DropExpMin = dropExpMin;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DROPEXPMAX"), out int dropExpMax)) statusInfo.DropExpMax = dropExpMax;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DROPMONEYMIN"), out int dropMoneyMin)) statusInfo.DropMoneyMin = dropMoneyMin;
                    if (int.TryParse(GetValue(values, columnIndexMap, "DROPMONEYMAX"), out int dropMoneyMax)) statusInfo.DropMoneyMax = dropMoneyMax;
                    if (float.TryParse(GetValue(values, columnIndexMap, "ATTACKRANGE"), out float attackRange)) statusInfo.AttackRange = attackRange;
                    
                    if (statusField != null) statusField.SetValue(monsterData, statusInfo);
                    
                    targetDB.monsterDataList.Add(monsterData);
                }
                
                EditorUtility.SetDirty(targetDB);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully imported {lines.Length - 1} monster data entries to selected DB");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import monster data: {e.Message}");
                throw;
            }
        }
        
        public static void ImportTrainDataToDB(string filePath, DB targetDB, bool clearExisting)
        {
            // 파일명에서 TrainData 타입 결정
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            Type trainDataType = GetTrainDataTypeFromFileName(fileName);
            
            ImportTrainDataToDBWithType(filePath, targetDB, clearExisting, trainDataType);
        }
        
        public static void ImportTrainDataToDBWithType(string filePath, DB targetDB, bool clearExisting, Type trainDataType)
        {
            try
            {
                // 파일 접근 가능 여부 확인
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }

                // 파일이 다른 프로그램에서 사용 중인지 확인
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // 파일 접근 가능
                    }
                }
                catch (System.IO.IOException ex)
                {
                    Debug.LogError($"File is being used by another program. Please close Excel or any other program that might be using the file: {filePath}\nError: {ex.Message}");
                    return;
                }

                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length < 2)
                {
                    Debug.LogError("Excel file must have at least a header row and one data row");
                    return;
                }
                
                // 헤더 파싱
                string[] headers = ParseCSVLine(lines[0]);
                Dictionary<string, int> columnIndexMap = CreateColumnIndexMap(headers);
                
                Debug.Log($"Headers found: {string.Join(", ", headers)}");
                Debug.Log($"Importing train data with specified type: {trainDataType.Name}");
                
                // 타입별 리스트 클리어 및 헤더 검증
                if (clearExisting)
                {
                    if (trainDataType == typeof(RangeTrainData))
                    {
                        targetDB.rangeTrainDataList.Clear();
                        if (!ValidateRangeTrainHeaders(columnIndexMap))
                        {
                            Debug.LogError("Missing required headers for RangeTrainData");
                            return;
                        }
                    }
                    else if (trainDataType == typeof(TurretTrainData))
                    {
                        targetDB.turretTrainDataList.Clear();
                        if (!ValidateTurretTrainHeaders(columnIndexMap))
                        {
                            Debug.LogError("Missing required headers for TurretTrainData");
                            return;
                        }
                    }
                    else
                    {
                        targetDB.trainDataList.Clear();
                        if (!ValidateTrainHeaders(columnIndexMap))
                        {
                            Debug.LogError("Missing required headers for TrainData");
                            return;
                        }
                    }
                }
                
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length < columnIndexMap.Count)
                    {
                        Debug.LogWarning($"Row {i} has fewer columns than expected. Skipping.");
                        continue;
                    }
                    
                    // 적절한 TrainData 타입으로 객체 생성
                    TrainData trainData = CreateTrainDataInstanceWithHeaders(trainDataType, values, columnIndexMap);
                    
                    // 타입별 추가 필드 설정
                    if (trainData is RangeTrainData rangeTrainData)
                    {
                        SetRangeTrainDataFieldsWithHeaders(rangeTrainData, values, columnIndexMap);
                        targetDB.rangeTrainDataList.Add(rangeTrainData);
                    }
                    else if (trainData is TurretTrainData turretTrainData)
                    {
                        SetTurretTrainDataFieldsWithHeaders(turretTrainData, values, columnIndexMap);
                        targetDB.turretTrainDataList.Add(turretTrainData);
                    }
                    else
                    {
                        targetDB.trainDataList.Add(trainData);
                    }
                    
                    Debug.Log($"Imported {trainDataType.Name}: {GetValue(values, columnIndexMap, "ID")} - {GetValue(values, columnIndexMap, "NAME")}");
                }
                
                EditorUtility.SetDirty(targetDB);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully imported {lines.Length - 1} {trainDataType.Name} entries to selected DB");
                Debug.Log($"Target DB now contains - Basic: {targetDB.trainDataList.Count}, Range: {targetDB.rangeTrainDataList.Count}, Turret: {targetDB.turretTrainDataList.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import train data: {e.Message}");
                throw;
            }
        }
        
        public static void ImportStageDataToDB(string filePath, DB targetDB, bool clearExisting)
        {
            try
            {
                // 파일 접근 가능 여부 확인
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }

                // 파일이 다른 프로그램에서 사용 중인지 확인
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // 파일 접근 가능
                    }
                }
                catch (System.IO.IOException ex)
                {
                    Debug.LogError($"File is being used by another program. Please close Excel or any other program that might be using the file: {filePath}\nError: {ex.Message}");
                    return;
                }

                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length < 2)
                {
                    Debug.LogError("Excel file must have at least a header row and one data row");
                    return;
                }
                
                // 헤더 파싱
                string[] headers = ParseCSVLine(lines[0]);
                Dictionary<string, int> columnIndexMap = CreateColumnIndexMap(headers);
                
                Debug.Log($"Headers found: {string.Join(", ", headers)}");
                
                // 필수 컬럼 확인
                if (!ValidateStageHeaders(columnIndexMap))
                {
                    Debug.LogError("Missing required headers in the CSV file");
                    return;
                }
                
                if (clearExisting)
                {
                    targetDB.stageDataList.Clear();
                }
                
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length < columnIndexMap.Count)
                    {
                        Debug.LogWarning($"Row {i} has fewer columns than expected. Skipping.");
                        continue;
                    }
                    
                    StageData stageData = new StageData();
                    
                    var idField = typeof(StageData).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
                    var endTimeField = typeof(StageData).GetField("stageEndTime", BindingFlags.NonPublic | BindingFlags.Instance);
                    var inspectionTimeField = typeof(StageData).GetField("stageInspectionTime", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (idField != null) idField.SetValue(stageData, GetValue(values, columnIndexMap, "ID"));
                    if (endTimeField != null && float.TryParse(GetValue(values, columnIndexMap, "STAGEENDTIME"), out float endTime)) 
                        endTimeField.SetValue(stageData, endTime);
                    
                    if (inspectionTimeField != null)
                    {
                        string inspectionTimesValue = GetValue(values, columnIndexMap, "INSPECTIONTIMES");
                        if (!string.IsNullOrEmpty(inspectionTimesValue))
                        {
                            string[] inspectionTimes = inspectionTimesValue.Split(';');
                            float[] times = new float[inspectionTimes.Length];
                            for (int j = 0; j < inspectionTimes.Length; j++)
                            {
                                if (float.TryParse(inspectionTimes[j].Trim(), out float time))
                                {
                                    times[j] = time;
                                }
                            }
                            inspectionTimeField.SetValue(stageData, times);
                        }
                    }
                    
                    targetDB.stageDataList.Add(stageData);
                }
                
                EditorUtility.SetDirty(targetDB);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully imported {lines.Length - 1} stage data entries to selected DB");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import stage data: {e.Message}");
                throw;
            }
        }
        
        #endregion
    }
}

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Excel 가져오기 고급 창
    /// </summary>
    public class ExcelImportWindow : EditorWindow
{
    private string excelFilePath = "";
    private string[] availableSheets = new string[0];
    private int selectedSheetIndex = 0;
    private DataType selectedDataType = DataType.MonsterData;
    private Vector2 scrollPosition;
    private DB targetDB;
    private bool clearExistingData = true;
    
    private enum DataType
    {
        MonsterData,
        TrainData,
        StageData,
        UpgradeData,
        ChoiceData
    }
    
    
    private void OnGUI()
    {
        GUILayout.Label("Excel Data Import", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // DB 선택
        GUILayout.Label("Target DB:", EditorStyles.label);
        targetDB = (DB)EditorGUILayout.ObjectField(targetDB, typeof(DB), false);
        
        if (targetDB == null)
        {
            EditorGUILayout.HelpBox("Please select a DB asset to import data into.", MessageType.Warning);
            GUILayout.Space(10);
        }
        
        // Excel 파일 선택
        GUILayout.Label("Excel File:", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        excelFilePath = EditorGUILayout.TextField(excelFilePath);
		if (GUILayout.Button("Browse", GUILayout.Width(60)))
		{
			string path = EditorUtility.OpenFilePanelWithFilters(
				"Select Excel File",
				"",
				new string[] { "Excel/CSV files", "xlsx,csv", "All files", "*" }
			);
			if (!string.IsNullOrEmpty(path))
			{
				excelFilePath = path;
				LoadSheets();
			}
		}
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // 시트 선택
        if (availableSheets.Length > 0)
        {
            GUILayout.Label("Available Sheets:", EditorStyles.label);
            selectedSheetIndex = EditorGUILayout.Popup(selectedSheetIndex, availableSheets);
        }
        
        GUILayout.Space(10);
        
        // 데이터 타입 선택
        GUILayout.Label("Target Data Type:", EditorStyles.label);
        selectedDataType = (DataType)EditorGUILayout.EnumPopup(selectedDataType);
        
        GUILayout.Space(10);
        
        // 옵션
        GUILayout.Label("Import Options:", EditorStyles.label);
        clearExistingData = EditorGUILayout.Toggle("Clear Existing Data", clearExistingData);
        
        GUILayout.Space(10);
        
        // 미리보기
        if (!string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
        {
            GUILayout.Label("Preview:", EditorStyles.label);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            ShowPreview();
            EditorGUILayout.EndScrollView();
        }
        
        GUILayout.Space(20);
        
        // 가져오기 버튼
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(excelFilePath) || !File.Exists(excelFilePath) || targetDB == null);
        if (GUILayout.Button("Import Data", GUILayout.Height(30)))
        {
            ImportSelectedData();
        }
        EditorGUI.EndDisabledGroup();
        
        GUILayout.Space(10);
        
        // 도움말
        EditorGUILayout.HelpBox(
            "1. DB 에셋을 선택하세요\n" +
            "2. Excel 파일을 CSV로 저장하세요\n" +
            "3. 파일을 선택하면 시트 목록이 표시됩니다\n" +
            "4. 가져올 데이터 타입을 선택하세요\n" +
            "5. 미리보기를 확인하고 Import Data를 클릭하세요",
            MessageType.Info);
    }
    
    private void LoadSheets()
    {
        if (string.IsNullOrEmpty(excelFilePath) || !File.Exists(excelFilePath))
        {
            availableSheets = new string[0];
            return;
        }
        
        try
        {
            // CSV 파일의 경우 첫 번째 줄을 헤더로 사용
            string[] lines = File.ReadAllLines(excelFilePath, System.Text.Encoding.UTF8);
            if (lines.Length > 0)
            {
                availableSheets = new string[] { "Main Sheet" };
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load sheets: {e.Message}");
            availableSheets = new string[0];
        }
    }
    
    private void ShowPreview()
    {
        if (string.IsNullOrEmpty(excelFilePath) || !File.Exists(excelFilePath))
            return;
        
        try
        {
            string[] lines = File.ReadAllLines(excelFilePath, System.Text.Encoding.UTF8);
            int previewLines = Mathf.Min(5, lines.Length);
            
            for (int i = 0; i < previewLines; i++)
            {
                string[] values = ParseCSVLine(lines[i]);
                string previewText = string.Join(" | ", values);
                
                if (i == 0)
                {
                    EditorGUILayout.LabelField($"Header: {previewText}", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"Row {i}: {previewText}");
                }
            }
            
            if (lines.Length > previewLines)
            {
                EditorGUILayout.LabelField($"... and {lines.Length - previewLines} more rows");
            }
        }
        catch (System.Exception e)
        {
            EditorGUILayout.LabelField($"Error reading file: {e.Message}");
        }
    }
    
    private void ImportSelectedData()
    {
        if (string.IsNullOrEmpty(excelFilePath) || !File.Exists(excelFilePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid Excel file", "OK");
            return;
        }
        
        if (targetDB == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target DB", "OK");
            return;
        }
        
        try
        {
            // 시트명을 기반으로 TrainData 타입 결정
            string sheetName = availableSheets.Length > selectedSheetIndex ? availableSheets[selectedSheetIndex] : "Main Sheet";
            Type trainDataType = ExcelImporter.GetTrainDataTypeFromSheetName(sheetName);
            
            Debug.Log($"Importing from sheet: {sheetName}, detected type: {trainDataType.Name}");
            
            switch (selectedDataType)
            {
                case DataType.MonsterData:
                    ExcelImporter.ImportMonsterDataToDB(excelFilePath, targetDB, clearExistingData);
                    break;
                case DataType.TrainData:
                    // 시트명 기반으로 올바른 타입으로 Import
                    ExcelImporter.ImportTrainDataToDBWithType(excelFilePath, targetDB, clearExistingData, trainDataType);
                    break;
                case DataType.StageData:
                    ExcelImporter.ImportStageDataToDB(excelFilePath, targetDB, clearExistingData);
                    break;
                case DataType.UpgradeData:
                    EditorUtility.DisplayDialog("Info", "UpgradeData import not implemented yet", "OK");
                    break;
                case DataType.ChoiceData:
                    EditorUtility.DisplayDialog("Info", "ChoiceData import not implemented yet", "OK");
                    break;
            }
            
            EditorUtility.DisplayDialog("Success", $"Successfully imported {selectedDataType} from Excel", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to import data: {e.Message}", "OK");
        }
    }
    
    private static string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        result.Add(currentField.Trim());
        return result.ToArray();
    }
    
}

namespace TrainDefense.Game.Datas
{
/// <summary>
/// Excel Import View - Excel 데이터 가져오기 전용 창
/// </summary>
public class ExcelImportView : EditorWindow
{
    private int selectedTab = 0;
    private string[] tabNames = { "Import", "Export", "Templates" };
    
    // Import 탭 변수들
    private bool importMonsterData = true;
    private bool importTrainData = true;
    private bool importStageData = true;
    private bool importUpgradeData = false;
    private bool importChoiceData = false;
    private string targetExcelFile = "";
    private DB targetDB;
    
    // Export 탭 변수들
    private DB sourceDB;
    private bool exportMonsterData = true;
    private bool exportTrainData = true;
    private bool exportStageData = true;
    private bool exportUpgradeData = false;
    private bool exportChoiceData = false;
    private string exportPath = "";
    
        // Template 탭 변수들
        private bool createMonsterTemplate = true;
        private bool createTrainTemplate = true;
        private bool createRangeTrainTemplate = true;
        private bool createTurretTrainTemplate = true;
        private bool createStageTemplate = true;
        private string templatePath = "";
    
    public static void ShowWindow()
    {
        ExcelImportView window = GetWindow<ExcelImportView>("Excel Import View");
        window.minSize = new Vector2(500, 400);
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Excel Data Manager", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 탭 선택
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        GUILayout.Space(20);
        
        switch (selectedTab)
        {
            case 0:
                DrawImportTab();
                break;
            case 1:
                DrawExportTab();
                break;
            case 2:
                DrawTemplateTab();
                break;
        }
    }
    
    private void DrawImportTab()
    {
        GUILayout.Label("Import Data from Excel", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Target Excel File 선택
        GUILayout.Label("Target Excel File:", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        targetExcelFile = EditorGUILayout.TextField(targetExcelFile);
		if (GUILayout.Button("Browse", GUILayout.Width(60)))
		{
			string filePath = EditorUtility.OpenFilePanelWithFilters(
				"Select Excel File",
				"",
				new string[] { "Excel/CSV files", "xlsx,csv", "All files", "*" }
			);
			if (!string.IsNullOrEmpty(filePath))
			{
				targetExcelFile = filePath;
			}
		}
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Target DB 선택
        GUILayout.Label("Target DB:", EditorStyles.label);
        targetDB = (DB)EditorGUILayout.ObjectField(targetDB, typeof(DB), false);
        
        if (targetDB == null)
        {
            EditorGUILayout.HelpBox("Please select a target DB to import data to.", MessageType.Warning);
            GUILayout.Space(10);
        }
        
        GUILayout.Space(10);
        
        // 데이터 타입 선택
        GUILayout.Label("Select Data Types to Import:", EditorStyles.label);
        importMonsterData = EditorGUILayout.Toggle("Monster Data", importMonsterData);
        importTrainData = EditorGUILayout.Toggle("Train Data", importTrainData);
        importStageData = EditorGUILayout.Toggle("Stage Data", importStageData);
        importUpgradeData = EditorGUILayout.Toggle("Upgrade Data", importUpgradeData);
        importChoiceData = EditorGUILayout.Toggle("Choice Data", importChoiceData);
        
        GUILayout.Space(20);
        
        // Import 버튼들
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(targetExcelFile) || targetDB == null || 
                                   (!importMonsterData && !importTrainData && !importStageData && !importUpgradeData && !importChoiceData));
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Import Data", GUILayout.Height(30)))
        {
            ImportSelectedData();
        }
        if (GUILayout.Button("Import All Data", GUILayout.Height(30)))
        {
            ImportAllSelectedData();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
        
        GUILayout.Space(10);
        
        // 도움말
        EditorGUILayout.HelpBox(
            "1. Target Excel File을 선택하세요\n" +
            "2. Target DB를 선택하세요\n" +
            "3. 가져올 데이터 타입을 선택하세요\n" +
            "4. Import Data (단일 파일) 또는 Import All Data (폴더 전체)를 클릭하세요",
            MessageType.Info);
    }
    
    private void DrawExportTab()
    {
        GUILayout.Label("Export Data to Excel", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 소스 DB 선택
        GUILayout.Label("Source DB:", EditorStyles.label);
        sourceDB = (DB)EditorGUILayout.ObjectField(sourceDB, typeof(DB), false);
        
        if (sourceDB == null)
        {
            EditorGUILayout.HelpBox("Please select a source DB to export data from.", MessageType.Warning);
            GUILayout.Space(10);
        }
        
        GUILayout.Space(10);
        
        // 내보낼 데이터 타입 선택
        GUILayout.Label("Select Data Types to Export:", EditorStyles.label);
        exportMonsterData = EditorGUILayout.Toggle("Monster Data", exportMonsterData);
        exportTrainData = EditorGUILayout.Toggle("Train Data", exportTrainData);
        exportStageData = EditorGUILayout.Toggle("Stage Data", exportStageData);
        exportUpgradeData = EditorGUILayout.Toggle("Upgrade Data", exportUpgradeData);
        exportChoiceData = EditorGUILayout.Toggle("Choice Data", exportChoiceData);
        
        GUILayout.Space(10);
        
        // 내보내기 경로
        GUILayout.Label("Export Path:", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        exportPath = EditorGUILayout.TextField(exportPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Export Location", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                exportPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        
        // 내보내기 버튼
        EditorGUI.BeginDisabledGroup(sourceDB == null || string.IsNullOrEmpty(exportPath) || 
                                   (!exportMonsterData && !exportTrainData && !exportStageData && !exportUpgradeData && !exportChoiceData));
        if (GUILayout.Button("Export Selected Data", GUILayout.Height(30)))
        {
            ExportSelectedData();
        }
        EditorGUI.EndDisabledGroup();
        
        GUILayout.Space(10);
        
        // 도움말
        EditorGUILayout.HelpBox(
            "1. 소스 DB를 선택하세요\n" +
            "2. 내보낼 데이터 타입을 선택하세요\n" +
            "3. 내보내기 경로를 선택하세요\n" +
            "4. Export Selected Data를 클릭하세요",
            MessageType.Info);
    }
    
    private void DrawTemplateTab()
    {
        GUILayout.Label("Create Excel Templates", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 템플릿 타입 선택
        GUILayout.Label("Select Template Types to Create:", EditorStyles.label);
        createMonsterTemplate = EditorGUILayout.Toggle("Monster Data Template", createMonsterTemplate);
        createTrainTemplate = EditorGUILayout.Toggle("Basic Train Data Template", createTrainTemplate);
        createRangeTrainTemplate = EditorGUILayout.Toggle("Range Train Data Template", createRangeTrainTemplate);
        createTurretTrainTemplate = EditorGUILayout.Toggle("Turret Train Data Template", createTurretTrainTemplate);
        createStageTemplate = EditorGUILayout.Toggle("Stage Data Template", createStageTemplate);
        
        GUILayout.Space(10);
        
        // 템플릿 저장 경로
        GUILayout.Label("Template Save Path:", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        templatePath = EditorGUILayout.TextField(templatePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Template Save Location", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                templatePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        
        // 템플릿 생성 버튼
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(templatePath) || 
                                   (!createMonsterTemplate && !createTrainTemplate && !createRangeTrainTemplate && !createTurretTrainTemplate && !createStageTemplate));
        if (GUILayout.Button("Create Selected Templates", GUILayout.Height(30)))
        {
            CreateSelectedTemplates();
        }
        EditorGUI.EndDisabledGroup();
        
        GUILayout.Space(10);
        
        // 도움말
        EditorGUILayout.HelpBox(
            "1. 생성할 템플릿 타입을 선택하세요\n" +
            "2. 템플릿 저장 경로를 선택하세요\n" +
            "3. Create Selected Templates를 클릭하세요",
            MessageType.Info);
    }
    
    private void ImportSelectedData()
    {
        if (string.IsNullOrEmpty(targetExcelFile))
        {
            EditorUtility.DisplayDialog("Error", "Please select a target Excel file", "OK");
            return;
        }
        
        if (targetDB == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target DB", "OK");
            return;
        }
        
        try
        {
            if (importMonsterData)
            {
                ExcelImporter.ImportMonsterDataToDB(targetExcelFile, targetDB, true);
            }
            
            if (importTrainData)
            {
                // 파일명에 따라 적절한 TrainData 타입으로 Import
                string fileName = Path.GetFileNameWithoutExtension(targetExcelFile).ToLower();
                Type trainDataType = ExcelImporter.GetTrainDataTypeFromFileName(fileName);
                ExcelImporter.ImportTrainDataToDBWithType(targetExcelFile, targetDB, true, trainDataType);
            }
            
            if (importStageData)
            {
                ExcelImporter.ImportStageDataToDB(targetExcelFile, targetDB, true);
            }
            
            if (importUpgradeData)
            {
                EditorUtility.DisplayDialog("Info", "UpgradeData import not implemented yet", "OK");
            }
            
            if (importChoiceData)
            {
                EditorUtility.DisplayDialog("Info", "ChoiceData import not implemented yet", "OK");
            }
            
            EditorUtility.DisplayDialog("Success", "Data imported successfully!", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to import data: {e.Message}", "OK");
        }
    }
    
    private void ImportAllSelectedData()
    {
        if (string.IsNullOrEmpty(targetExcelFile))
        {
            EditorUtility.DisplayDialog("Error", "Please select a target Excel file", "OK");
            return;
        }
        
        if (targetDB == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target DB", "OK");
            return;
        }
        
        try
        {
            // 폴더 경로인지 확인
            if (Directory.Exists(targetExcelFile))
            {
                ExcelImporter.ImportAllData(targetExcelFile, targetDB);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder for Import All Data", "OK");
                return;
            }
            
            EditorUtility.DisplayDialog("Success", "All data imported successfully!", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to import all data: {e.Message}", "OK");
        }
    }
    
    private void ExportSelectedData()
    {
        if (sourceDB == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a source DB", "OK");
            return;
        }
        
        if (string.IsNullOrEmpty(exportPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select an export path", "OK");
            return;
        }
        
        try
        {
            if (exportMonsterData)
            {
                ExcelImporter.ExportMonsterData(Path.Combine(exportPath, "MonsterData.csv"));
            }
            
            if (exportTrainData)
            {
                ExcelImporter.ExportTrainData(Path.Combine(exportPath, "TrainData.csv"));
                ExcelImporter.ExportRangeTrainData(Path.Combine(exportPath, "RangeTrainData.csv"));
                ExcelImporter.ExportTurretTrainData(Path.Combine(exportPath, "TurretTrainData.csv"));
            }
            
            if (exportStageData)
            {
                ExcelImporter.ExportStageData(Path.Combine(exportPath, "StageData.csv"));
            }
            
            if (exportUpgradeData)
            {
                EditorUtility.DisplayDialog("Info", "UpgradeData export not implemented yet", "OK");
            }
            
            if (exportChoiceData)
            {
                EditorUtility.DisplayDialog("Info", "ChoiceData export not implemented yet", "OK");
            }
            
            EditorUtility.DisplayDialog("Success", "Data exported successfully!", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to export data: {e.Message}", "OK");
        }
    }
    
    private void CreateSelectedTemplates()
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a template save path", "OK");
            return;
        }
        
        try
        {
            if (createMonsterTemplate)
            {
                ExcelImporter.CreateMonsterDataTemplate(templatePath);
                Debug.Log("Created MonsterData_Template.csv");
            }
            
            if (createTrainTemplate)
            {
                ExcelImporter.CreateTrainDataTemplate(templatePath);
                Debug.Log("Created TrainData_Template.csv");
            }
            
            if (createRangeTrainTemplate)
            {
                ExcelImporter.CreateRangeTrainDataTemplate(templatePath);
                Debug.Log("Created RangeTrainData_Template.csv");
            }
            
            if (createTurretTrainTemplate)
            {
                ExcelImporter.CreateTurretTrainDataTemplate(templatePath);
                Debug.Log("Created TurretTrainData_Template.csv");
            }
            
            if (createStageTemplate)
            {
                ExcelImporter.CreateStageDataTemplate(templatePath);
                Debug.Log("Created StageData_Template.csv");
            }
            
            EditorUtility.DisplayDialog("Success", "Templates created successfully!", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to create templates: {e.Message}", "OK");
        }
    }
}
}
}