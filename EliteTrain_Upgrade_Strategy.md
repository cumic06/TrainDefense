# EliteTrain 업그레이드 전략

## 문제 상황

- UpgradeChoice를 3번 선택하면 AddEliteTrainChoice가 나타나야 함
- AddTrainChoice에서 Tier 시스템을 구현하여, Upgrade 3번 이상 한 Train이 없으면 0 tier만, 있으면 1 tier도 선택 가능하도록 해야 함
- 새로 Train을 소환하면 업그레이드된 스탯이 반영되지 않을 수 있음
- MainTrain의 정렬 시스템에도 문제가 발생할 수 있음

## 해결 방안: Tier 기반 AddTrainChoice 필터링 시스템

### 핵심 아이디어

**AddTrainChoice에 Tier 필드를 추가하고, UserDataManager의 업그레이드 이력을 확인하여 Tier 1 선택지를 조건부로 활성화**

- UpgradeChoice를 3번 이상 선택한 Train이 있으면 Tier 1 AddTrainChoice도 선택 가능
- UpgradeChoice를 3번 이상 선택한 Train이 없으면 Tier 0 AddTrainChoice만 선택 가능

---

## 구현 전략

### 1. ChoiceEntry에 Tier 필드 추가

현재 `ChoiceEntry`는 Weight만 가지고 있지만, Tier 필드를 추가해야 합니다:

```csharp
// ChoiceEntry.cs
[Serializable]
public class ChoiceEntry
{
    [UnityEngine.SerializeReference]
    public IChoiceOption Option;
    public int Weight;
    public int Tier;  // ✨ 새로 추가: 0 또는 1
}
```

### 2. AddTrainChoiceImporter에 Tier 필드 추가

Excel 데이터에서 tier 컬럼을 읽어오도록 수정:

```csharp
// AddTrainChoiceImporter.cs
public string[] Headers => new[] { "id", "train_data_id", "weight", "tier" };  // tier 추가

// AddTrainChoiceRow.cs
public int tier;  // ✨ 새로 추가

public void FromExcelRow(IRow row)
{
    id = row.GetCell(0)?.ToString();
    trainDataId = row.GetCell(1)?.ToString();
    int.TryParse(row.GetCell(2)?.ToString(), out weight);
    int.TryParse(row.GetCell(3)?.ToString(), out tier);  // ✨ tier 읽기
}
```

### 3. UserDataManager의 데이터 구조 활용

현재 `UserDataManager`는 다음과 같이 업그레이드 이력을 관리합니다:

```csharp
// UserDataManager.cs
private Dictionary<string, int> _triChoiceData = new();  // ChoiceOption ID -> 선택 횟수
```

- `_triChoiceData`: 선택된 `UpgradeTrainChoice`의 ID와 선택 횟수를 저장
- 예: `{"turret_train_upgrade_choice_1": 3}` → 해당 Choice를 3번 선택함

### 4. UserDataManager에 선택 횟수 조회 메서드 추가

```csharp
// UserDataManager.cs에 추가
/// <summary>
/// 특정 Choice의 선택 횟수를 반환합니다.
/// </summary>
public int GetSelectionCount(string choiceId)
{
    return _triChoiceData.ContainsKey(choiceId) ? _triChoiceData[choiceId] : 0;
}

/// <summary>
/// 특정 Train에 대해 UpgradeChoice를 3번 이상 선택했는지 확인합니다.
/// </summary>
public bool HasTrainUpgradedThreeTimes(string trainDataId)
{
    var triChoiceDB = DatabaseManager.Instance.GetTriChoiceDB();
    var upgradeChoices = triChoiceDB.UpgradeTrainChoices
        .Where(entry => entry.Option is UpgradeTrainChoice upgradeChoice 
                     && upgradeChoice.TargetTrainId == trainDataId)
        .ToList();
    
    // 해당 Train의 UpgradeChoice 중 하나라도 3번 이상 선택되었는지 확인
    foreach (var entry in upgradeChoices)
    {
        if (entry.Option is UpgradeTrainChoice upgradeChoice)
        {
            int count = GetSelectionCount(upgradeChoice.Id);
            if (count >= 3)
            {
                return true;
            }
        }
    }
    
    return false;
}
```

