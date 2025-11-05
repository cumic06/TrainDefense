namespace TrainDefense.Game
{
    /// <summary>
    /// 투사체 이동 타입
    /// </summary>
    public enum MovementType
    {
        Linear,         // 직선 이동
        DelayedDrop,    // 지연 후 타겟 위치에 낙하
        NonMovement     // 이동하지 않음 (고정 위치)
    }
}
