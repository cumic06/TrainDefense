namespace TrainDefense.Game.Datas
{
    public interface IChoiceOption
    {
        string Id { get; }

        // 반복 선택 가능 여부. true면 추첨에서 "이미 선택됨(HasSelectedChoice)" 제외를 적용하지 않는다.
        // 만렙 보상·스탯 업글처럼 소진 전까지 계속 떠야 하는 카드가 override한다.
        // (기본 구현을 두어 새 타입 추가 시 매니저의 타입 스위치를 편집할 필요가 없게 한다)
        bool IsRepeatable => false;

        bool IsValid();

        // ★ 계약: IsValid()가 true인 상태에서 Execute()는 실패 없이 효과를 적용해야 한다.
        // 상점은 "코인 차감 → Execute" 순서라, Execute가 조용히 no-op이 되면 돈만 나간다.
        void Execute();
    }
}