### 5. TriChoiceManager의 GetAddTrainChoices 수정

Tier 필터링 로직을 추가하여 조건에 맞는 AddTrainChoice만 반환:

```csharp
// TriChoiceManager.cs
private List<ChoiceEntry> GetAddTrainChoices()
{
    var addDatas = DatabaseManager.Instance.GetTriChoiceDB().AddTrainChoices;
    var userDataManager = UserDataManager.Instance;
    
    // Upgrade 3번 이상 한 Train이 있는지 확인
    bool hasUpgradedTrain = HasAnyTrainUpgradedThreeTimes();
    
    // 아직 획득하지 않은 train에 대한 choice만 필터링
    return addDatas
        .Where(entry => 
        {
            if (entry.Option == null || !entry.Option.IsValid())
                return false;
            
            // Tier 0은 항상 포함
            if (entry.Tier == 0)
                return true;
            
            // Tier 1은 Upgrade 3번 이상 한 Train이 있을 때만 포함
            if (entry.Tier == 1)
                return hasUpgradedTrain;
            
            // 기타 Tier는 제외 (확장성을 위해)
            return false;
        })
        .ToList();
}

/// <summary>
/// UpgradeChoice를 3번 이상 선택한 Train이 있는지 확인합니다.
/// </summary>
private bool HasAnyTrainUpgradedThreeTimes()
{
    var userDataManager = UserDataManager.Instance;
    if (userDataManager == null) return false;
    
    var triChoiceDB = DatabaseManager.Instance.GetTriChoiceDB();
    var upgradeChoices = triChoiceDB.UpgradeTrainChoices;
    
    foreach (var entry in upgradeChoices)
    {
        if (entry.Option is UpgradeTrainChoice upgradeChoice)
        {
            int count = userDataManager.GetSelectionCount(upgradeChoice.Id);
            if (count >= 3)
            {
                return true;
            }
        }
    }
    
    return false;
}
```

### 6. Train Initialize 시 업그레이드 스탯 복원 로직

#### 6.1 MainTrain.SpawnTrain() 수정

```csharp
public void SpawnTrain(Train trainPrefab)
{
    // ... 기존 코드 ...
  
    Train trainObject = Instantiate(trainPrefab, transform);
    TrainData trainData = DatabaseManager.Instance.GetTrainData(trainPrefab.Id);
    trainObject.Initialize(trainData);
  
    // ✨ 새로 추가: UserDataManager에서 업그레이드 이력 확인 및 스탯 적용
    ApplyTrainUpgradesFromUserData(trainObject, trainData.Id);
  
    // ... 나머지 코드 ...
}
```

#### 6.2 업그레이드 적용 메서드 추가

