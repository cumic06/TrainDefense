namespace TrainDefense.Game
{
    /// <summary>
    /// 투사체 이동 타입
    /// </summary>
    public enum MovementType
    {
        Linear,         // 직선 이동
        TargetPos,      // 타겟 위치로 이동
        NonMovement     // 이동하지 않음 (고정 위치)
    }
}
