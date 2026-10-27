using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic;
using Cumic.Events;
using Cumic.Sequence;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.TrackEvent;

namespace TrainDefense.Game.Manager
{
    /// <summary>
    /// 선로 이벤트 오케스트레이터. 전투 구간이 시작될 때(EngageStartEvent) 추첨을 맡기고,
    /// 예약 시각이 되면 효과를 실행하며, 역 도착·게임 종료 때 진행 중인 효과를 강제로 끝낸다.
    /// 추첨 규칙은 TrackEventScheduler, 효과 내용은 ITrackEventEffect 구현체가 가진다.
    /// 역 도착 시 반드시 끝나므로 런 세이브(역 도착 시점 저장)에 이벤트 상태를 담을 필요가 없다.
    /// </summary>
    public class TrackEventManager : Singleton<TrackEventManager>
    {
        #region Variables

        public TrackEventData ActiveData => _activeData;
        public bool IsEventActive => _activeEffect != null;
        public float ActiveElapsedTime => _elapsedTime;
        public IReadOnlyList<TrackEventData> EventDatas => eventDatas;

        #endregion

        #region Fields

        [SerializeField]
        [BoxGroup("Roll")]
        [Tooltip("전체 on/off. 끄면 추첨 자체를 하지 않는다.")]
        private bool isEnabled = true;

        [SerializeField]
        [BoxGroup("Roll")]
        [Range(0f, 1f)]
        [Tooltip("전투 구간 하나에서 이벤트가 터질 확률.")]
        private float eventChancePerSegment = 0.35f;

        [SerializeField]
        [BoxGroup("Roll")]
        [Tooltip("이 누적 상점 수 전에는 이벤트를 아예 뽑지 않는다(튜토리얼·첫 역 보호).")]
        private int firstEventStationCount = 2;

        [SerializeField]
        [BoxGroup("Roll")]
        [Range(0f, 1f)]
        [Tooltip("발동 시각 하한(구간 길이 대비). 출발 직후는 피한다.")]
        private float minTriggerTimeRatio = 0.2f;

        [SerializeField]
        [BoxGroup("Roll")]
        [Range(0f, 1f)]
        [Tooltip("발동 시각 상한(구간 길이 대비). 도착 직전은 피한다.")]
        private float maxTriggerTimeRatio = 0.6f;

        [SerializeField]
        [Tooltip("이벤트 정의 목록. 종류가 늘면 DB + 엑셀 임포터로 옮긴다.")]
        private TrackEventData[] eventDatas;

        private readonly TrackEventScheduler _scheduler = new();
        private readonly List<TrackEventData> _availableDatas = new();
        private TrackEventData _activeData;
        private ITrackEventEffect _activeEffect;
        private float _elapsedTime;
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
        }

        private void FixedUpdate()
        {
            if (_isGameOver)
                return;

            if (InGameSequence.Instance is not { IsRunning: true })
                return;

            float deltaTime = Time.deltaTime;
            _TickActiveEffect(deltaTime);
            _TickScheduler(deltaTime);
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
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);
            GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
        }

        #endregion

        // 효과 구현이 있는 이벤트만 추첨 후보로 남긴다. 기획만 있고 미구현인 타입이 뽑혀 구간을 낭비하지 않게.
        private void _BuildAvailableDatas()
        {
            _availableDatas.Clear();

            if (eventDatas == null)
                return;

            foreach (TrackEventData data in eventDatas)
            {
                if (data == null || TrackEventEffectFactory.Create(data.EventType) == null)
                    continue;

                _availableDatas.Add(data);
            }
        }

        private void _OnGameEnter(GameEnterEvent _)
        {
            _isGameOver = false;
            _hasRolledThisSegment = false;
            _scheduler.Clear();
        }

        // EngageStartEvent는 상점 퇴장·맵 이동·튜토리얼 완료 등 여러 곳에서 오므로, 구간당 한 번만 추첨한다.
        private void _OnEngageStart(EngageStartEvent _)
        {
            if (_hasRolledThisSegment)
                return;

            _hasRolledThisSegment = true;

            if (!isEnabled || _availableDatas.Count == 0)
                return;

            int stationCount = StageManager.Instance != null ? StageManager.Instance.TotalInspectionPassedCount : 0;

            if (stationCount < firstEventStationCount)
                return;

            float segmentDuration = GameEventSystem.Query<GetCurrentInspectionDurationEvent, float>(new GetCurrentInspectionDurationEvent());
            _scheduler.RollSegment(_availableDatas, stationCount, segmentDuration,
                                   eventChancePerSegment, minTriggerTimeRatio, maxTriggerTimeRatio);
        }

        private void _OnInspectionStart(InspectionStartEvent _)
        {
            _scheduler.Clear();
            _EndActiveEvent(true);
            _hasRolledThisSegment = false;
        }

        private void _OnGameOverStart(GameOverStartEvent _) => _StopAll();

        private void _OnGameEnd(GameEndEvent _) => _StopAll();

        private void _StopAll()
        {
            _isGameOver = true;
            _scheduler.Clear();
            _EndActiveEvent(true);
        }

        private void _TickScheduler(float deltaTime)
        {
            TrackEventData triggered = _scheduler.Tick(deltaTime);

            if (triggered == null)
                return;

            _StartEvent(triggered);
        }

        private void _TickActiveEffect(float deltaTime)
        {
            if (_activeEffect == null)
                return;

            _activeEffect.Tick(deltaTime);
            _elapsedTime += deltaTime;

            if (!_activeData.IsUntilStation && _elapsedTime >= _activeData.Duration)
                _EndActiveEvent(false);
        }

        private void _StartEvent(TrackEventData data)
        {
            // 한 구간에 하나만 예약하므로 겹칠 일은 없지만, 치트 등으로 강제 발동할 때를 대비해 이전 것을 먼저 닫는다.
            _EndActiveEvent(true);

            ITrackEventEffect effect = TrackEventEffectFactory.Create(data.EventType);

            if (effect == null)
                return;

            _activeData = data;
            _activeEffect = effect;
            _elapsedTime = 0f;

            _activeEffect.Begin(data);
            GameEventSystem.Publish(new TrackEventStartEvent(data));
        }

        private void _EndActiveEvent(bool isInterrupted)
        {
            if (_activeEffect == null)
                return;

            TrackEventData endedData = _activeData;
            _activeEffect.End(isInterrupted);
            _activeEffect = null;
            _activeData = null;

            GameEventSystem.Publish(new TrackEventEndEvent(endedData, isInterrupted));
        }

        /// <summary>치트·테스트용 강제 발동. 등장 조건·확률을 무시하고 즉시 시작한다.</summary>
        public void ForceStartEvent(TrackEventData data)
        {
            if (data == null)
                return;

            _StartEvent(data);
        }

        /// <summary>치트·테스트용 강제 종료. 진행 중인 효과를 지속 시간 만료처럼 끝낸다.</summary>
        public void ForceEndEvent()
        {
            _EndActiveEvent(false);
        }
    }
}