```csharp
/// <summary>
/// UserDataManager에 저장된 업그레이드 이력을 확인하여 Train에 스탯을 적용합니다.
/// </summary>
private void ApplyTrainUpgradesFromUserData(Train train, string trainDataId)
{
    var userDataManager = UserDataManager.Instance;
    if (userDataManager == null) return;
  
    // 1. 해당 Train에 대한 UpgradeTrainChoice 찾기
    var upgradeChoices = DatabaseManager.Instance.GetTriChoiceDB()
        .UpgradeTrainChoices
        .Where(entry => entry.Option is UpgradeTrainChoice upgradeChoice 
                     && upgradeChoice.TargetTrainId == trainDataId)
        .ToList();
  
    if (upgradeChoices.Count == 0) return;
  
    // 2. 각 UpgradeTrainChoice에 대해 선택 횟수 확인
    foreach (var choiceEntry in upgradeChoices)
    {
        var upgradeChoice = choiceEntry.Option as UpgradeTrainChoice;
        if (upgradeChoice == null) continue;
      
        // UserDataManager에서 선택 횟수 가져오기
        int selectionCount = userDataManager.GetSelectedChoiceIds()
            .Contains(upgradeChoice.Id) 
            ? GetSelectionCount(userDataManager, upgradeChoice.Id) 
            : 0;
      
        if (selectionCount <= 0) continue;
      
        // 3. 선택된 업그레이드 데이터 가져오기 및 적용
        ApplyUpgradesForChoice(train, upgradeChoice, selectionCount);
    }
}

/// <summary>
/// 특정 UpgradeTrainChoice에 대해 선택된 업그레이드를 적용합니다.
/// </summary>
private void ApplyUpgradesForChoice(Train train, UpgradeTrainChoice choice, int selectionCount)
{
    var triChoiceManager = TriChoiceManager.Instance;
    if (triChoiceManager == null) return;
  
    // 선택 횟수만큼 업그레이드 적용
    // 예: 3번 선택했다면 레벨 -1 -> 0 -> 1 -> 2로 3번 업그레이드
    for (int i = 0; i < selectionCount; i++)
    {
        // 현재 Train의 레벨에 맞는 업그레이드 데이터 가져오기
        var upgradeData = triChoiceManager.GetSelectedUpgrade(choice);
        if (upgradeData == null) break;
      
        // 업그레이드 적용
        train.Upgrade(upgradeData);
    }
}

/// <summary>
/// UserDataManager에서 특정 Choice의 선택 횟수를 가져옵니다.
/// </summary>
private int GetSelectionCount(UserDataManager userDataManager, string choiceId)
{
    // UserDataManager에 선택 횟수를 반환하는 메서드가 필요
    // 현재는 _triChoiceData에 저장되어 있으므로 접근 메서드 추가 필요
    // 또는 UserDataManager에 GetSelectionCount(string choiceId) 메서드 추가
}
```

### 7. EliteTrain 교체 시나리오

#### 7.1 ReplaceTrain 메서드 (간단 버전)

```csharp
/// <summary>
/// Train을 EliteTrain으로 교체합니다.
/// UserDataManager의 업그레이드 이력이 자동으로 적용됩니다.
/// </summary>
public void ReplaceTrain(string targetTrainId, Train newTrainPrefab)
{
    Train oldTrain = _currentAliveTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);
    if (oldTrain == null)
    {
        Debug.LogWarning($"ReplaceTrain: Train with ID '{targetTrainId}' not found");
        return;
    }
  
    // 기존 Train의 위치와 인덱스 저장
    int aliveIndex = _currentAliveTrains.IndexOf(oldTrain);
    int originalIndex = _trainOriginalIndexMap.ContainsKey(oldTrain) 
        ? _trainOriginalIndexMap[oldTrain] 
        : _currentTrains.IndexOf(oldTrain);
    Vector3 localPosition = oldTrain.transform.localPosition;
  
    // 기존 Train 제거
    _currentAliveTrains.Remove(oldTrain);
    _currentTrains.Remove(oldTrain);
    _trainOriginalIndexMap.Remove(oldTrain);
  
    // 새 Train 생성
    Train newTrain = Instantiate(newTrainPrefab, transform);
    TrainData newTrainData = DatabaseManager.Instance.GetTrainData(newTrainPrefab.Id);
    newTrain.Initialize(newTrainData);
    newTrain.IsUnDead = isUnDead;
  
    // ✨ UserDataManager의 업그레이드 이력 자동 적용
    // (SpawnTrain과 동일한 로직)
    ApplyTrainUpgradesFromUserData(newTrain, newTrainData.Id);
  
    // 위치 복원
    newTrain.transform.localPosition = localPosition;
  
    // 리스트에 추가 (같은 위치에)
    _currentAliveTrains.Insert(aliveIndex, newTrain);
    _currentTrains.Insert(originalIndex, newTrain);
    _trainOriginalIndexMap[newTrain] = originalIndex;
  
    // 기존 Train 제거
    Destroy(oldTrain.gameObject);
  
    // 재정렬 (위치는 이미 복원했으므로 필요 없을 수도 있음)
    // RearrangeTrains();
  
    Debug.Log($"Train replaced: {targetTrainId} -> {newTrainPrefab.Id}");
}
```

---

## 장점

### ✅ 리플렉션 불필요

- UserDataManager에 이미 업그레이드 이력이 저장되어 있음
- 공개된 API만 사용하여 구현 가능

