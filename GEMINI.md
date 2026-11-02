<!-- 이 파일은 앱이 자동 생성합니다(Gemini CLI용). 직접 고치지 말고 rules\ 폴더의 .md 파일을 수정한 뒤 다시 내보내세요. -->

(아직 등록된 프로젝트 규칙이 없습니다 — rules\ 폴더에 .md 파일을 추가하세요.)


## 프로젝트 지도 (앱 자동 생성 2026-11-03 00:53 · 파일 3359개 · 폴더 381개)

구조 파악은 이 지도로 끝내고, 지도에 없는 부분만 열어 본다. 폴더를 처음부터 다시 훑거나 파일을 전수로 읽지 않는다.
지도의 각 줄은 파일 앞부분의 첫 주석·선언 한 줄일 뿐이다 — 세부 동작이 필요한 파일만 골라 연다.

- 종류: Unity 프로젝트 (에디터 6000.3.11f1 · 패키지 23개: com.antigravity.editor, com.boxqkrtm.ide.cursor, com.cysharp.unitask, com.nosuchstudio.rtltmpro, com.redeyestemplate.core, com.unity.2d.animation, com.unity.2d.aseprite, com.unity.2d.psdimporter, com.unity.2d.sprite, com.unity.2d.spriteshape, com.unity.2d.tilemap, com.unity.2d.tilemap.extras …)
- 씬: Assets/00_Scenes/00_AuthScene.unity, Assets/00_Scenes/01_LobbyScene.unity, Assets/00_Scenes/02_GameScene.unity, Assets/Plugins/Demigiant/DOTweenPro Examples/DOTweenAnimation_Advanced.unity, Assets/Plugins/Demigiant/DOTweenPro Examples/DOTweenAnimation_Basics.unity, Assets/Plugins/Demigiant/DOTweenPro Examples/DOTweenPath.unity, Assets/Settings/Scenes/URP2DSceneTemplate.unity

### (루트) (코드 4 · 기타 15)
- .mcp.json
- DESIGN.md — ============================================================
- README.md — TrainDefense
- StoreDescription.md — TrainDefense — 앱스토어 / 구글플레이 스토어 설명

### Assets (코드 1 · 에셋 2 · 기타 1)
- google-services.json

### Assets/00_Scenes (코드 3)
- 00_AuthScene.unity
- 01_LobbyScene.unity
- 02_GameScene.unity

### Assets/01_Scripts (코드 7)
- AutoReleaseEffect.cs — class AutoReleaseEffect : MonoBehaviour — 스폰된 이펙트가 lifetime 후 스스로 풀에 반환(ResourceManager.Destroy)되게 한다.
- LobbyGameSimulation.cs — class LobbyGameSimulation : MonoBehaviour
- ResourceManager.cs — class ResourceManager : MonoBehaviour — 씬 언로드/앱 종료로 ResourceManager가 파괴되는 중인지. 파괴 중엔 풀 회수(SetParent)를 막아
- TrainDefenseAchievement.cs — class TrainDefenseAchievement : MonoBehaviour, IAchievementTracker
- UserDataManager.cs — class UserDataManager : Singleton<UserDataManager> — 해금된 개조 스킬 ID. 판이 끝날 때 조건을 만족한 개조가 쌓이고 다음 판부터 상점에 뜬다.
- UserOptionData.cs — class UserOptionData
- UserOptionDataParser.cs — class UserOptionDataParser — 사운드/햅틱 옵션 PlayerPrefs 키를 모두 삭제한다. 다음 Load 시 기본값으로 복원된다.

### Assets/01_Scripts/Cumic (코드 15)
- AuthSceneSequencer.cs — class AuthSceneSequencer : ISceneSequencer — 로그인 팝업UI 표시
- GameEventSystem.cs — class GameEventSystem — 핸들러 내부에서 같은 이벤트를 구독/해제해도 순회가 깨지지 않도록 스냅샷을 순회하고,
- GameSceneSequence.cs — class GameSceneSequence : MonoBehaviour
- InGameSequence.cs — class InGameSequence : MonoBehaviour — TriChoiceUI 등 다른 컴포넌트의 Start()보다 먼저 이벤트를 구독해야
- LoadingSceneController.cs — class LoadingSceneController : MonoBehaviour — 최소한 한 프레임 대기하여 LoadingScene이 완전히 로드되도록 함
- LobbySceneSequencer.cs — class LobbySceneSequencer : ISceneSequencer — 대충 유저 정보 불러오는 코드
- NumberExtension.cs — class NumberExtension
- SceneController.cs — class SceneController — LoadingScene은 빌드 목록에서 꺼져 있을 수 있다. 그 상태로 이름 로드를 하면
- SequenceInvokor.cs — class SequenceInvokor : MonoBehaviour
- Singleton.cs — class Singleton
- UnityExtension.cs — class UnityExtension
- Util.cs — class Util — 컴포넌트가 있으면 가져오고, 없으면 추가합니다.
- UtilMath.cs — class UtilMath — 두 Vector3 사이의 제곱 거리를 반환합니다. (sqrt 없이 빠른 비교용)
- UtilTransform.cs — class UtilTransform — Transform의 position, rotation, localScale을 초기값으로 리셋합니다.
- UtilUI.cs — class UtilUI — RectTransform의 너비를 설정합니다.

