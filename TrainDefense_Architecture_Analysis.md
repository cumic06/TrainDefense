# TrainDefense 아키텍처 분석 및 개선 제안

## 1. TriChoice와 UpgradeData에서 제거해야 할 변수들

### 1.1 UpgradeTrainChoice 클래스의 문제점

**위치**: `Assets/01_Scripts/Game/Datas/TriChoiceData/UpgradeTrainChoice.cs`

#### 문제가 되는 변수/프로퍼티:

1. **`_selectedUpgrade` (private 필드)**
   - **문제**: 런타임 상태를 데이터 클래스에 저장하고 있음
   - **현재 사용**: `SelectedUpgrade` 프로퍼티에서 지연 초기화로 사용
   - **영향**: 데이터 클래스가 런타임 상태를 가지게 되어 직렬화/역직렬화 시 문제 발생 가능
   - **해결 방안**: 
     - `_selectedUpgrade` 필드를 제거
     - `SelectedUpgrade` 프로퍼티를 제거하고, 대신 `SelectRandomUpgrade()` 메서드를 직접 호출하도록 변경
     - 선택된 업그레이드는 `TriChoiceManager`나 별도의 런타임 상태 관리 클래스에서 관리

2. **`SelectedUpgrade` 프로퍼티**
   - **문제**: 런타임에 랜덤 선택을 수행하는 로직이 데이터 클래스에 포함됨
   - **현재 사용**: 
     - `TriChoiceUI.cs:92` - UI 표시용
     - `TriChoiceSelectUI.cs:63` - UI 표시용
     - `UpgradeTrainChoice.Execute()` - 실행 시 사용
   - **해결 방안**:
     - 선택 로직을 `TriChoiceManager`로 이동
     - 선택된 업그레이드는 별도의 런타임 컨텍스트 객체에 저장
     - `IChoiceOption` 인터페이스에 `GetDisplayInfo()` 같은 메서드 추가하여 UI 정보만 반환

### 1.2 TrainUpgradeData 계열 클래스의 문제점

**위치**: 
- `Assets/01_Scripts/Game/Datas/TrainUpgradeData/TrainUpgradeData.cs`
- `Assets/01_Scripts/Game/Datas/TrainUpgradeData/TurretTrainUpgradeData.cs`
- `Assets/01_Scripts/Game/Datas/TrainUpgradeData/RangeTrainUpgradeData.cs`

#### 문제가 되는 변수:

1. **`level` 필드 (SerializeField, ReadOnly)**
   - **문제**: 데이터 클래스에 런타임 상태(레벨)를 저장하고 있음
   - **현재 사용**: 
     - `Level` 프로퍼티로 노출
     - `UpgradeTrainChoice.IsValid()` 및 `SelectRandomUpgrade()`에서 Train의 현재 레벨과 비교
     - `DatabaseManager.GetTrainUpgradeData()`에서 레벨 필터링에 사용
   - **문제점**: 
     - 하나의 업그레이드 데이터가 특정 레벨에만 해당한다는 잘못된 설계
     - 실제로는 `upgradeStats` 배열의 인덱스가 레벨을 나타냄
   - **해결 방안**:
     - `level` 필드를 완전히 제거
     - 레벨 정보는 `upgradeStats` 배열의 인덱스로 계산 (인덱스 + 1 = 레벨)
     - Train의 현재 레벨에 맞는 업그레이드를 찾을 때는 `upgradeStats` 배열을 순회하며 찾도록 변경

### 1.3 UpgradeData 클래스의 문제점

**위치**: `Assets/01_Scripts/Game/Datas/UpgradeData.cs`

#### 검토가 필요한 변수:

1. **`needMoney`**
   - **현재 상태**: TriChoice와 직접적인 연관은 없어 보임
   - **판단**: Shop 시스템용으로 보이며, TriChoice와는 무관해 보임

2. **`maxUpgradeCount`**
   - **현재 상태**: TriChoice와 직접적인 연관은 없어 보임
   - **판단**: Shop 시스템용으로 보이며, TriChoice와는 무관해 보임

## 2. 변수 위치 수정 제안

### 2.1 UpgradeTrainChoice의 선택된 업그레이드 관리

**현재 구조**:
```
UpgradeTrainChoice (데이터)
  └─ _selectedUpgrade (런타임 상태) ❌
```

**제안하는 구조**:
```
Option 1: Manager에서 관리
TriChoiceManager
  └─ Dictionary<UpgradeTrainChoice, ITrainUpgradeData> selectedUpgrades

Option 2: 별도 컨텍스트 클래스
ChoiceSelectionContext (런타임 전용)
  └─ Dictionary<string, ITrainUpgradeData> selectedUpgradesByChoiceId
```

**권장 방안**: Option 2
- 런타임 상태와 데이터를 명확히 분리
- `TriChoiceManager`가 `ChoiceSelectionContext`를 관리
- 선택된 업그레이드는 게임 세션 동안만 유지

### 2.2 TrainUpgradeData의 레벨 정보

**현재 구조**:
```
TrainUpgradeData (데이터)
  └─ level: int (런타임 상태로 오해될 수 있음) ❌
  └─ upgradeStats: TrainUpgradeStats[] (실제 데이터)
```

