namespace Cumic.Achievement
{
    public interface IAchievementData
    {
        string Id { get; }
        string Title { get; }
        string Description { get; }
        int TargetValue { get; }
        bool IsHidden { get; }
    }
}
