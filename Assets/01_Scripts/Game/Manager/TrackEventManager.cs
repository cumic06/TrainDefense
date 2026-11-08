using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic;
using Cumic.Events;
using Cumic.Sequence;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.TrackEvent;
using TrainDefense.Game.UI.TrackEventChoice;

namespace TrainDefense.Game.Manager
{
    /// <summary>
    /// 선로 이벤트 오케스트레이터. 전투 구간이 시작될 때(EngageStartEvent) 분류별로 이벤트를 정하고,
    /// 예약 시각이 되면 효과를 실행하며, 역 도착·게임 종료 때 진행 중인 효과를 강제로 끝낸다.
    /// 분류(TrackEventCategory)마다 칸이 따로 있어 한 구간에 동시에 걸릴 수 있다.
    ///   - 무작위: 구간마다 eventChancePerSegment 확률, 구간 도중 무작위 시각 (기존 방식)
    ///   - 날씨:   누적 상점 수가 weatherStationInterval의 배수인 구간 — 출발과 동시에 확정, 역 도착까지.
    ///             지금 맵에 어울리는 날씨만 뽑는다(TrackEventDefaults.IsAllowedOnStage). 무작위 모래폭풍·한파도 같은 표로 거른다
    ///   - 갈림길: 날씨가 바뀐 구간과 누적 상점 수가 choiceStationInterval의 배수인 구간 — choiceTriggerTimeRatio 시점에 2지선다 팝업.
    ///             날씨 구간이면 그 날씨 짝(TrackEventDefaults.WeatherChoicePairs), 아니면 위험/안전 짝(RiskChoicePairs). 고른 효과는 역 도착까지
    /// 추첨 규칙은 TrackEventScheduler, 효과 내용은 ITrackEventEffect 구현체가 가진다.
    /// 역 도착 시 반드시 끝나므로 런 세이브(역 도착 시점 저장)에 이벤트 상태를 담을 필요가 없다.
    /// </summary>
    public class TrackEventManager : Singleton<TrackEventManager>
    {
        #region Variables

        // 무작위 칸 (치트 패널이 읽는다)
        public TrackEventData ActiveData => _randomSlot.Data;
        public bool IsEventActive => _randomSlot.IsActive;
        public float ActiveElapsedTime => _randomSlot.ElapsedTime;
        public IReadOnlyList<TrackEventData> EventDatas => eventDatas;

        // 날씨·선택 칸
        public TrackEventData ActiveWeatherData => _weatherSlot.Data;
        public TrackEventData ActiveChoiceData => _choiceSlot.Data;
        public bool IsChoicePopupOpen => _choicePopup != null;

        #endregion

        #region Fields

        [SerializeField]
        [BoxGroup("Roll")]
        [Tooltip("전체 on/off. 끄면 추첨 자체를 하지 않는다(날씨·선택 포함).")]
        private bool isEnabled = true;

        [SerializeField]
        [BoxGroup("Roll")]
        [Range(0f, 1f)]
        [Tooltip("전투 구간 하나에서 무작위 이벤트가 터질 확률.")]
        private float eventChancePerSegment = 0.25f;

        [SerializeField]
        [BoxGroup("Roll")]
        [Tooltip("이 누적 상점 수 전에는 이벤트를 아예 뽑지 않는다(튜토리얼·첫 역 보호). 날씨·선택에도 적용된다.")]
        private int firstEventStationCount = 5;

        [SerializeField]
        [BoxGroup("Roll")]
        [Range(0f, 1f)]
        [Tooltip("무작위 이벤트 발동 시각 하한(구간 길이 대비). 출발 직후는 피한다.")]
        private float minTriggerTimeRatio = 0.2f;

        [SerializeField]
        [BoxGroup("Roll")]
        [Range(0f, 1f)]
        [Tooltip("무작위 이벤트 발동 시각 상한(구간 길이 대비). 도착 직전은 피한다.")]
        private float maxTriggerTimeRatio = 0.6f;

        [SerializeField]
        [BoxGroup("Weather")]
        [Min(0)]
        [Tooltip("누적 상점 수가 이 값의 배수인 구간마다 날씨가 확정으로 바뀐다. 0이면 날씨 없음.")]
        private int weatherStationInterval = 4;

