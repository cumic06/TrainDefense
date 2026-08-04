using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Events;
using TrainDefense.Game.Events;
using System.Collections;

namespace TrainDefense.Game.UI
{
   public class StageInspectionTimeUI : MonoBehaviour
   {
      #region Variable

      private Slider _slider;
      private TextMeshProUGUI _nextInspectionTimeText;
      private float _maxTime;
      private bool _isActive;
      #endregion


      private void Awake()
      {
         _slider = GetComponent<Slider>();
         _nextInspectionTimeText = GetComponentInChildren<TextMeshProUGUI>();
      }

      private void Start()
      {
         _SubscribeEvents();
         StartCoroutine(ResetSliderValue());
      }

      private void OnDestroy()
      {
         _UnsubscribeEvents();
      }

      private void _SubscribeEvents()
      {
         GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Subscribe<EngageReadyEvent>(_OnEngageReady);
         GameEventSystem.Subscribe<InspectionStartEvent>(SetMaxValue);
         GameEventSystem.Subscribe<ChangeStageTimeEvent>(OnChangeStageTime);
         GameEventSystem.Subscribe<StageSelectEvent>(OnStageSelected);
         GameEventSystem.Subscribe<EngageStartEvent>(OnEngageStart);
      }

      private void _UnsubscribeEvents()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Unsubscribe<EngageReadyEvent>(_OnEngageReady);
         GameEventSystem.Unsubscribe<InspectionStartEvent>(SetMaxValue);
         GameEventSystem.Unsubscribe<ChangeStageTimeEvent>(OnChangeStageTime);
         GameEventSystem.Unsubscribe<StageSelectEvent>(OnStageSelected);
         GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
      }

      private void _OnGameEnter(GameEnterEvent _)
      {
         _isActive = false;
         _maxTime = 0f;
         _slider.value = 0f;
      }

      private void _OnEngageReady(EngageReadyEvent _)
      {
         _isActive = false;
         _maxTime = 0f;
         _slider.value = 0f;
      }

      private void OnEngageStart(EngageStartEvent engageStartEvent)
      {
         _isActive = true;
         _maxTime = 0f;
         StartCoroutine(ResetSliderValue());
      }

      private IEnumerator ResetSliderValue()
      {
         yield return null;
         _slider.value = 0;
      }

      private void ResetMaxValue()
      {
         _maxTime = 0f;
         _slider.maxValue = 1;
      }

      private void SetMaxValue(InspectionStartEvent inspectionStartEvent)
      {
         _isActive = true;
         RefreshMaxValue();
         StartCoroutine(ResetSliderValue());
      }

      private void OnStageSelected(StageSelectEvent stageSelectEvent)
      {
         _maxTime = 0f;
         StartCoroutine(ResetSliderValue());
      }

      private void RefreshMaxValue()
      {
         _maxTime = GameEventSystem.Query<GetCurrentInspectionDurationEvent, float>(new GetCurrentInspectionDurationEvent());

         if (_maxTime <= 0f)
         {
            ResetMaxValue();
            StartCoroutine(ResetSliderValue());
            return;
         }

         _slider.maxValue = 1f;
      }

      private void OnChangeStageTime(ChangeStageTimeEvent changeStageTimeEvent)
      {
         if (!_isActive) return;

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
   }
}
