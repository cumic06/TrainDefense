using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;
using System.Collections;

namespace TrainDefense.Game.UI
{
   public class StageInspectionTimeUI : MonoBehaviour
   {
      #region Variable

      // 바 왼쪽 역 배지 글자. "지금 향하는 역 번호/종착역"이고, 무한 모드면 번호만 쓴다. 숫자만이라 언어 공통.
      [SerializeField]
      private TMP_Text stationText;
      // 남은 시간 글자. 자식 TMP가 둘(남은 시간·역 배지)이라 계층 순서에 기대지 않고 직접 연결한다.
      [SerializeField]
      private TextMeshProUGUI nextInspectionTimeText;

      private Slider _slider;
      private float _maxTime;
      private bool _isActive;
      private int _lastShownRemainingSeconds = -1;
      #endregion


      private void Awake()
      {
         _slider = GetComponent<Slider>();
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
         GameEventSystem.Subscribe<StationPassedEvent>(_OnStationPassed);
         GameEventSystem.Subscribe<RunRestoredEvent>(_OnRunRestored);
      }

      private void _UnsubscribeEvents()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Unsubscribe<EngageReadyEvent>(_OnEngageReady);
         GameEventSystem.Unsubscribe<InspectionStartEvent>(SetMaxValue);
         GameEventSystem.Unsubscribe<ChangeStageTimeEvent>(OnChangeStageTime);
         GameEventSystem.Unsubscribe<StageSelectEvent>(OnStageSelected);
         GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
         GameEventSystem.Unsubscribe<StationPassedEvent>(_OnStationPassed);
         GameEventSystem.Unsubscribe<RunRestoredEvent>(_OnRunRestored);
      }

      private void _OnGameEnter(GameEnterEvent _)
      {
         _isActive = false;
         _maxTime = 0f;
         _slider.value = 0f;
         _SetRemainingTimeText(0f);
         // StageManager도 같은 이벤트에서 무한 모드를 리셋하는데 구독 순서가 불확정이라, 매니저 상태를 읽지 않고 첫 역으로 고정한다.
         _SetStationText(1, false);
      }

      private void _OnStationPassed(StationPassedEvent _) => _RefreshStationText();

      // 이어하기 복원은 StationPassedEvent 없이 지나온 역 수를 바로 바꾸므로 여기서 다시 맞춘다.
      private void _OnRunRestored(RunRestoredEvent _) => _RefreshStationText();

      // 지나온 역 수와 무한 모드는 StageManager 한 곳에서만 읽는다(이벤트 값과 복원 값이 어긋나지 않게).
      private void _RefreshStationText()
      {
         var stageManager = StageManager.Instance;
         if (stageManager == null) return;

         _SetStationText(stageManager.TotalInspectionPassedCount + 1, stageManager.IsInfiniteMode);
      }

      // "향하는 역/종착역". 종착역에 닿은 뒤(결산·무한 모드 선택 중)에도 종착역 번호를 넘기지 않고, 무한 모드면 번호만 쓴다.
      private void _SetStationText(int stationNumber, bool isInfiniteMode)
      {
         if (stationText == null) return;

         int finalStationCount = StageManager.Instance != null ? StageManager.Instance.FinalStationCount : 0;
         if (isInfiniteMode || finalStationCount <= 0)
         {
            stationText.text = stationNumber.ToString();
            return;
         }

         stationText.text = $"{Mathf.Min(stationNumber, finalStationCount)}/{finalStationCount}";
      }

      private void _OnEngageReady(EngageReadyEvent _)
      {
         _isActive = false;
         _maxTime = 0f;
         _slider.value = 0f;
         _SetRemainingTimeText(0f);
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
         _SetRemainingTimeText(0f);
      }

      private void OnStageSelected(StageSelectEvent stageSelectEvent)
      {
         _maxTime = 0f;
         StartCoroutine(ResetSliderValue());
         _SetRemainingTimeText(0f);
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
            _SetRemainingTimeText(changeStageTimeEvent.StageTime);
         }
         else
         {
            _slider.value = 0;
            _SetRemainingTimeText(0f);
         }
      }

      private void _SetRemainingTimeText(float remainingSeconds)
      {
         if (nextInspectionTimeText == null) return;

         int remainingSecondsCeiled = remainingSeconds > 0f ? Mathf.CeilToInt(remainingSeconds) : 0;
         if (remainingSecondsCeiled == _lastShownRemainingSeconds) return;

         _lastShownRemainingSeconds = remainingSecondsCeiled;
         nextInspectionTimeText.text = remainingSecondsCeiled > 0
            ? $"{remainingSecondsCeiled / 60:00}:{remainingSecondsCeiled % 60:00}"
            : string.Empty;
      }
   }
}
