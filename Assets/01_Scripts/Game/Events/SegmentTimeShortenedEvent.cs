namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 현재 구간의 남은 시간이 줄었다(StageManager.ShortenRemainingSegmentTime — 순풍 운행·엔진 과부하).
    /// 구간 안에 시각을 예약해 둔 쪽(엘리트 일정·무작위 이벤트)은 남은 예약을 같은 비율로 당겨 줄어든 시간 안에 그대로 나오게 한다.
    /// </summary>
    public class SegmentTimeShortenedEvent
    {
        /// <summary>실제로 줄인 남은 시간 비율(0~1). 0.3이면 남은 시간이 70%가 됐다.</summary>
        public float Ratio { get; }

        public SegmentTimeShortenedEvent(float ratio)
        {
            Ratio = ratio;
        }
    }
}
