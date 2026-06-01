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
        private bool _shouldShowStageSelectionOnStageEnd;
        private bool _isGameOver;
        #endregion

        public StageData CurrentStageData => _stageDatas[_currentStageIndex];

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

            _UpdateSpawnRules();
            _SetCurrentStage();
        }

        private void _OnLevelUp(LevelUpEvent levelUpEvent)
        {
            _UpdateSpawnRules();
        }

        private void _OnGameEnter(GameEnterEvent gameEnterEvent)
        {
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
                if (_currentStageTime >= GetInspectionDurationForIndex(_currentStageInspectionTimeIndex))
                {
                    _CurrentStageInspectionUp();
                }
            }
            else if (_shouldShowStageSelectionOnStageEnd
                     && _currentStageTime >= _GetPostLastInspectionDuration())
            {
                _shouldShowStageSelectionOnStageEnd = false;
                _TransitionToStageSelection();
            }
        }

        private float GetInspectionDurationForIndex(int i)
        {
            return CurrentStageData.BaseInspectionTime + stationInspectionTimeIncrement * i;
        }

        private void _CurrentStageInspectionUp()
        {
            _currentStageInspectionTimeIndex++;
            _currentStageTime = 0f;
            GameEventSystem.Publish(new InspectionStartEvent());

            _inspectionCount++;
        }

        private void _OnGameOverStart(GameOverStartEvent _) => _isGameOver = true;

        private void _OnInspectionEnd(InspectionEndEvent inspectionEndEvent)
        {
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

            return GetInspectionDurationForIndex(_currentStageInspectionTimeIndex);
        }

        private float _GetPostLastInspectionDuration()
        {
            float sum = 0f;
            for (int i = 0; i < CurrentStageData.StationCount; i++)
                sum += GetInspectionDurationForIndex(i);
            return CurrentStageData.StageEndTime - sum;
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
            MonsterSpawner.Instance?.DestroyAllMonsters();
            ResourceManager.Instance.ReturnAll();
            GameEventSystem.Publish(new EngageReadyEvent());

            _ShowStageSelection();
        }

        #region Scaling
        /// <summary>
        /// 현재 역 인덱스에 따른 HP 배율 반환
        /// </summary>
        public float GetHPScale()
        {
            // 0번째 역(시작)은 1.0 (기본값)
            if (_currentStageInspectionTimeIndex <= 0) return 1.0f;

            // TODO: 구체적인 수식 적용 필요
            return 1.0f + (_currentStageInspectionTimeIndex * hpScale);
        }

        /// <summary>
        /// 현재 역 인덱스에 따른 공격력 배율 반환
        /// </summary>
        public float GetAttackScale()
        {
            // 0번째 역(시작)은 1.0 (기본값)
            if (_currentStageInspectionTimeIndex <= 0) return 1.0f;

            // TODO: 구체적인 수식 적용 필요
            return 1.0f + (_currentStageInspectionTimeIndex * attackScale);
        }

        /// <summary>
        /// 현재 역 인덱스에 따른 골드 배율 반환
        /// </summary>
        public float GetGoldScale()
        {
            // 0번째 역(시작)은 1.0 (기본값)
            if (_currentStageInspectionTimeIndex <= 0) return 1.0f;

            // TODO: 구체적인 수식 적용 필요
            return 1.0f + (_currentStageInspectionTimeIndex * goldScale);
        }
        #endregion
    }
}
