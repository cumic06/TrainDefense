namespace Cumic.Achievement
{
    public class AchievementProgressEvent
    {
        private IAchievementData _data;
        private AchievementState _state;

        public IAchievementData Data => _data;
        public AchievementState State => _state;

        public AchievementProgressEvent(IAchievementData data, AchievementState state)
        {
            _data = data;
            _state = state;
        }
    }
}
