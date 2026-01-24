using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game;
using TrainDefense.Game.Datas;

public class StageManager : Singleton<StageManager>
{
    #region Variables

    #region Fields
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
    #endregion
    
    private void Start()
    {
        LoadStageDatas();
        ResetCurrentStageInfo();

        GameEventSystem.Subscribe<GetNextInspectionRemainingTimeEvent, float>(GetNextInspectionRemainingTime);
    }

    private void OnDestroy()
    {
        GameEventSystem.Unsubscribe<GetNextInspectionRemainingTimeEvent, float>(GetNextInspectionRemainingTime);
    }

    private void LoadStageDatas()
    {
        _stageDatas = DatabaseManager.Instance.GetStageDatas();
    }

    private void ResetCurrentStageInfo()
    {
        _currentStageIndex = 0;
        _currentStageTime = 0;
        _currentStageInspectionTimeIndex = 0;
    }

    private void Update()
    {
        CurrentStageTimeUp();

        StageHandler();
    }

    private void StageHandler()
    {
        if (_currentStageTime >= GetCurrentStageInspectionTime() && _currentStageInspectionTimeIndex < GetCurrentStageData().StageInspectionTime.Length)
        {
            CurrentStageInpectionUp();
        }
        else if (_currentStageTime >= GetCurrentStageData().StageEndTime)
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
        if (_currentStageInspectionTimeIndex >= GetCurrentStageData().StageInspectionTime.Length)
        {
            return GetCurrentStageData().StageInspectionTime[^1] - _currentStageTime;
        }
        return GetCurrentStageData().StageInspectionTime[_currentStageInspectionTimeIndex] - _currentStageTime;
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