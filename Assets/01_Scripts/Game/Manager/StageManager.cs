using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using TrainDefense;

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
    private int _currentStageIndex;
    private float _currentStageTime;
    private int _currentStageInspectionTimeIndex;
    private GameObject _currentMapInstance;
    private int _inspectionCount = 0;
    #endregion

    public StageData CurrentStageData => _stageDatas[_currentStageIndex];

    private void Start()
    {
        LoadStageDatas();
        _currentStageIndex = 0;
        ResetCurrentStageInfo();

        GameEventSystem.Subscribe<GetNextInspectionRemainingTimeEvent, float>(GetNextInspectionRemainingTime);
        GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);
        GameEventSystem.Subscribe<StageSelectEvent>(OnStageSelected);
    }

    private void OnDestroy()
    {
        GameEventSystem.Unsubscribe<GetNextInspectionRemainingTimeEvent, float>(GetNextInspectionRemainingTime);
        GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
        GameEventSystem.Unsubscribe<StageSelectEvent>(OnStageSelected);
    }

    private void LoadStageDatas()
    {
        _stageDatas = DatabaseManager.Instance.GetStageDatas();
    }

    private void ResetCurrentStageInfo()
    {
        _currentStageTime = 0;
        _currentStageInspectionTimeIndex = 0;
        _inspectionCount = 0;

        UpdateSpawnRules();
        SetCurrentStage();
    }

    private void OnLevelUp(LevelUpEvent levelUpEvent)
    {
        UpdateSpawnRules();
    }

    private void UpdateSpawnRules()
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

        MonsterSpawner.Instance.SetSpawnRule(filteredList.ToArray());
    }

    private void SetCurrentStage()
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

    private void Update()
    {
        CurrentStageTimeUp();

        StageHandler();
    }

    private void StageHandler()
    {
        if (_currentStageTime >= GetCurrentStageInspectionTime() && _currentStageInspectionTimeIndex < CurrentStageData.StageInspectionTime.Length)
        {
            CurrentStageInpectionUp();
        }
        else if (_currentStageTime >= CurrentStageData.StageEndTime)
        {
            StageEnd();
        }
    }

    private void StageEnd()
    {
        GameEventSystem.Publish(new StageEndEvent(true));
    }

    private void CurrentStageInpectionUp()
    {
        _currentStageInspectionTimeIndex++;
        GameEventSystem.Publish(new InspectionEvent());

        _inspectionCount++;

        // 몬스터 러쉬 체크 : inspectionCount가 changeInterval - 1일 때
        if (changeInterval > 0 && _inspectionCount % changeInterval == changeInterval - 1)
        {
            StartMonsterRush();
        }

        if (changeInterval > 0 && _inspectionCount % changeInterval == 0)
        {
            ShowStageSelection();
        }
    }
    
    private void StartMonsterRush()
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

    private void CurrentStageTimeUp()
    {
        _currentStageTime += Time.deltaTime;

        float nextInspectionRemainingtime = GetCurrentStageInspectionTime() - _currentStageTime;
        GameEventSystem.Publish(new ChangeStageTimeEvent(nextInspectionRemainingtime));
    }


    private float GetCurrentStageInspectionTime()
    {
        if (_currentStageInspectionTimeIndex >= CurrentStageData.StageInspectionTime.Length)
        {
            return CurrentStageData.StageInspectionTime[^1];
        }

        return CurrentStageData.StageInspectionTime[_currentStageInspectionTimeIndex];
    }

    private float GetNextInspectionRemainingTime(GetNextInspectionRemainingTimeEvent getNextInspectionRemainingTimeEvent)
    {
        if (_currentStageInspectionTimeIndex >= CurrentStageData.StageInspectionTime.Length)
        {
            return CurrentStageData.StageInspectionTime[^1] - _currentStageTime;
        }
        return CurrentStageData.StageInspectionTime[_currentStageInspectionTimeIndex] - _currentStageTime;
    }

    private void OnStageSelected(StageSelectEvent stageSelectedEvent)
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
        ResetCurrentStageInfo();
    }

    private void ShowStageSelection()
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