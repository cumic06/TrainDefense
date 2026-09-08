namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 이어하기로 런 상태 복원이 끝난 직후 발행된다.
    /// UI처럼 "복원된 값으로 표시를 다시 그려야 하는" 쪽이 구독한다. (상태 변경 자체는 RunSaveManager가 직접 수행)
    /// </summary>
    public class RunRestoredEvent
    {
    }
}
