using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
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
        _SubscribeEvents();
        ResetSliderValue();
    }

    private void OnDestroy()
    {
        _UnsubscribeEvents();
    }

    private void _SubscribeEvents()
    {
        GameEventSystem.Subscribe<InspectionStartEvent>(SetMaxValue);
        GameEventSystem.Subscribe<ChangeStageTimeEvent>(OnChangeStageTime);
    }

    private void _UnsubscribeEvents()
    {
        GameEventSystem.Unsubscribe<InspectionStartEvent>(SetMaxValue);
        GameEventSystem.Unsubscribe<ChangeStageTimeEvent>(OnChangeStageTime);
    }

    private void ResetSliderValue()
    {
        _slider.value = 0;
    }

    private void ResetMaxValue()
    {
        _maxTime = 0f;
        _slider.maxValue = 1;
    }

    private void SetMaxValue(InspectionStartEvent inspectionStartEvent)
    {
        RefreshMaxValue();
    }

    private void RefreshMaxValue()
    {
        _maxTime = GameEventSystem.Query<GetCurrentInspectionDurationEvent, float>(new GetCurrentInspectionDurationEvent());

        if (_maxTime <= 0f)
        {
            ResetMaxValue();
            ResetSliderValue();
            return;
        }

        _slider.maxValue = 1f;
    }

    private void OnChangeStageTime(ChangeStageTimeEvent changeStageTimeEvent)
    {
        if (_maxTime <= 0f)
        {
            RefreshMaxValue();
        }

        if (_maxTime > 0)
        {
            float remainingTime = 1f - (changeStageTimeEvent.StageTime / _maxTime);
            _slider.value = Mathf.Clamp01(remainingTime);
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
}
