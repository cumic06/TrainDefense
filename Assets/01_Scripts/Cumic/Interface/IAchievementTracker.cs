using System.Collections.Generic;

namespace Cumic.Achievement
{
    public interface IAchievementTracker
    {
        void Initialize();
        void AddProgress(string achievementId, int amount = 1);
        void SetProgress(string achievementId, int value);
        AchievementState GetState(string achievementId);
        IReadOnlyList<IAchievementData> GetAllAchievements();
        void Save();
        void Load();
    }
}
