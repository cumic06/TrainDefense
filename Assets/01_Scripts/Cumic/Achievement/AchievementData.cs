namespace Cumic.Achievement
{
    /// <summary>
    /// 업적 한 개의 정적 정의. 카탈로그(AchievementCatalog)가 단일 소스로 보유하며,
    /// 인게임 트래커(TrainDefenseAchievement)와 로비 업적 UI가 공통으로 참조한다.
    /// 진행 상태(현재값/달성 여부)는 AchievementState가 별도로 들고 저장된다.
    /// </summary>
    public class AchievementData : IAchievementData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TargetValue { get; set; }
        public bool IsHidden { get; set; }

        /// <summary>같은 조건키를 공유하는 업적은 하나의 게임 이벤트로 함께 진행된다.</summary>
        public string ConditionKey { get; set; }
    }
}
