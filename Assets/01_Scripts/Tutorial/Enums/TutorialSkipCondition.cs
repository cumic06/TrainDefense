namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 스텝을 넘기는 조건
    /// </summary>
    public enum TutorialSkipCondition
    {
        /// <summary> 화면 아무 곳 터치/클릭 </summary>
        ScreenTap,

        /// <summary> 지정된 버튼 클릭 </summary>
        ButtonClick,

        /// <summary> 일정 시간 후 자동 진행 </summary>
        Timeout,

        /// <summary> 외부 UnityEvent로 진행 </summary>
        Custom
    }
}
