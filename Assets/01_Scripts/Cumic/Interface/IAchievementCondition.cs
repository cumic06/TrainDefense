namespace Cumic.Achievement
{
    public interface IAchievementCondition
    {
        string AchievementId { get; }
        bool IsMet(int currentValue);
    }
}