### Assets/01_Scripts/Cumic/Achievement (코드 6)
- AchievementCatalog.cs — class AchievementCatalog — 모든 업적의 정적 정의 목록. 인게임 트래커(TrainDefenseAchievement)와
- AchievementData.cs — class AchievementData : IAchievementData — 업적 한 개의 정적 정의. 카탈로그(AchievementCatalog)가 단일 소스로 보유하며,
- AchievementProgressEvent.cs — class AchievementProgressEvent
- AchievementSaveData.cs — class AchievementSaveData — 저장된 업적 진행/달성 데이터를 삭제한다. (초기화)
- AchievementState.cs — class AchievementState
- AchievementUnlockedEvent.cs — class AchievementUnlockedEvent

### Assets/01_Scripts/Cumic/Checker (코드 2)
- AuthChecker.cs — class AuthChecker : IAuthable
- VersionChecker.cs — class VersionChecker : IVersionable — 최신 버전 문자열이 저장된 URL (redeyeshq.github.io)

### Assets/01_Scripts/Cumic/Events (코드 7)
- EngageReadyEvent.cs — class EngageReadyEvent
- EngageStartEvent.cs — class EngageStartEvent
- GameEndEvent.cs — class GameEndEvent
- GameEnterEvent.cs — class GameEnterEvent
- GetUserNameEvent.cs — class GetUserNameEvent
- LobbyEnterEvent.cs — class LobbyEnterEvent
- StageEndEvent.cs — class StageEndEvent

### Assets/01_Scripts/Cumic/Interface (코드 6)
- IAchievementCondition.cs — interface IAchievementCondition
- IAchievementData.cs — interface IAchievementData
- IAchievementTracker.cs — interface IAchievementTracker
- IAuthable.cs — interface IAuthable
- ISceneSequencer.cs — interface ISceneSequencer : IDisposable //인터페이스 쓰는 이유 : 간단하게 만들 게임은 특정 씬 시퀀서가 필요 없어서
- IVersionable.cs — interface IVersionable

### Assets/01_Scripts/Cumic/Sequence (코드 2)
- BasePhase.cs — enum BasePhase
- OverlayPhase.cs — enum OverlayPhase

### Assets/01_Scripts/Cumic/UI (코드 3)
- ChangeSceneButton.cs — class ChangeSceneButton : MonoBehaviour
- UIManager.cs — class UIManager : Singleton<UIManager>
- VersionUpdatePopup.cs — class VersionUpdatePopup : MonoBehaviour — 버전 불일치 시 뜨는 강제 업데이트 팝업. 업데이트 버튼을 누르면 스토어로 이동한다.

### Assets/01_Scripts/Editor (코드 6)
- DatabaseBrowserWindow.cs — class DatabaseBrowserWindow : EditorWindow — DB.asset 통합 브라우저 (Tools > DB Browser).
- DatabaseGeneratorWindow.cs — class DatabaseGeneratorWindow : EditorWindow
- MissingReferenceBuildProcessor.cs — class MissingReferenceBuildProcessor : IPreprocessBuildWithReport — Usually we want to stop the build if there are critical errors.
- MissingReferenceCheckerWindow.cs — class MissingReferenceCheckerWindow : EditorWindow
- MissingReferenceScanner.cs — class MissingReferenceScanner — Scan Prefabs
- SkillTreeGraphWindow.cs — class SkillTreeGraphWindow : EditorWindow — 스킬트리 노드 그래프 에디터 (Tools > SkillTree Graph Editor).

### Assets/01_Scripts/Editor/DataImport (코드 7)
- ExcelImporterRegistry.cs — class ExcelImporterRegistry
- ExcelReadUtil.cs — class HeaderMap — 헤더 이름 → 컬럼 인덱스 매핑
- ExcelTemplate.cs — class ExcelTemplate — Header row — 이미 있는 헤더는 자리와 이름을 그대로 둔다.
- ExcelWriter.cs — class ExcelWriter — 헤더 행에서 HeaderMap 생성
- IExcelRow.cs — interface IExcelRow — Row adapter for Excel read/write operations.
- IExcelSheetImporter.cs — interface IExcelSheetImporter
- SnakeCaseUtil.cs — class SnakeCaseUtil

