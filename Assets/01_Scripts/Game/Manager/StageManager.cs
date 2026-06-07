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
        // 난이도 스케일링용 누적 역 통과 수. 스테이지가 바뀌어도 리셋되지 않고 한 판 동안 계속 누적된다.
        // (스테이지 흐름 제어용 _currentStageInspectionTimeIndex 와 분리)
        private int _totalStationPassedCount = 0;
        private bool _shouldShowStageSelectionOnStageEnd;
        private bool _pendingStageSelectionAfterShop;
        private bool _isGameOver;
        #endregion

        public StageData CurrentStageData => _stageDatas[_currentStageIndex];

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
            GameEventSystem.Subscribe<LevelUpEvent>(_OnLevelUp);
            GameEventSystem.Subscribe<StageSelectEvent>(_OnStageSelected);
            GameEventSystem.Subscribe<InspectionEndEvent>(_OnInspectionEnd);
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOverStart);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<GetCurrentInspectionDurationEvent, float>(_GetCurrentInspectionDuration);
            GameEventSystem.Unsubscribe<LevelUpEvent>(_OnLevelUp);
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

        private void _OnLevelUp(LevelUpEvent levelUpEvent)
        {
            _UpdateSpawnRules();
        }

        private void _OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            // 새 게임 시작 시에만 누적 난이도 카운터 초기화 (스테이지 변경 시에는 유지)
            _totalStationPassedCount = 0;
            _UpdateSpawnRules();
        }

        private void _UpdateSpawnRules()
        {
            if (_stageDatas == null || _stageDatas.Length == 0) return;

            var allSpawnDatas = CurrentStageData.SpawnDatas;
            var filteredList = new List<StageSpawnData>();
            int userLevel = UserDataManager.Instance.CurrentLevel;

            foreach (var data in allSpawnDatas)
            {
                if (userLevel >= data.SpawnLevel)
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
            GameEventSystem.Publish(new InspectionStartEvent());
        }

        // 현재 진행 중인 역 구간의 도착 시간.
        // 한 판 동안 통과한 누적 역 수(_totalStationPassedCount)에 비례해 증가하며, 스테이지가 바뀌어도 리셋되지 않는다.
        private float _GetCurrentStationDuration()
        {
            return CurrentStageData.BaseInspectionTime + stationInspectionTimeIncrement * _totalStationPassedCount;
        }

        private void _CurrentStageInspectionUp()
        {
            _currentStageInspectionTimeIndex++;
            _currentStageTime = 0f;
            GameEventSystem.Publish(new InspectionStartEvent());

            _inspectionCount++;
            _totalStationPassedCount++;
        }

        private void _OnGameOverStart(GameOverStartEvent _) => _isGameOver = true;

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
            // 스테이지 종료 버퍼는 기본 도착 시간 기준으로 계산(역 도착 시간 누적 증가의 영향 없이 안정적으로 유지)
            return CurrentStageData.StageEndTime - CurrentStageData.BaseInspectionTime * CurrentStageData.StationCount;
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

            _currentStageIndex = index;
            _ResetCurrentStageInfo();
            MonsterSpawner.Instance?.StartSpawnMonster();

            if (TimelineManager.Instance != null)
                TimelineManager.Instance.StartTimeline(isMapChange: true, () => GameEventSystem.Publish(new EngageStartEvent()));
            else
                GameEventSystem.Publish(new EngageStartEvent());
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
        /// 누적 통과 역 수에 따른 HP 배율 반환 (스테이지가 바뀌어도 리셋되지 않음)
        /// </summary>
        public float GetHPScale()
        {
            // 한 판 시작(누적 0)은 1.0 (기본값)
            if (_totalStationPassedCount <= 0) return 1.0f;

            // 지수(복리) 스케일 — 플레이어 DPS가 곱셈(공격력×공속×치명타×포탑수)으로 커지므로
            // 난이도도 곱으로 추격해야 균형. base = 1 + hpScale (예: 0.10 → 1.10^역수, 역44≈×44)
            return Mathf.Pow(1f + hpScale, _totalStationPassedCount);
        }

        /// <summary>
        /// 누적 통과 역 수에 따른 공격력 배율 반환 (스테이지가 바뀌어도 리셋되지 않음)
        /// </summary>
        public float GetAttackScale()
        {
            // 한 판 시작(누적 0)은 1.0 (기본값)
            if (_totalStationPassedCount <= 0) return 1.0f;

            // TODO: 구체적인 수식 적용 필요
            return 1.0f + (_totalStationPassedCount * attackScale);
        }

        /// <summary>
        /// 누적 통과 역 수에 따른 골드 배율 반환 (스테이지가 바뀌어도 리셋되지 않음)
        /// </summary>
        public float GetGoldScale()
        {
            // 골드는 초반 억제(역0 ×0.30)로 첫 상점 과소비를 막고, 역수에 비례 가속(역44 ≈ ×3.25)해
            // 후반 무한 상점 골드는 유지. base 0.30 + goldScale(0.067)×역수. 경험치는 이 곡선 영향 없음.
            return 0.30f + (_totalStationPassedCount * goldScale);
        }
        #endregion
    }
}
