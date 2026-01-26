using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Events;
using TrainDefense.Game.Events;

public class StageInspectionTimeUI : MonoBehaviour
{
    #region Variable

    #region Fields
    [SerializeField]
    private Image stationIcon;
    #endregion

    private Slider _slider;
    private TextMeshProUGUI _nextInspectionTimeText;
    private float _maxTime;
    #endregion


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
        _slider.maxValue = 1;
    }

    private void SetMaxValue(InspectionEvent inspectionEvent)
    {
        _maxTime = GameEventSystem.Query<GetNextInspectionRemainingTimeEvent, float>(new GetNextInspectionRemainingTimeEvent());
        _slider.maxValue = 1;
    }

    private void OnChangeStageTime(ChangeStageTimeEvent changeStageTimeEvent)
    {
        // _nextInspectionTimeText.text = $"Next Inspection Time : {changeStageTimeEvent.StageTime:F1}";

        if (_maxTime > 0)
        {
            _slider.value = 1f - (changeStageTimeEvent.StageTime / _maxTime);
        }
        else
        {
            _slider.value = 0;
        }
    }

    private void SetStationIcon(Sprite sprite)
    {
        if (stationIcon != null)
        {
            stationIcon.sprite = sprite;
        }
    }
}