**제안하는 구조**:
```
TrainUpgradeData (데이터)
  └─ upgradeStats: TrainUpgradeStats[] (인덱스 = 레벨 - 1)
  └─ GetStatsForLevel(int level): TrainUpgradeStats (헬퍼 메서드)
```

**변경 사항**:
- `level` 필드 제거
- `Level` 프로퍼티 제거
- `GetStatsForLevel(int level)` 메서드로 레벨별 스탯 조회
- `ITrainUpgradeData` 인터페이스에서 `Level` 프로퍼티 제거

## 3. TrainDefense 구조의 고질적인 문제점

### 3.1 데이터와 런타임 상태의 혼재

**문제점**:
- 데이터 클래스(`UpgradeTrainChoice`, `TrainUpgradeData`)에 런타임 상태가 포함됨
- 직렬화/역직렬화 시 예상치 못한 동작 발생 가능
- 데이터 재사용 시 상태 오염 위험

**영향**:
- 게임 재시작 시 선택된 업그레이드가 유지될 수 있음
- 데이터 에디터에서 수정 시 런타임 상태가 리셋되지 않을 수 있음
- 멀티플레이어나 세이브/로드 시스템 구현 시 문제 발생 가능

**해결책**:
1. **명확한 책임 분리**
   - 데이터 클래스: 불변 데이터만 보관
   - 런타임 상태: 별도 컨텍스트 클래스에서 관리
   - Manager 클래스: 데이터와 상태를 연결하는 역할만 수행

2. **인터페이스 설계 개선**
   ```csharp
   // 현재
   public interface IChoiceOption {
       string Id { get; }
       bool IsValid();
       void Execute();
   }
   
   // 제안
   public interface IChoiceOption {
       string Id { get; }
       bool IsValid();
       void Execute();
       ChoiceDisplayInfo GetDisplayInfo(); // UI 정보만 반환
   }
   ```

### 3.2 레벨 정보의 이중 표현

**문제점**:
- `TrainUpgradeData.level` 필드와 `upgradeStats` 배열 인덱스가 중복
- 실제 레벨은 Train 인스턴스에만 존재해야 함
- 데이터 클래스의 `level`은 단순히 배열 인덱스를 의미하는데, 런타임 상태로 오해될 수 있음

**영향**:
- 코드 가독성 저하
- 버그 발생 가능성 증가 (레벨 불일치)
- 데이터 설계의 모호함

**해결책**:
1. `level` 필드 완전 제거
2. 레벨 정보는 Train 인스턴스에서만 관리
3. 업그레이드 데이터는 레벨별 스탯 배열만 보관
4. 레벨 조회는 헬퍼 메서드로 제공: `GetStatsForLevel(int level)`

### 3.3 Manager 클래스의 과도한 책임

**문제점**:
- `DatabaseManager`가 데이터 조회뿐만 아니라 런타임 상태(Train의 현재 레벨)를 참조하여 필터링 수행
- `TriChoiceManager`가 선택 로직뿐만 아니라 데이터 필터링도 수행
- Manager 간 의존성이 복잡함

**영향**:
- 단위 테스트 어려움
- 코드 재사용성 저하
- 순환 의존성 위험

**해결책**:
1. **Repository 패턴 도입**
   ```csharp
   public interface ITrainUpgradeRepository {
       ITrainUpgradeData GetById(string id);
       IEnumerable<ITrainUpgradeData> GetByTrainIdAndLevel(string trainId, int level);
   }
   ```

2. **Service 레이어 분리**
   ```csharp
   public class TriChoiceService {
       private readonly ITrainUpgradeRepository _upgradeRepository;
       private readonly ITrainStateProvider _trainStateProvider;
       
       public ITrainUpgradeData SelectUpgrade(UpgradeTrainChoice choice) {
           // 선택 로직만 담당
       }
   }
   ```

### 3.4 선택 로직의 분산

**문제점**:
- `UpgradeTrainChoice.SelectRandomUpgrade()`: 데이터 클래스에 선택 로직 포함
- `TriChoiceManager.GetRandomChoices()`: Manager에 선택 로직 포함
- 선택 로직이 여러 곳에 분산되어 있음

**영향**:
- 로직 변경 시 여러 곳 수정 필요
- 일관성 유지 어려움
- 테스트 복잡도 증가

**해결책**:
1. **선택 전략 패턴 도입**
   ```csharp
   public interface IUpgradeSelectionStrategy {
       ITrainUpgradeData Select(IEnumerable<WeightedUpgradeData> upgrades, int currentLevel);
   }
   
   public class WeightedRandomSelectionStrategy : IUpgradeSelectionStrategy {
       // 가중치 기반 랜덤 선택 로직
   }
   ```

2. **선택 로직을 Service로 통합**
   - `UpgradeTrainChoice`에서 선택 로직 제거
   - `TriChoiceService`에서 모든 선택 로직 관리

### 3.5 UI와 데이터의 강한 결합

