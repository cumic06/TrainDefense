using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 진행 중인 선로 이벤트 한 칸(데이터 + 효과 + 경과 시간). TrackEventManager가 분류별(무작위·날씨·선택)로 한 칸씩 들고 있어
    /// 세 종류가 한 구간에 동시에 걸릴 수 있다. 이벤트 발행은 매니저의 몫이고, 이 클래스는 효과의 Begin/Tick/End 수명만 관리한다.
    /// </summary>
    public class TrackEventSlot
    {
        #region Variables

        public TrackEventData Data { get; private set; }
        public bool IsActive => _effect != null;
        public float ElapsedTime { get; private set; }

        #endregion

        #region Fields

        private readonly bool _isAlwaysUntilStation;
        private ITrackEventEffect _effect;

        #endregion

        /// <param name="isAlwaysUntilStation">true면 데이터의 지속 시간을 무시하고 역 도착(강제 종료)까지 유지한다(날씨·선택).</param>
        public TrackEventSlot(bool isAlwaysUntilStation)
        {
            _isAlwaysUntilStation = isAlwaysUntilStation;
        }

        /// <summary>효과를 만들어 시작한다. 효과 구현이 없는 타입이면 false.</summary>
        public bool Begin(TrackEventData data)
        {
            ITrackEventEffect effect = data != null ? TrackEventEffectFactory.Create(data.EventType) : null;

            if (effect == null)
                return false;

            Data = data;
            _effect = effect;
            ElapsedTime = 0f;
            _effect.Begin(data);

            return true;
        }

        /// <summary>시간을 흘린다. 지속 시간이 다 됐으면 true(매니저가 End(false)를 부른다).</summary>
        public bool Tick(float deltaTime)
        {
            if (_effect == null)
                return false;

            _effect.Tick(deltaTime);
            ElapsedTime += deltaTime;

            if (_isAlwaysUntilStation || Data.IsUntilStation)
                return false;

            return ElapsedTime >= Data.Duration;
        }

        /// <summary>효과를 끝내고 끝난 데이터를 돌려준다. 진행 중이 아니면 null.</summary>
        public TrackEventData End(bool isInterrupted)
        {
            if (_effect == null)
                return null;

            TrackEventData endedData = Data;
            _effect.End(isInterrupted);
            _effect = null;
            Data = null;

            return endedData;
        }
    }
}
