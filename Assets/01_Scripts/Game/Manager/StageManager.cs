using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;
using TrainDefense;

namespace TrainDefense.Game.Manager
{
    public class StageManager : Singleton<StageManager>
    {
        #region Variables

        #region Fields

        [SerializeField]
        private int monsterRushInterval = 0;

        [SerializeField]
        private int mapSelectInterval = 3;

        [SerializeField]
        private float hpScale;

        // 상점을 지날 때마다 hpScale에 더해지는 증가폭. 0이면 매 상점 같은 비율(단순 지수 성장)이 된다.
        [SerializeField]
        private float hpScaleIncrement;

        [SerializeField]
        private float attackScale;

        [SerializeField]
        private float goldScale;

        [SerializeField]
        private float stationInspectionTimeIncrement;
        #endregion

        private StageData[] _stageDatas;
        private int _currentStageIndex = 0;
        private float _currentStageTime;
        private int _currentStageInspectionTimeIndex = 0;
        private GameObject _currentMapInstance;
        private int _inspectionCount = 0;
        // 난이도·가격 스케일링용 누적 상점 방문 수. 역 도착 상점과 맵 선택 상점을 모두 세므로
        // 스테이지당 4씩 늘어난다. 스테이지가 바뀌어도 리셋되지 않고 한 판 동안 계속 누적된다.
        // (스테이지 흐름 제어용 _currentStageInspectionTimeIndex 와 분리)
        private int _totalInspectionPassedCount = 0;
        private bool _shouldShowStageSelectionOnStageEnd;
        private bool _pendingStageSelectionAfterShop;
        private bool _isGameOver;

        // 맵별 점수 결산 기록(게임오버 슬라이드쇼용). 맵 진입 시 점수/처치 수를 스냅샷(_mapStart*)하고,
        // 다음 맵으로 넘어가거나(_OnStageSelected) 게임이 끝날 때(_OnGameEnd) 차이를 StageRunRecord로 확정한다.
        private readonly List<StageRunRecord> _runRecords = new();
        private int _mapStartScore;
        private int _mapStartNormalKill;
        private int _mapStartEliteKill;
        // 게임오버 시 마지막 맵 마감이 중복되지 않도록 하는 가드(FinalizeAndGetRecords).
        private bool _runFinalized;
        #endregion

        public StageData CurrentStageData => _stageDatas[_currentStageIndex];

        /// <summary>
        /// 한 판 동안 열린 누적 상점 수(역 도착 + 맵 선택). 난이도 스케일과 상품 가격이 공유하는 시간 축.
        /// </summary>
        public int TotalInspectionPassedCount => _totalInspectionPassedCount;

        /// <summary>게임오버 결산용 — 한 판 동안 거쳐 간 맵별 점수/처치 기록(방문 순서).</summary>
        public IReadOnlyList<StageRunRecord> RunRecords => _runRecords;

        /// <summary>
        /// 스테이지 선택 전 마지막 상점을 들른 상태로, 상점을 닫으면 스테이지 선택 UI가 떠야 하는지 여부.
        /// </summary>
        public bool IsStageSelectionPending => _pendingStageSelectionAfterShop;

        //protected override void Awake()
        //{
        //    base.Awake();
        //    //TODO: 타이밍 때문에 Awake에 빼뒀지만 나중에는 Start로 옮기는거 고려해보기.
        //}

