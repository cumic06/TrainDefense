namespace TrainDefense.Game.UI
{
    // 아직 평가하지 않은 유저에게만 표시한다.
    public class NotRatedCondition : IRatingPopupCondition
    {
        public bool IsSatisfied() => !RatingRecord.IsRated;
    }
}
