using System;
using System.Collections.Generic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 선로 이벤트 추첨 코어 (순수 C# — Unity 없이 테스트 가능).
    /// 전투 구간마다 "이번 구간에 이벤트를 터뜨릴지 / 언제 / 무엇을"만 정한다.
    /// 효과 실행·이벤트 발행은 TrackEventManager(어댑터)의 몫이다.
    /// </summary>
    public class TrackEventScheduler
    {
        #region Variables
        private readonly Random _random;
        private readonly List<TrackEventData> _candidates = new();

        private TrackEventData _scheduledData;
        private float _elapsedTime;
        private float _triggerTime;
        private string _lastTriggeredId;
        #endregion

        public bool HasScheduled => _scheduledData != null;

        public TrackEventScheduler(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// 새 전투 구간 시작 시 한 번 부른다. chance 확률로 이벤트 하나를 예약한다.
        /// 발동 시각은 구간 길이의 [minTimeRatio, maxTimeRatio] 사이 — 출발 직후·도착 직전은 피한다.
        /// </summary>
        public void RollSegment(IReadOnlyList<TrackEventData> pool, int stationCount, float segmentDuration,
                                float chance, float minTimeRatio, float maxTimeRatio)
        {
            Clear();

            if (pool == null || segmentDuration <= 0f)
                return;

            if (_random.NextDouble() >= chance)
                return;

            TrackEventData picked = _PickWeighted(pool, stationCount);

            if (picked == null)
                return;

            float ratio = minTimeRatio + (float)_random.NextDouble() * (maxTimeRatio - minTimeRatio);
            _scheduledData = picked;
            _triggerTime = segmentDuration * ratio;
        }

        /// <summary>시간을 흘린다. 예약 시각에 닿은 프레임에만 발동할 이벤트를 돌려주고 예약을 비운다.</summary>
        public TrackEventData Tick(float deltaTime)
        {
            if (_scheduledData == null)
                return null;

            _elapsedTime += deltaTime;

            if (_elapsedTime < _triggerTime)
                return null;

            TrackEventData triggered = _scheduledData;
            _lastTriggeredId = triggered.Id;
            Clear();

            return triggered;
        }

        public void Clear()
        {
            _scheduledData = null;
            _elapsedTime = 0f;
            _triggerTime = 0f;
        }

        // 등장 조건(누적 상점 수)을 넘긴 후보 중 가중치로 하나 뽑는다. 직전 이벤트는 연속으로 나오지 않게 뺀다(후보가 그것뿐이면 허용).
        private TrackEventData _PickWeighted(IReadOnlyList<TrackEventData> pool, int stationCount)
        {
            _candidates.Clear();

            foreach (TrackEventData data in pool)
            {
                if (data == null || data.Weight <= 0f || data.MinStationCount > stationCount)
                    continue;

                _candidates.Add(data);
            }

            if (_candidates.Count > 1)
                _candidates.RemoveAll(data => data.Id == _lastTriggeredId);

            float totalWeight = 0f;

            foreach (TrackEventData data in _candidates)
                totalWeight += data.Weight;

            if (totalWeight <= 0f)
                return null;

            float roll = (float)_random.NextDouble() * totalWeight;

            foreach (TrackEventData data in _candidates)
            {
                roll -= data.Weight;

                if (roll <= 0f)
                    return data;
            }

            return _candidates[_candidates.Count - 1];
        }
    }
}