        private void Start()
        {
            _LoadStageDatas();
            _currentStageIndex = Random.Range(0, _stageDatas.Length);
            _ResetCurrentStageInfo();
            _SubscribeEvents();
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();
        }

        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Subscribe<GetCurrentInspectionDurationEvent, float>(_GetCurrentInspectionDuration);
            GameEventSystem.Subscribe<StageSelectEvent>(_OnStageSelected);
            GameEventSystem.Subscribe<InspectionEndEvent>(_OnInspectionEnd);
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOverStart);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<GetCurrentInspectionDurationEvent, float>(_GetCurrentInspectionDuration);
            GameEventSystem.Unsubscribe<StageSelectEvent>(_OnStageSelected);
            GameEventSystem.Unsubscribe<InspectionEndEvent>(_OnInspectionEnd);
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);
        }

        private void FixedUpdate()
        {
            if (_isGameOver) return;
            if (Cumic.Sequence.InGameSequence.Instance is { IsRunning: false }) return;
            _CurrentStageTimeUp();
            _StageHandler();
        }

        private void _LoadStageDatas()
        {
            _stageDatas = DatabaseManager.Instance.GetStageDatas();
        }

        private void _ResetCurrentStageInfo()
        {
            _currentStageTime = 0;
            _currentStageInspectionTimeIndex = 0;
            _inspectionCount = 0;
            _shouldShowStageSelectionOnStageEnd = false;
            _pendingStageSelectionAfterShop = false;

            _UpdateSpawnRules();
            _SetCurrentStage();
        }

        private void _OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            // 새 게임 시작 시에만 누적 난이도 카운터 초기화 (스테이지 변경 시에는 유지)
            _totalInspectionPassedCount = 0;
            _UpdateSpawnRules();

            // 맵별 결산 기록 초기화. 첫 맵은 게임 시작과 동시이므로 진입 스냅샷을 0으로 둔다.
            // (ScoreManager도 GameEnterEvent에서 0으로 리셋되지만 구독 순서가 불확정이라 직접 0 고정)
            _runRecords.Clear();
            _runFinalized = false;
            _mapStartScore = 0;
            _mapStartNormalKill = 0;
            _mapStartEliteKill = 0;
        }

        private void _UpdateSpawnRules()
        {
            if (_stageDatas == null || _stageDatas.Length == 0) return;

            var allSpawnDatas = CurrentStageData.SpawnDatas;
            var filteredList = new List<StageSpawnData>();

            // 몬스터 해금 기준 = 누적 역 도착 수. 역 도착이 곧 성장(상점·강화) 시점이 되면서
            // 유저 레벨보다 진행도를 잘 나타내게 됐다. SpawnLevel 값은 그대로 역 도착 횟수로 읽는다.
            int progressCount = _totalInspectionPassedCount;

            foreach (var data in allSpawnDatas)
            {
                if (progressCount >= data.SpawnLevel)
                {
                    filteredList.Add(data);
                }
            }

            var mapData = DatabaseManager.Instance.GetMapData(CurrentStageData);
            MonsterSpawner.Instance.SetSpawnRule(filteredList.ToArray(), CurrentStageData.SpawnInterval, mapData?.CustomSpawnAreas, mapData?.SpawnEffectPrefab);
        }

        private void _SetCurrentStage()
        {
            if (_stageDatas == null || _stageDatas.Length == 0)
            {
                Debug.LogWarning("StageManager: _stageDatas is null or empty. Cannot set map.");
                return;
            }

            var stageData = CurrentStageData;
            if (stageData == null)
            {
                Debug.LogWarning("StageManager: CurrentStageData is null. Cannot set map.");
                return;
            }

            var mapData = DatabaseManager.Instance.GetMapData(stageData);
            if (mapData == null)
            {
                Debug.LogWarning($"StageManager: StageData [{stageData.Id}] has null MapData. Cannot set map.");
                return;
            }

            var prefab = mapData.Prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"StageManager: MapData [{mapData.Id}] prefab is null. Cannot instantiate map.");
                return;
            }

            if (_currentMapInstance != null)
            {
                Destroy(_currentMapInstance);
            }

            Vector3 spawnPosition = Vector3.zero;
            if (TrainManager.Instance.MainTrain != null)
            {
                spawnPosition = TrainManager.Instance.MainTrain.transform.position;
            }

            // 맵 전환 연출 중에는 기차가 화면 밖에 있거나 아직 프리팹 참조 상태라 위치가 화면과 어긋날 수 있다.
            // 가로는 실제 보이는 기준인 카메라에 맞춰 생성해 맵 끝이 검게 비는 문제를 막는다.
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                spawnPosition.x = mainCamera.transform.position.x;
            }

            _currentMapInstance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        }

        private void _StageHandler()
        {
            if (CurrentStageData == null) return;

            if (_currentStageInspectionTimeIndex < CurrentStageData.StationCount)
            {
                if (_currentStageTime >= _GetCurrentStationDuration())
                {
                    _CurrentStageInspectionUp();
                }
            }
            else if (_shouldShowStageSelectionOnStageEnd
                     && _currentStageTime >= _GetPostLastInspectionDuration())
            {
                _shouldShowStageSelectionOnStageEnd = false;
                _BeginStageSelectionShop();
            }
        }

        /// <summary>
        /// 스테이지 선택 직전에 상점을 한 번 더 들르게 한다.
        /// 상점을 닫는 순간(<see cref="InspectionEndEvent"/>) 스테이지 선택 UI로 전환된다.
        /// </summary>
        private void _BeginStageSelectionShop()
        {
            _pendingStageSelectionAfterShop = true;
            _StartInspectionWithTimeline();
        }

        /// <summary>
        /// 상점(점검) 진입 연출을 거쳐 상점을 연다.
        /// 화면이 페이드 아웃되며 기차가 빠져나간 뒤 InspectionStartEvent가 발행된다.
        /// </summary>
        private void _StartInspectionWithTimeline()
        {
            if (TimelineManager.Instance != null)
                TimelineManager.Instance.StartShopEnterTimeline(() => GameEventSystem.Publish(new InspectionStartEvent()));
            else
                GameEventSystem.Publish(new InspectionStartEvent());

            // 역 도착·맵 선택 두 경로의 공통 진입점. 난이도와 가격이 같은 축을 쓰도록 여기서만 센다.
            _totalInspectionPassedCount++;

            // 몬스터 해금이 이 카운트를 기준으로 하므로 늘린 직후 목록을 다시 만든다.
            _UpdateSpawnRules();
        }

        // 현재 진행 중인 역 구간의 도착 시간.
        // 한 판 동안 열린 누적 상점 수(_totalInspectionPassedCount)에 비례해 증가하며, 스테이지가 바뀌어도 리셋되지 않는다.
        private float _GetCurrentStationDuration()
        {
            return CurrentStageData.BaseInspectionTime + stationInspectionTimeIncrement * _totalInspectionPassedCount;
        }

        private void _CurrentStageInspectionUp()
        {
            _currentStageInspectionTimeIndex++;
            _currentStageTime = 0f;
            _StartInspectionWithTimeline();

            _inspectionCount++;
        }

        private void _OnGameOverStart(GameOverStartEvent _) => _isGameOver = true;

        /// <summary>
        /// 게임오버 결산 UI가 호출 — 아직 마감 안 된 현재(마지막) 맵 구간을 확정하고 전체 기록을 반환한다.
        /// GameEndEvent 구독 순서에 의존하면 맵을 1개만 간 판에서 결산 UI가 먼저 읽어 빈 목록이 되므로,
        /// UI가 읽기 직전에 직접 마감을 트리거하는 방식으로 순서 의존을 없앤다.
        /// </summary>
        public IReadOnlyList<StageRunRecord> FinalizeAndGetRecords()
        {
            if (!_runFinalized)
            {
                _CloseMapRecord();
                _runFinalized = true;
            }
            return _runRecords;
        }

        // 현재 맵 진입 시점의 점수/처치 수를 스냅샷한다. (다음 _CloseMapRecord의 기준값)
        private void _BeginMapRecord()
        {
            var scoreManager = ScoreManager.Instance;
            if (scoreManager == null) return;

            _mapStartScore = scoreManager.CurrentScore;
            _mapStartNormalKill = scoreManager.NormalKillCount;
            _mapStartEliteKill = scoreManager.EliteKillCount;
        }

        // 현재 머문 맵 구간을 마감해 RunRecords에 누적한다.
        // 번 점수/처치 수 = 현재값 - 진입 스냅샷(_mapStart*). 맵 정보는 현재 CurrentStageData 기준.
        private void _CloseMapRecord()
        {
            var scoreManager = ScoreManager.Instance;
            if (scoreManager == null) return;

            var stage = CurrentStageData;
            _runRecords.Add(new StageRunRecord
            {
                StageId = stage != null ? stage.Id : string.Empty,
                StageImage = stage != null ? stage.StageImage : null,
                ScoreEarned = Mathf.Max(0, scoreManager.CurrentScore - _mapStartScore),
                NormalKill = Mathf.Max(0, scoreManager.NormalKillCount - _mapStartNormalKill),
                EliteKill = Mathf.Max(0, scoreManager.EliteKillCount - _mapStartEliteKill),
            });
        }

        private void _OnInspectionEnd(InspectionEndEvent inspectionEndEvent)
        {
            // 스테이지 선택용 상점을 닫은 경우, 전투로 복귀하지 않고 스테이지 선택 UI로 전환한다.
            if (_pendingStageSelectionAfterShop)
            {
                _pendingStageSelectionAfterShop = false;
                _TransitionToStageSelection();
                return;
            }

            bool shouldStartMonsterRush = monsterRushInterval > 0
                && _inspectionCount > 0
                && _inspectionCount % monsterRushInterval == monsterRushInterval - 1;

            if (shouldStartMonsterRush)
            {
                _StartMonsterRush();
            }

            if (mapSelectInterval > 0
                && _inspectionCount > 0
                && _inspectionCount % mapSelectInterval == 0)
            {
                _shouldShowStageSelectionOnStageEnd = true;
            }
        }

        private void _StartMonsterRush()
        {
            GameEventSystem.Publish(new MonsterRushEvent(0.5f));
        }

        //private void TriggerBossSpawn()
        //{
        //    StartCoroutine(BossSpawnCoroutine());
        //}

        //private void BossSpawnCoroutine()
        //{
        //    var stageData = CurrentStageData;
        //    if (stageData == null || string.IsNullOrEmpty(stageData.BossMonsterId))
        //    {
        //        Debug.LogWarning("StageManager: BossMonsterId is null or empty. Cannot spawn boss.");
        //        yield break;
        //    }

        //    GameEventSystem.Publish(new BossSpawnEvent(stageData.BossMonsterId, 0.7f));
        //    Debug.Log($"[StageManager] Boss spawn triggered: {stageData.BossMonsterId}");
        //}

        private void _CurrentStageTimeUp()
        {
            float remainingTime = _GetCurrentSegmentDuration() - _currentStageTime;

            GameEventSystem.Publish(new ChangeStageTimeEvent(remainingTime));

            _currentStageTime += Time.deltaTime;
        }


        private float _GetCurrentSegmentDuration()
        {
            if (CurrentStageData == null) return 0f;

            if (_currentStageInspectionTimeIndex >= CurrentStageData.StationCount)
                return _GetPostLastInspectionDuration();

            return _GetCurrentStationDuration();
        }

        private float _GetPostLastInspectionDuration()
        {
            // 스테이지 선택 직전 마지막 구간도 일반 역과 동일한 도착 시간 사용(이 구간만 길어지던 문제 해결)
            return _GetCurrentStationDuration();
        }

        private float _GetCurrentInspectionDuration(GetCurrentInspectionDurationEvent getCurrentInspectionDurationEvent)
        {
            return _GetCurrentSegmentDuration();
        }

        private void _OnStageSelected(StageSelectEvent stageSelectedEvent)
        {
            if (stageSelectedEvent == null || stageSelectedEvent.SelectedStageData == null)
            {
                Debug.LogWarning("StageManager: StageSelectEvent or SelectedStageData is null.");
                return;
            }

            var selectedStage = stageSelectedEvent.SelectedStageData;

            int index = System.Array.FindIndex(_stageDatas, s => s != null && s.Id == selectedStage.Id);
            if (index < 0)
            {
                Debug.LogWarning($"StageManager: Selected StageData [{selectedStage.Id}] not found in stage list.");
                return;
            }

            // 이전 맵 구간 결산을 마감한 뒤, 새 맵으로 전환하며 새 구간 점수 스냅샷을 시작한다.
            // (선택~전환 사이에는 전투가 없어 점수가 변하지 않으므로 이 시점에 스냅샷해도 안전)
            _CloseMapRecord();
            _currentStageIndex = index;
            _BeginMapRecord();

            if (TimelineManager.Instance != null)
            {
                // 상점 진입에서 검게 가려진 상태 그대로, 암전 사이 맵을 새 맵으로 교체하고
                // 기차가 슬라이드 인 + 페이드 인하며 새 맵 전투를 시작한다(상점 퇴장 연출과 동일).
                TimelineManager.Instance.StartMapMoveTimeline(
                    onMapSwitch: () =>
                    {
                        _ResetCurrentStageInfo();
                        MonsterSpawner.Instance?.StartSpawnMonster();
                    },
                    onComplete: () => GameEventSystem.Publish(new EngageStartEvent()));
            }
            else
            {
                _ResetCurrentStageInfo();
                MonsterSpawner.Instance?.StartSpawnMonster();
                GameEventSystem.Publish(new EngageStartEvent());
            }
        }

        private bool _ShowStageSelection()
        {
            if (_stageDatas == null || _stageDatas.Length < 2)
            {
                Debug.LogWarning("StageManager: StageData is null or less than 2. Cannot show stage selection.");
                return false;
            }

            var candidates = _stageDatas
                .Where((stage, index) => index != _currentStageIndex)
                .OrderBy(_ => Random.value)
                .Take(2)
                .ToArray();

            if (candidates.Length < 2)
            {
                Debug.LogWarning("StageManager: Not enough candidate stages to show selection.");
                return false;
            }

            GameEventSystem.Publish(new RandomStageOptionsEvent(candidates[0], candidates[1]));
            return true;
        }

        public void ForceMapSelection() => _TransitionToStageSelection();

        private void _TransitionToStageSelection()
        {
            MonsterSpawner.Instance?.StopSpawnMonster();
            MonsterSpawner.Instance?.DestroyAllMonsters();
            ResourceManager.Instance.ReturnAll();
            GameEventSystem.Publish(new EngageReadyEvent());

            _ShowStageSelection();
        }

        #region Scaling
        /// <summary>
        /// 누적 상점 방문 수에 따른 HP 배율 반환 (스테이지가 바뀌어도 리셋되지 않음)
        /// </summary>
        public float GetHPScale()
        {
            // 한 판 시작(누적 0)은 1.0 (기본값)
            if (_totalInspectionPassedCount <= 0) return 1.0f;

            // 가속 복리 스케일 — 상점을 지날수록 증가율 자체가 hpScaleIncrement만큼 커진다.
            // 유저 성장이 2차식이라 단일 비율 지수로는 초반이 가파르고 후반이 밋밋해서 그 반대 모양을 만든다.
            // hpScaleIncrement가 0이면 Pow(1 + hpScale, 상점수)와 정확히 같다.
            float scale = 1f;
            for (int i = 0; i < _totalInspectionPassedCount; i++)
            {
                scale *= 1f + hpScale + hpScaleIncrement * i;
            }

            return scale;
        }

        /// <summary>
        /// 누적 상점 방문 수에 따른 공격력 배율 반환 (스테이지가 바뀌어도 리셋되지 않음)
        /// </summary>
        public float GetAttackScale()
        {
            // 한 판 시작(누적 0)은 1.0 (기본값)
            if (_totalInspectionPassedCount <= 0) return 1.0f;

            // HP와 달리 선형 — 둘 다 곱으로 커지면 후반에 기차가 즉사한다.
            // 부하는 HP가 지고 공격력은 완만하게 따라간다.
            return 1.0f + (_totalInspectionPassedCount * attackScale);
        }

        /// <summary>
        /// 누적 상점 방문 수에 따른 골드 배율 반환 (스테이지가 바뀌어도 리셋되지 않음)
        /// </summary>
        public float GetGoldScale()
        {
            // 골드 배율 = 1.0 + goldScale×상점수. 드랍골드(엑셀)가 곧 초반 실드랍, 상점수 비례 가속.
            return 1.0f + (_totalInspectionPassedCount * goldScale);
        }
        #endregion
    }
}
