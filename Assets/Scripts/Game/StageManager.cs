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
        GameEventSystem.Subscribe<GetNextInspectionRemainingTimeEvent, float>(GetNextInspectionRemainingTime);
    }

    private void OnDestroy()
    {
        GameEventSystem.Unsubscribe<GetNextInspectionRemainingTimeEvent, float>(GetNextInspectionRemainingTime);
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
        CurrentStageTimeUp();

        if (_currentStageTime >= GetCurrentStageInspectionTime() && _currentStageInspectionTimeIndex < GetCurrentStageData().StageInspectionTime.Length)
        {
            _currentStageInspectionTimeIndex++;
            GameEventSystem.Publish(new InspectionEvent());
        }
        else if (_currentStageTime >= GetCurrentStageData().StageEndTime)
        {
            GameEventSystem.Publish(new StageEndEvent(true));
        }
    }

    private void CurrentStageTimeUp()
    {
        _currentStageTime += Time.deltaTime;

        float nextInspectionRemainingtime = GetCurrentStageInspectionTime() - _currentStageTime;
        GameEventSystem.Publish(new ChangeStageTimeEvent(nextInspectionRemainingtime));
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

    private float GetNextInspectionRemainingTime(GetNextInspectionRemainingTimeEvent getNextInspectionRemainingTimeEvent)
    {
        return GetCurrentStageData().StageInspectionTime[_currentStageInspectionTimeIndex] - _currentStageTime;
    }
}