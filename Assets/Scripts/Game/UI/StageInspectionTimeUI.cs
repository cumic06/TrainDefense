using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.UI;

public class StageInspectionTimeUI : MonoBehaviour
{
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void Start()
    {
        GameEventSystem.Subscribe<ChangeStageTimeEvent>(OnChangeStageTime);
        ResetSliderValue();
        SetMaxValue();
    }

    private void ResetSliderValue()
    {
        _slider.value = 0;
    }

    private void SetMaxValue()
    {
        float stageEndTime = GameEventSystem.Query<GetStageEndTimeEvent, float>(new GetStageEndTimeEvent());
        _slider.maxValue = stageEndTime;
    }

    private void OnChangeStageTime(ChangeStageTimeEvent changeStageTimeEvent)
    {
        _slider.value = changeStageTimeEvent.StageTime;
    }
}