        [SerializeField]
        [BoxGroup("Choice")]
        [Min(0)]
        [Tooltip("누적 상점 수가 이 값의 배수인 구간마다 갈림길(위험/안전 짝)이 확정으로 뜬다. 날씨가 바뀐 구간은 이 값과 무관하게 날씨 짝이 뜬다. 0이면 날씨 없는 갈림길 없음.")]
        private int choiceStationInterval = 3;

        [SerializeField]
        [BoxGroup("Choice")]
        [Range(0f, 1f)]
        [Tooltip("갈림길 팝업이 뜨는 시각(구간 길이 대비).")]
        private float choiceTriggerTimeRatio = 0.3f;

        [SerializeField]
        [Tooltip("이벤트 정의 목록. category로 무작위/날씨를 나눈다. 날씨가 하나도 없으면 TrackEventDefaults의 코드 기본값을 쓴다. 갈림길 카드는 코드(TrackEventDefaults)에만 있다.")]
        private TrackEventData[] eventDatas;

        private readonly TrackEventScheduler _scheduler = new();
        private readonly TrackEventScheduler _weatherScheduler = new();
        private readonly TrackEventScheduler _riskScheduler = new();

        private readonly List<TrackEventData> _availableDatas = new();
        private readonly List<TrackEventData> _weatherDatas = new();
        private readonly List<TrackEventData> _choiceOptions = new();
        // 효과 구현이 있는 짝만. 위험/안전 짝은 첫 카드 목록(_riskDatas)으로 추첨하고 같은 순서의 _riskPairs에서 짝을 찾는다.
        private readonly List<ChoicePair> _riskPairs = new();
        private readonly List<TrackEventData> _riskDatas = new();
        private readonly List<ChoicePair> _weatherPairs = new();
        private readonly List<TrackEventData> _randomRollDatas = new();
        private readonly List<TrackEventData> _stageWeatherDatas = new();

        private readonly TrackEventSlot _randomSlot = new(false);
        private readonly TrackEventSlot _weatherSlot = new(true);
        private readonly TrackEventSlot _choiceSlot = new(true);

        private TrackEventChoicePopupUI _choicePopup;
        private float _choiceElapsedTime;
        private float _choiceTriggerTime;
        private bool _isChoiceScheduled;
        private bool _hasRolledThisSegment;
        private bool _isGameOver;

        #endregion

        #region LifeCycle

        private void Start()
        {
            // 정적 배율은 씬을 넘어 남으므로(도메인 리로드 꺼짐 포함) 판 시작마다 원복한다.
            TrackEventModifiers.ResetAll();
            _BuildAvailableDatas();
            _SubscribeEvents();
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();
            _CloseChoicePopup();
        }

