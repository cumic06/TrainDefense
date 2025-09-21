using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    private StageData[] _stageDatas;
    private int _currentStageIndex;

    private float _currentStageTime;
    private int _currentStageInspectionTimeIndex;

    private void Awake()
    {
        LoadStageDatas();
        ResetCurrentStageInfo();
        GameEventSystem.Subscribe<GetLastStageInspectionTimeEvent, float>(OnGetLastStageInspectionTime);
    }

    private void OnDestroy()
    {
        GameEventSystem.Unsubscribe<GetLastStageInspectionTimeEvent, float>(OnGetLastStageInspectionTime);
    }

    private void ResetCurrentStageInfo()
    {
        _currentStageIndex = 0;
        _currentStageTime = 0;
        _currentStageInspectionTimeIndex = 0;
    }

    private void LoadStageDatas()
    {
        _stageDatas = Resources.LoadAll<StageData>("Datas/StageDatas");
    }

    private void Update()
    {
        _currentStageTime += Time.deltaTime;
        GameEventSystem.Publish(new ChangeStageTimeEvent(_currentStageTime));

        if (_currentStageTime >= GetCurrentStageInspectionTime())
        {
            _currentStageInspectionTimeIndex++;
            GameEventSystem.Publish(new EngageReadyEvent());
        }
    }

    private StageData GetCurrentStageData()
    {
        return _stageDatas[_currentStageIndex];
    }

    private float GetCurrentStageInspectionTime()
    {
        return GetCurrentStageData().StageInspectionTime[_currentStageInspectionTimeIndex];
    }

    private float OnGetLastStageInspectionTime(GetLastStageInspectionTimeEvent _)
    {
        return GetCurrentStageData().StageInspectionTime[^1];
    }
}