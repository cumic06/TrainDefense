namespace TrainDefense.Game.UI
{
    // 마지막으로 팝업을 보여준 이후 새로운 패배가 있어야 표시한다 (첫 패배 포함).
    // 팝업을 닫기만 한 유저에게는 다음 패배 때 다시 보여준다.
    public class NewDefeatCondition : IRatingPopupCondition
    {
        public bool IsSatisfied()
        {
            if (RatingRecord.DefeatCount < 1)
                return false;

            return RatingRecord.DefeatCount > RatingRecord.LastShownDefeatCount;
        }
    }
}
