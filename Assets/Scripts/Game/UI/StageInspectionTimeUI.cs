using Cumic.Events;
using TMPro;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.UI;

public class StageInspectionTimeUI : MonoBehaviour
{
    private Slider _slider;
    private TextMeshProUGUI _nextInspectionTimeText;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _nextInspectionTimeText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        GameEventSystem.Subscribe<ChangeStageTimeEvent>(OnChangeStageTime);
        GameEventSystem.Subscribe<EngageReadyEvent>(SetMaxValue);
        ResetSliderValue();
        SetMaxValue(null);
    }

    private void ResetSliderValue()
    {
        _slider.value = 0;
    }

    private void SetMaxValue(EngageReadyEvent engageReadyEvent)
    {
        ResetSliderValue();
        float nextInspectionTime = GameEventSystem.Query<GetNextInspectionRemainingTimeEvent, float>(new GetNextInspectionRemainingTimeEvent());
        _slider.maxValue = nextInspectionTime;
    }

    private void OnChangeStageTime(ChangeStageTimeEvent changeStageTimeEvent)
    {
        _nextInspectionTimeText.text = $"Next Inspection Time : {changeStageTimeEvent.StageTime:F1}";
        _slider.value = changeStageTimeEvent.StageTime;
    }
}
