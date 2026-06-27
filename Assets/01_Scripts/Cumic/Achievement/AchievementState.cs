using System;

namespace Cumic.Achievement
{
    [Serializable]
    public class AchievementState
    {
        public string AchievementId;
        public int CurrentValue;
        public bool IsUnlocked;

        public AchievementState() { }

        public AchievementState(string achievementId)
        {
            AchievementId = achievementId;
            CurrentValue = 0;
            IsUnlocked = false;
        }
    }
}