### Assets/01_Scripts/Editor/DataImport/Importers (코드 13)
- AddTrainChoiceImporter.cs — class AddTrainChoiceImporter : IExcelSheetImporter
- ImporterBootstrap.cs — class ImporterBootstrap — Registers sheet importers on load.
- MonsterImporter.cs — class MonsterImporter : IExcelSheetImporter
- PermanentUpgradeImporter.cs — class PermanentUpgradeImporter : IExcelSheetImporter — Excel 헤더 안내 (H: category[TurretStat/Passive], I: passive_type, K: stat_type)
- RangeTrainImporter.cs — class RangeTrainImporter : IExcelSheetImporter
- RewardChoiceImporter.cs — class RewardChoiceImporter : IExcelSheetImporter — 만렙 보상 선택지(골드 / 엘리트 재화)를 ChoiceData.xlsx의 reward_choice_data 시트에서 임포트한다.
- StageImporter.cs — class StageImporter : IExcelSheetImporter
- StatUpgradeImporter.cs — class StatUpgradeTierImporter : IExcelSheetImporter — 상점 스탯 강화 등급을 StatUpgradeData.xlsx의 stat_upgrade_tier_data 시트에서 임포트한다.
- TrainImporter.cs — class TrainImporter : IExcelSheetImporter
- TrainPassiveSkillDataImporter.cs — class TrainPassiveSkillDataImporter : IExcelSheetImporter
- TurretTrainImporter.cs — class TurretTrainImporter : IExcelSheetImporter — RangeTrain과 동일하게 attack_interval 명칭 사용
- UpgradeDataImporter.cs — class UpgradeDataImporter : IExcelSheetImporter — Excel 헤더 안내 (H: upgrade_type, I: stat_type, K: pool[LevelUp/ShopItem], L: stats["AttackDam
- UpgradeTrainChoiceImporter.cs — class UpgradeTrainChoiceImporter : IExcelSheetImporter

### Assets/01_Scripts/Editor/DataImport/Importers/Reflectors (코드 5)
- MonsterDataReflector.cs — class MonsterDataReflector
- RangeTrainDataReflector.cs — class RangeTrainDataReflector
- TrainDataReflector.cs — class TrainDataReflector
- TrainPassiveSkillDataReflector.cs — class TrainPassiveSkillDataReflector
- TurretTrainDataReflector.cs — class TurretTrainDataReflector

### Assets/01_Scripts/Editor/DataImport/Importers/Rows (코드 13)
- AddTrainChoiceRow.cs — class AddTrainChoiceRow : IExcelRow
- MonsterRow.cs — class MonsterRow : IExcelRow
- PermanentUpgradeRow.cs — class PermanentUpgradeRow : IExcelRow
- RangeTrainRow.cs — class RangeTrainRow : TrainRow
- RewardChoiceRow.cs — class RewardChoiceRow : IExcelRow
- StageRow.cs — class StageRow : IExcelRow
- StatUpgradeTierRow.cs — class StatUpgradeTierRow : IExcelRow
- TrainPassiveSkillDataRow.cs — class TrainPassiveSkillDataRow : IExcelRow — 개조 해금 조건 원문("AttackDamage>=100;Kills>=300"). RemodelUnlockCondition.ParseList가 해석한다.
- TrainRow.cs — class TrainRow : IExcelRow
- TrainStatUpgradeRuleRow.cs — class TrainStatUpgradeRuleRow : IExcelRow — 포탑 한 대의 강화 규칙 한 줄 — 스탯이 컬럼으로 펼쳐진 가로 배치.
- TurretTrainRow.cs — class TurretTrainRow : TrainRow
- UpgradeRow.cs — class UpgradeRow : IExcelRow — 선택지 풀(LevelUp/ShopItem). 비어 있으면 LevelUp.
- UpgradeTrainChoiceRow.cs — class UpgradeTrainChoiceRow : IExcelRow

### Assets/01_Scripts/Game/Analytics (코드 1)
- AnalyticsConsent.cs — enum AnalyticsConsentState — Analytics 데이터 수집 동의 상태를 PlayerPrefs에 영속화한다.

### Assets/01_Scripts/Game/Analytics/Editor (코드 1)
- AnalyticsConsentDebugMenu.cs — class AnalyticsConsentDebugMenu — Analytics 동의 상태 디버그 메뉴. 동의 팝업을 다시 띄워 테스트할 때 사용한다.

### Assets/01_Scripts/Game/Controller (코드 2)
- CameraController.cs — class CameraController : MonoBehaviour
- MapController.cs — class MapController : MonoBehaviour — 배경 타일 2장을 왼쪽으로 흘려보내며 무한 스크롤을 만든다.

### Assets/01_Scripts/Game/Datas (코드 23)
- DB.cs — class DB : ScriptableObject, ISerializationCallbackReceiver — 게임 내 모든 데이터를 통합 관리하는 중앙 데이터베이스
- EliteData.cs — class EliteData — variants 가 비어있으면 코드 기본 4종을 생성해 돌려준다.
- EliteType.cs — enum EliteType — 적 엘리트 4종 타입. 색(불 이펙트) + 고유 능력으로 구분한다.
- EliteVariant.cs — class EliteVariant — 엘리트 타입별 변형 정의. 스탯 배율 + 불 이펙트 색 + 타입 고유 능력 파라미터를 담는다.
- IData.cs — interface IData
- IDescribableData.cs — interface IDescribableData : IData

(지도 12KB 상한으로 이하 생략 — 나머지는 필요할 때 직접 ls 한다)