using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Manager
{
public class StageManager : Singleton<StageManager>
{
    #region Variables

    #region Fields

    [SerializeField]
    private int changeInterval = 1;

    [SerializeField]
    private float hpScale;

    [SerializeField]
    private float attackScale;

    [SerializeField]
    private float goldScale;
    #endregion

    private StageData[] _stageDatas;
    private int _currentStageIndex = 0;
    private float _currentStageTime;
    private int _currentStageInspectionTimeIndex;
    private GameObject _currentMapInstance;
    private int _inspectionCount = 0;
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
        _currentStageIndex = 0;
        _ResetCurrentStageInfo();
        _SubscribeEvents();
    }

    private void OnDestroy()
    {
        _UnsubscribeEvents();
    }

    private void _SubscribeEvents()
    {
        GameEventSystem.Subscribe<GetCurrentInspectionDurationEvent, float>(_GetCurrentInspectionDuration);
        GameEventSystem.Subscribe<LevelUpEvent>(_OnLevelUp);
        GameEventSystem.Subscribe<StageSelectEvent>(_OnStageSelected);
    }

    private void _UnsubscribeEvents()
    {
        GameEventSystem.Unsubscribe<GetCurrentInspectionDurationEvent, float>(_GetCurrentInspectionDuration);
        GameEventSystem.Unsubscribe<LevelUpEvent>(_OnLevelUp);
        GameEventSystem.Unsubscribe<StageSelectEvent>(_OnStageSelected);
    }

    private void Update()
    {
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

        _UpdateSpawnRules();
        _SetCurrentStage();
    }

    private void _OnLevelUp(LevelUpEvent levelUpEvent)
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

        MonsterSpawner.Instance.SetSpawnRule(filteredList.ToArray(), CurrentStageData.SpawnInterval);
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
        if (_currentStageTime >= _GetCurrentStageInspectionTime() && _currentStageInspectionTimeIndex < CurrentStageData.StageInspectionTime.Length)
        {
            _CurrentStageInpectionUp();
        }
        else if (_currentStageTime >= CurrentStageData.StageEndTime)
        {
            _StageEnd();
        }
    }

    private void _StageEnd()
    {
        GameEventSystem.Publish(new StageEndEvent(true));
    }

    private void _CurrentStageInpectionUp()
    {
        _currentStageInspectionTimeIndex++;
        GameEventSystem.Publish(new InspectionStartEvent());

        _inspectionCount++;

        // 몬스터 러쉬 체크 : inspectionCount가 changeInterval - 1일 때
        if (changeInterval > 0 && _inspectionCount % changeInterval == changeInterval - 1)
        {
            _StartMonsterRush();
        }

        if (changeInterval > 0 && _inspectionCount % changeInterval == 0)
        {
            _ShowStageSelection();
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
        _currentStageTime += Time.deltaTime;

        float nextInspectionRemainingtime = _GetCurrentStageInspectionTime() - _currentStageTime;
        GameEventSystem.Publish(new ChangeStageTimeEvent(nextInspectionRemainingtime));
    }


    private float _GetCurrentStageInspectionTime()
    {
        if (_currentStageInspectionTimeIndex >= CurrentStageData.StageInspectionTime.Length)
        {
            return CurrentStageData.StageInspectionTime[^1];
        }

        return CurrentStageData.StageInspectionTime[_currentStageInspectionTimeIndex];
    }

    private float _GetCurrentInspectionDuration(GetCurrentInspectionDurationEvent getCurrentInspectionDurationEvent)
    {
        if (CurrentStageData == null || CurrentStageData.StageInspectionTime == null)
        {
            return 0f;
        }

        if (_currentStageInspectionTimeIndex >= CurrentStageData.StageInspectionTime.Length)
        {
            return 0f;
        }

        float currentInspectionTime = CurrentStageData.StageInspectionTime[_currentStageInspectionTimeIndex];
        float previousInspectionTime = _currentStageInspectionTimeIndex > 0
            ? CurrentStageData.StageInspectionTime[_currentStageInspectionTimeIndex - 1]
            : 0f;

        return currentInspectionTime - previousInspectionTime;
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
    }

    private void _ShowStageSelection()
    {
        if (_stageDatas == null || _stageDatas.Length < 2)
        {
            Debug.LogWarning("StageManager: StageData is null or less than 2. Cannot show stage selection.");
            return;
        }

        var candidates = _stageDatas
            .Where((stage, index) => index != _currentStageIndex)
            .OrderBy(_ => Random.value)
            .Take(2)
            .ToArray();

        if (candidates.Length < 2)
        {
            Debug.LogWarning("StageManager: Not enough candidate stages to show selection.");
            return;
        }

        GameEventSystem.Publish(new RandomStageOptionsEvent(candidates[0], candidates[1]));
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