        private void FixedUpdate()
        {
            if (_isGameOver)
                return;

            if (InGameSequence.Instance is not { IsRunning: true })
                return;

            float deltaTime = Time.deltaTime;
            _TickSlot(_randomSlot, deltaTime);
            _TickSlot(_weatherSlot, deltaTime);
            _TickSlot(_choiceSlot, deltaTime);
            _TickScheduler(deltaTime);
            _TickChoice(deltaTime);
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOverStart);
            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
            GameEventSystem.Subscribe<SegmentTimeShortenedEvent>(_OnSegmentTimeShortened);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);
            GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
            GameEventSystem.Unsubscribe<SegmentTimeShortenedEvent>(_OnSegmentTimeShortened);
        }

        #endregion

        // 효과 구현이 있는 이벤트만 분류별 후보로 남긴다. 기획만 있고 미구현인 타입이 뽑혀 구간을 낭비하지 않게.
        // 날씨는 인스펙터 목록에 하나도 없으면 코드 기본값으로 채운다. 갈림길 짝은 코드 기본값만 쓴다.
        private void _BuildAvailableDatas()
        {
            _availableDatas.Clear();
            _weatherDatas.Clear();

            if (eventDatas != null)
            {
                foreach (TrackEventData data in eventDatas)
                    _AddIfImplemented(data);
            }

            if (_weatherDatas.Count == 0)
            {
                foreach (TrackEventData data in TrackEventDefaults.WeatherDatas)
                    _AddIfImplemented(data);
            }

            _riskPairs.Clear();
            _riskDatas.Clear();

            foreach (ChoicePair pair in TrackEventDefaults.RiskChoicePairs)
            {
                if (!_IsImplemented(pair))
                    continue;

                _riskPairs.Add(pair);
                _riskDatas.Add(pair.First);
            }

            _weatherPairs.Clear();

            foreach (ChoicePair pair in TrackEventDefaults.WeatherChoicePairs)
            {
                if (_IsImplemented(pair))
                    _weatherPairs.Add(pair);
            }
        }

        private static bool _IsImplemented(ChoicePair pair)
        {
            return _IsImplemented(pair.First) && _IsImplemented(pair.Second);
        }

        private static bool _IsImplemented(TrackEventData data)
        {
            return data != null && TrackEventEffectFactory.Create(data.EventType) != null;
        }

        private void _AddIfImplemented(TrackEventData data)
        {
            if (!_IsImplemented(data))
                return;

            switch (data.Category)
            {
                case TrackEventCategory.Weather:
                    _weatherDatas.Add(data);
                    break;
                case TrackEventCategory.Choice:
                    // 갈림길 카드는 짝으로만 뜨므로(TrackEventDefaults) 인스펙터의 낱장 선택 카드는 쓰지 않는다.
                    break;
                default:
                    _availableDatas.Add(data);
                    break;
            }
        }

        private void _OnGameEnter(GameEnterEvent _)
        {
            _isGameOver = false;
            _hasRolledThisSegment = false;
            _scheduler.Clear();
            _ClearChoiceSchedule();
        }

        // EngageStartEvent는 상점 퇴장·맵 이동·튜토리얼 완료 등 여러 곳에서 오므로, 구간당 한 번만 정한다.
        private void _OnEngageStart(EngageStartEvent _)
        {
            if (_hasRolledThisSegment)
                return;

            _hasRolledThisSegment = true;

            if (!isEnabled)
                return;

            int stationCount = StageManager.Instance != null ? StageManager.Instance.TotalInspectionPassedCount : 0;

            if (stationCount < firstEventStationCount)
                return;

            float segmentDuration = GameEventSystem.Query<GetCurrentInspectionDurationEvent, float>(new GetCurrentInspectionDurationEvent());

            if (_IsIntervalStation(stationCount, weatherStationInterval))
                _StartWeather(stationCount);

            // 날씨가 바뀐 구간은 날씨 짝 갈림길이 뜬다 — 날씨 간격과 갈림길 간격이 겹치지 않는 역에서도 빠지지 않게.
            if (_weatherSlot.IsActive || _IsIntervalStation(stationCount, choiceStationInterval))
                _ScheduleChoice(segmentDuration);

            _CollectRandomRollDatas();

            if (_randomRollDatas.Count > 0)
            {
                _scheduler.RollSegment(_randomRollDatas, stationCount, segmentDuration,
                                       eventChancePerSegment, minTriggerTimeRatio, maxTriggerTimeRatio);
            }
        }

        // 날씨와 같은 종류의 무작위 이벤트(모래폭풍·한파)는 그 구간에 뽑지 않는다 — 같은 효과가 겹쳐 커지는 것을 막는다.
        // 맵에 어울리지 않는 날씨류 이벤트(해안의 모래폭풍 등)도 뺀다.
        private void _CollectRandomRollDatas()
        {
            _randomRollDatas.Clear();
            TrackEventType weatherType = _weatherSlot.IsActive ? _weatherSlot.Data.EventType : TrackEventType.None;
            string stageId = _GetCurrentStageId();

            foreach (TrackEventData data in _availableDatas)
            {
                if (data.EventType == weatherType)
                    continue;

                if (!TrackEventDefaults.IsAllowedOnStage(stageId, data.EventType))
                    continue;

                _randomRollDatas.Add(data);
            }
        }

        // 지금 달리는 맵의 날씨 후보만 모은다. 맵에 맞는 날씨가 하나도 없으면 비어 있고, 그 구간은 날씨 없이 지나간다.
        private void _CollectStageWeatherDatas()
        {
            _stageWeatherDatas.Clear();
            string stageId = _GetCurrentStageId();

            foreach (TrackEventData data in _weatherDatas)
            {
                if (TrackEventDefaults.IsAllowedOnStage(stageId, data.EventType))
                    _stageWeatherDatas.Add(data);
            }
        }

        private static string _GetCurrentStageId()
        {
            StageManager stageManager = StageManager.Instance;

            if (stageManager == null)
                return null;

            return stageManager.CurrentStageData?.Id;
        }

        private void _OnInspectionStart(InspectionStartEvent _)
        {
            _scheduler.Clear();
            _ClearChoiceSchedule();
            _EndAllSlots(true);
            _hasRolledThisSegment = false;
        }

        // 순풍 운행·엔진 과부하로 구간이 짧아지면 아직 안 터진 무작위 이벤트·갈림길 시각도 같은 비율로 당긴다.
        private void _OnSegmentTimeShortened(SegmentTimeShortenedEvent shortenedEvent)
        {
            _scheduler.CompressRemaining(shortenedEvent.Ratio);

            if (_isChoiceScheduled && _choiceTriggerTime > _choiceElapsedTime)
                _choiceTriggerTime = _choiceElapsedTime + (_choiceTriggerTime - _choiceElapsedTime) * (1f - shortenedEvent.Ratio);
        }

        private void _OnGameOverStart(GameOverStartEvent _) => _StopAll();

        private void _OnGameEnd(GameEndEvent _) => _StopAll();

        private void _StopAll()
        {
            _isGameOver = true;
            _scheduler.Clear();
            _ClearChoiceSchedule();
            _EndAllSlots(true);
        }

        private static bool _IsIntervalStation(int stationCount, int interval)
        {
            return interval > 0 && stationCount % interval == 0;
        }

        private void _StartWeather(int stationCount)
        {
            _CollectStageWeatherDatas();
            TrackEventData weather = _weatherScheduler.PickOne(_stageWeatherDatas, stationCount);

            if (weather == null)
                return;

            _StartSlot(_weatherSlot, weather);
        }

        private void _ScheduleChoice(float segmentDuration)
        {
            _ClearChoiceSchedule();

            if (segmentDuration <= 0f)
                return;

            _isChoiceScheduled = true;
            _choiceTriggerTime = segmentDuration * choiceTriggerTimeRatio;
        }

        private void _ClearChoiceSchedule()
        {
            _isChoiceScheduled = false;
            _choiceElapsedTime = 0f;
            _choiceTriggerTime = 0f;
            _CloseChoicePopup();
        }

        private void _TickChoice(float deltaTime)
        {
            if (!_isChoiceScheduled)
                return;

            _choiceElapsedTime += deltaTime;

            if (_choiceElapsedTime < _choiceTriggerTime)
                return;

            _isChoiceScheduled = false;
            _OpenChoicePopup();
        }

        // 팝업이 떠 있는 동안 OverlayPhase.TrackEventChoice로 전투를 멈춘다. 고르면 오버레이를 내리고 효과를 시작한다.
        // 날씨 구간이면 그 날씨 짝, 아니면(또는 그 날씨에 짝이 없으면) 위험/안전 짝을 띄운다.
        private void _OpenChoicePopup()
        {
            int stationCount = StageManager.Instance != null ? StageManager.Instance.TotalInspectionPassedCount : 0;

            if (!_PickWeatherOptions(stationCount) && !_PickRiskOptions(stationCount))
                return;

            _choicePopup = TrackEventChoicePopupUI.Open(_choiceOptions, _OnChoiceSelected);

            if (_choicePopup == null)
                return;

            _SetChoiceOverlay(true);
        }

        // 진행 중인 날씨의 짝 중 등장 역 조건을 넘긴 가장 늦은 짝을 담는다(폭염은 엘리트가 나오는 역부터 엘리트 카드 짝).
        private bool _PickWeatherOptions(int stationCount)
        {
            if (!_weatherSlot.IsActive)
                return false;

            TrackEventType weatherType = _weatherSlot.Data.EventType;
            bool hasPicked = false;
            ChoicePair picked = default;

            foreach (ChoicePair pair in _weatherPairs)
            {
                if (pair.WeatherType != weatherType || pair.MinStationCount > stationCount)
                    continue;

                if (hasPicked && pair.MinStationCount <= picked.MinStationCount)
                    continue;

                picked = pair;
                hasPicked = true;
            }

            if (!hasPicked)
                return false;

            _SetOptions(picked);

            return true;
        }

        // 위험 카드 하나를 뽑고(직전 짝은 연속으로 안 나옴, 카드의 등장 역 조건 반영) 그 짝을 위험·안전 순서로 담는다.
        private bool _PickRiskOptions(int stationCount)
        {
            TrackEventData risk = _riskScheduler.PickOne(_riskDatas, stationCount);
            int pairIndex = risk != null ? _riskDatas.IndexOf(risk) : -1;

            if (pairIndex < 0)
                return false;

            _SetOptions(_riskPairs[pairIndex]);

            return true;
        }

        private void _SetOptions(ChoicePair pair)
        {
            _choiceOptions.Clear();
            _choiceOptions.Add(pair.First);
            _choiceOptions.Add(pair.Second);
        }

        private void _OnChoiceSelected(TrackEventData selected)
        {
            _choicePopup = null;
            _SetChoiceOverlay(false);

            if (_isGameOver || selected == null)
                return;

            _StartSlot(_choiceSlot, selected);
        }

        // 고르지 않은 채 구간이 끝나는 경우(역 도착·게임 종료) 팝업을 닫고 오버레이도 내린다.
        private void _CloseChoicePopup()
        {
            if (_choicePopup == null)
                return;

            _choicePopup.Close();
            _choicePopup = null;
            _SetChoiceOverlay(false);
        }

        private static void _SetChoiceOverlay(bool isOpen)
        {
            InGameSequence sequence = InGameSequence.Instance;

            if (sequence == null)
                return;

            if (isOpen)
                sequence.PushOverlay(OverlayPhase.TrackEventChoice);
            else
                sequence.PopOverlay(OverlayPhase.TrackEventChoice);
        }

        private void _TickScheduler(float deltaTime)
        {
            TrackEventData triggered = _scheduler.Tick(deltaTime);

            if (triggered == null)
                return;

            _StartSlot(_randomSlot, triggered);
        }

        private void _TickSlot(TrackEventSlot slot, float deltaTime)
        {
            if (slot.Tick(deltaTime))
                _EndSlot(slot, false);
        }

        private void _StartSlot(TrackEventSlot slot, TrackEventData data)
        {
            // 한 칸에 하나만 진행하므로 겹칠 일은 없지만, 치트 등으로 강제 발동할 때를 대비해 이전 것을 먼저 닫는다.
            _EndSlot(slot, true);

            if (!slot.Begin(data))
                return;

            GameEventSystem.Publish(new TrackEventStartEvent(data));
        }

        private void _EndSlot(TrackEventSlot slot, bool isInterrupted)
        {
            TrackEventData endedData = slot.End(isInterrupted);

            if (endedData == null)
                return;

            GameEventSystem.Publish(new TrackEventEndEvent(endedData, isInterrupted));
        }

        private void _EndAllSlots(bool isInterrupted)
        {
            _EndSlot(_randomSlot, isInterrupted);
            _EndSlot(_weatherSlot, isInterrupted);
            _EndSlot(_choiceSlot, isInterrupted);
        }

        /// <summary>치트·테스트용 강제 발동. 등장 조건·확률을 무시하고 즉시 시작한다. 날씨·선택 데이터는 각자의 칸에서 시작한다.</summary>
        public void ForceStartEvent(TrackEventData data)
        {
            if (data == null)
                return;

            switch (data.Category)
            {
                case TrackEventCategory.Weather:
                    _StartSlot(_weatherSlot, data);
                    break;
                case TrackEventCategory.Choice:
                    _StartSlot(_choiceSlot, data);
                    break;
                default:
                    _StartSlot(_randomSlot, data);
                    break;
            }
        }

        /// <summary>치트·테스트용 강제 종료. 진행 중인 무작위 이벤트를 지속 시간 만료처럼 끝낸다.</summary>
        public void ForceEndEvent()
        {
            _EndSlot(_randomSlot, false);
        }

        /// <summary>치트·테스트용. 지금 바로 날씨를 하나 정한다.</summary>
        public void ForceStartWeather()
        {
            int stationCount = StageManager.Instance != null ? StageManager.Instance.TotalInspectionPassedCount : 0;
            _StartWeather(stationCount);
        }

        /// <summary>치트·테스트용. 지금 바로 갈림길 팝업을 띄운다(날씨 구간이면 날씨 짝, 아니면 위험/안전 짝).</summary>
        public void ForceOpenChoice()
        {
            if (_choicePopup != null)
                return;

            _isChoiceScheduled = false;
            _OpenChoicePopup();
        }
    }
}
