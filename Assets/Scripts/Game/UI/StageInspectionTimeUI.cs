using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Events;
using TrainDefense.Game.Events;

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
        ResetMaxValue();
    }

    private void ResetSliderValue()
    {
        _slider.value = 0;
    }

    private void ResetMaxValue()
    {
        _slider.maxValue = 0;
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
