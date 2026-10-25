using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    /// <summary>선로 이벤트가 발동했다. 배너 UI·사운드·분석 로그가 구독한다.</summary>
    public class TrackEventStartEvent
    {
        public TrackEventData Data { get; }

        public TrackEventStartEvent(TrackEventData data)
        {
            Data = data;
        }
    }
}
