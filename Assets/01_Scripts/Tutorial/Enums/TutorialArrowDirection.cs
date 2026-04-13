namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 화살표 배치 방향 (타겟 기준 화살표 위치)
    /// </summary>
    public enum TutorialArrowDirection
    {
        None,
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }

    /// <summary>
    /// 튜토리얼 화살표가 바라보는(가리키는) 방향
    /// </summary>
    public enum TutorialArrowLookDirection
    {
        /// <summary>
        /// 배치 방향에 따라 자동으로 타겟을 가리킴 (기본 동작)
        /// </summary>
        Auto,
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }
}
