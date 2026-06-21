namespace Cumic.Achievement
{
    public class AchievementUnlockedEvent
    {
        private IAchievementData _data;
        private AchievementState _state;

        public IAchievementData Data => _data;
        public AchievementState State => _state;

        public AchievementUnlockedEvent(IAchievementData data, AchievementState state)
        {
            _data = data;
            _state = state;
        }
    }
}
