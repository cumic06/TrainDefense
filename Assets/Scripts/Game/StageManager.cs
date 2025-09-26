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
        GameEventSystem.Subscribe<GetStageEndTimeEvent, float>(GetCurrentStageEndTime);
    }

    private void OnDestroy()
    {
        GameEventSystem.Unsubscribe<GetStageEndTimeEvent, float>(GetCurrentStageEndTime);
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

        if (_currentStageTime >= GetCurrentStageInspectionTime() && _currentStageInspectionTimeIndex < GetCurrentStageData().StageInspectionTime.Length)
        {
            _currentStageInspectionTimeIndex++;
            GameEventSystem.Publish(new EngageReadyEvent());
        }
        else if (_currentStageTime >= GetCurrentStageData().StageEndTime)
        {
            GameEventSystem.Publish(new StageEndEvent(true));
        }
    }

    private StageData GetCurrentStageData()
    {
        return _stageDatas[_currentStageIndex];
    }

    private float GetCurrentStageInspectionTime()
    {
        if (_currentStageInspectionTimeIndex >= GetCurrentStageData().StageInspectionTime.Length)
        {
            return GetCurrentStageData().StageInspectionTime[^1];
        }

        return GetCurrentStageData().StageInspectionTime[_currentStageInspectionTimeIndex];
    }

    private float GetCurrentStageEndTime(GetStageEndTimeEvent getLastStageInspectionTimeEvent)
    {
        return GetCurrentStageData().StageEndTime;
    }
}