namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 편성이 통째로 비워졌을 때 발행된다(이어하기 복원 직전 기본 편성 제거).
    /// 편성에 1:1로 붙는 UI 슬롯처럼 "기차가 추가될 때 늘어나기만 하는" 표시를 함께 비우는 데 쓴다.
    /// </summary>
    public class TrainFormationClearedEvent
    {
    }
}