**문제점**:
- `TriChoiceUI`가 `UpgradeTrainChoice.SelectedUpgrade`에 직접 접근
- UI가 데이터 구조에 의존적
- UI 변경 시 데이터 구조도 변경해야 할 수 있음

**영향**:
- UI 리팩토링 어려움
- 데이터 구조 변경 시 UI 코드도 수정 필요

**해결책**:
1. **DTO(Data Transfer Object) 패턴 사용**
   ```csharp
   public class ChoiceDisplayInfo {
       public Sprite Icon { get; set; }
       public string Name { get; set; }
       public string Description { get; set; }
       // UI에 필요한 정보만 포함
   }
   ```

2. **IChoiceOption 인터페이스 확장**
   - `GetDisplayInfo()` 메서드 추가
   - UI는 인터페이스만 의존하도록 변경

### 3.6 데이터 접근의 비효율성

**문제점**:
- `DatabaseManager.GetTrainUpgradeData(string id)`가 복잡한 로직 수행
  1. TriChoiceDB에서 UpgradeTrainChoice 찾기
  2. TrainManager를 통해 MainTrain 접근
  3. Train의 현재 레벨 확인
  4. WeightedUpgrades에서 필터링
- 단순 ID 조회가 아닌 복잡한 로직이 포함됨

**영향**:
- 성능 저하 가능성
- 코드 가독성 저하
- 단위 테스트 어려움

**해결책**:
1. **명확한 메서드 분리**
   ```csharp
   // ID로 직접 조회
   public ITrainUpgradeData GetTrainUpgradeDataById(string id);
   
   // Choice ID로 조회 (별도 메서드)
   public ITrainUpgradeData GetTrainUpgradeDataByChoiceId(string choiceId, string trainId, int level);
   ```

2. **캐싱 전략 도입**
   - 자주 조회되는 데이터는 캐시
   - 레벨별 업그레이드 데이터 인덱싱

## 4. 개선 우선순위

### 높은 우선순위 (즉시 수정 권장)
1. ✅ `UpgradeTrainChoice._selectedUpgrade` 필드 제거
2. ✅ `TrainUpgradeData.level` 필드 제거
3. ✅ 선택 로직을 데이터 클래스에서 분리

### 중간 우선순위 (점진적 개선)
4. ⚠️ Manager 클래스의 책임 분리
5. ⚠️ UI와 데이터의 결합도 낮추기
6. ⚠️ 데이터 접근 메서드 명확화

### 낮은 우선순위 (장기적 리팩토링)
7. 📋 Repository 패턴 도입
8. 📋 Service 레이어 분리
9. 📋 선택 전략 패턴 도입

## 5. 마이그레이션 가이드

### 5.1 UpgradeTrainChoice 수정

**Before**:
```csharp
public class UpgradeTrainChoice : IChoiceOption {
    private ITrainUpgradeData _selectedUpgrade;
    
    public ITrainUpgradeData SelectedUpgrade {
        get {
            if (_selectedUpgrade == null) {
                _selectedUpgrade = SelectRandomUpgrade(db);
            }
            return _selectedUpgrade;
        }
    }
}
```

**After**:
```csharp
public class UpgradeTrainChoice : IChoiceOption {
    // _selectedUpgrade 제거
    // SelectedUpgrade 프로퍼티 제거
    
    // 선택 로직은 TriChoiceService로 이동
    public ChoiceDisplayInfo GetDisplayInfo(ITrainUpgradeData selectedUpgrade) {
        // UI 정보만 반환
    }
}
```

### 5.2 TrainUpgradeData 수정

**Before**:
```csharp
public class TrainUpgradeData : ITrainUpgradeData {
    [SerializeField, ReadOnly]
    private int level = 1;
    
    public int Level => level;
}
```

**After**:
```csharp
public class TrainUpgradeData : ITrainUpgradeData {
    // level 필드 제거
    // Level 프로퍼티 제거
    
    public TrainUpgradeStats GetStatsForLevel(int level) {
        int index = level - 1;
        if (index < 0 || index >= upgradeStats.Length) return null;
        return upgradeStats[index];
    }
}
```

### 5.3 ITrainUpgradeData 인터페이스 수정

**Before**:
```csharp
public interface ITrainUpgradeData : IDescribableData, IIconData {
    TrainStatusData StatusUpgrade { get; }
    int Level { get; }  // 제거 필요
    int MaxLevel { get; }
}
```

**After**:
```csharp
public interface ITrainUpgradeData : IDescribableData, IIconData {
    TrainStatusData GetStatusUpgrade(int level);  // 레벨 파라미터 추가
    int MaxLevel { get; }
}
```

## 6. 결론

TrainDefense 프로젝트의 주요 문제는 **데이터와 런타임 상태의 혼재**입니다. 이를 해결하기 위해서는:

1. 데이터 클래스에서 런타임 상태 완전 제거
2. 런타임 상태는 별도 컨텍스트 클래스에서 관리
3. Manager 클래스의 책임 명확화
4. UI와 데이터의 결합도 낮추기

이러한 변경을 통해 코드의 유지보수성, 테스트 가능성, 확장성을 크게 향상시킬 수 있습니다.