### ✅ 일관된 스탯 관리

- Train 소환 시 항상 동일한 로직으로 스탯 적용
- EliteTrain 교체 시에도 기존 업그레이드가 자동으로 적용됨

### ✅ MainTrain 정렬 유지

- 기존 Train의 위치와 인덱스를 보존
- `_currentAliveTrains`와 `_currentTrains` 리스트의 순서 유지

### ✅ 확장성

- 새로운 Train 타입 추가 시에도 동일한 로직 적용 가능
- 업그레이드 시스템 변경 시 UserDataManager만 수정하면 됨

---

## 고려사항

### 1. 업그레이드 적용 순서

- 현재 `TriChoiceManager.GetSelectedUpgrade()`는 Train의 현재 레벨을 기준으로 업그레이드를 선택
- Train 초기화 시 레벨이 -1이므로, 순차적으로 업그레이드를 적용해야 함
- 각 업그레이드 적용 후 레벨이 증가하므로 다음 업그레이드 선택에 영향

### 2. TriChoiceManager의 _selectedUpgrades 캐시

- `TriChoiceManager._selectedUpgrades`는 런타임 캐시
- Train 교체 시 기존 Train의 레벨과 새 Train의 레벨이 다를 수 있음
- 캐시 무효화 또는 재선택 로직 필요할 수 있음

### 3. Tier 필터링 로직

- Tier 0: 기본 AddTrainChoice, 항상 선택 가능
- Tier 1: EliteTrain 등 고급 Train, Upgrade 3번 이상 한 Train이 있을 때만 선택 가능
- Tier 필터링은 `GetAddTrainChoices()`에서 런타임에 동적으로 수행

### 4. 성능

- Train 소환 시마다 UserDataManager 조회 및 업그레이드 적용
- 업그레이드가 많을 경우 초기화 시간 증가 가능
- 필요 시 최적화 고려

---

## 구현 순서

1. **ChoiceEntry에 Tier 필드 추가**
   - `ChoiceEntry.cs`에 `public int Tier` 필드 추가
2. **AddTrainChoiceImporter 수정**
   - Excel 헤더에 "tier" 추가
   - `AddTrainChoiceRow`에 `tier` 필드 추가
   - Excel에서 tier 컬럼 읽기 로직 추가
   - `ChoiceEntry`에 tier 값 설정 로직 추가
3. **UserDataManager 확장**
   - `GetSelectionCount(string choiceId)` 메서드 추가
   - `HasTrainUpgradedThreeTimes(string trainDataId)` 메서드 추가
4. **TriChoiceManager 수정**
   - `HasAnyTrainUpgradedThreeTimes()` 메서드 추가
   - `GetAddTrainChoices()`에 Tier 필터링 로직 추가
5. **MainTrain에 업그레이드 적용 메서드 추가** (선택사항)
   - `ApplyTrainUpgradesFromUserData()` 구현
   - `ApplyUpgradesForChoice()` 구현
6. **SpawnTrain 수정** (선택사항)
   - Initialize 후 업그레이드 적용 로직 호출
7. **테스트**
   - UpgradeChoice를 3번 미만 선택한 상태에서 AddTrainChoice 확인 (Tier 0만 나와야 함)
   - UpgradeChoice를 3번 이상 선택한 Train이 있을 때 AddTrainChoice 확인 (Tier 1도 나와야 함)
   - 일반 Train 소환 시 업그레이드 적용 확인 (선택사항)

---

## 결론

이 방식은 **Tier 시스템을 통해 AddTrainChoice를 조건부로 필터링**하여, UpgradeChoice를 3번 이상 선택한 Train이 있을 때만 Tier 1 AddTrainChoice(EliteTrain 등)를 선택할 수 있도록 합니다.

- **Tier 0**: 기본 Train, 항상 선택 가능
- **Tier 1**: EliteTrain 등 고급 Train, Upgrade 3번 이상 한 Train이 있을 때만 선택 가능

UserDataManager의 업그레이드 이력을 확인하여 런타임에 동적으로 필터링하므로, 리플렉션 없이 구현 가능하며 확장성이 좋습니다.
