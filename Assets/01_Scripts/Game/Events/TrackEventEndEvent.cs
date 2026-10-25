using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    /// <summary>선로 이벤트가 끝났다(지속 시간 만료 또는 역 도착·게임 종료로 강제 종료).</summary>
    public class TrackEventEndEvent
    {
        public TrackEventData Data { get; }

        /// <summary>지속 시간을 다 채우지 못하고 역 도착·게임 종료로 끊겼는지.</summary>
        public bool IsInterrupted { get; }

        public TrackEventEndEvent(TrackEventData data, bool isInterrupted)
        {
            Data = data;
            IsInterrupted = isInterrupted;
        }
    }
}
