namespace TrainDefense.Game.UI
{
    // 평점 팝업 표시 조건. 새 조건은 이 인터페이스를 구현해서
    // RatingPopupManager의 조건 목록에 추가한다.
    public interface IRatingPopupCondition
    {
        bool IsSatisfied();
    }
}
