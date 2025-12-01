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
        GameEventSystem.Subscribe<InspectionEvent>(SetMaxValue);
        GameEventSystem.Subscribe<ChangeStageTimeEvent>(OnChangeStageTime);

        ResetSliderValue();
        SetMaxValue(null);
    }

    private void OnDestroy()
    {
        GameEventSystem.Unsubscribe<InspectionEvent>(SetMaxValue);
        GameEventSystem.Unsubscribe<ChangeStageTimeEvent>(OnChangeStageTime);
    }

    private void ResetSliderValue()
    {
        _slider.value = 0;
    }

    private void ResetMaxValue()
    {
        _slider.maxValue = 0;
    }

    private void SetMaxValue(InspectionEvent inspectionEvent)
    {
        float nextInspectionTime = GameEventSystem.Query<GetNextInspectionRemainingTimeEvent, float>(new GetNextInspectionRemainingTimeEvent());
        _slider.maxValue = nextInspectionTime;
    }

    private void OnChangeStageTime(ChangeStageTimeEvent changeStageTimeEvent)
    {
        _nextInspectionTimeText.text = $"Next Inspection Time : {changeStageTimeEvent.StageTime:F1}";
        _slider.value = changeStageTimeEvent.StageTime;
    }
}
