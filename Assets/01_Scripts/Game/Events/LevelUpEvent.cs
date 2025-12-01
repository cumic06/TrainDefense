namespace TrainDefense.Game.Events
{
    public class LevelUpEvent
    {
        private int _levelUpCount;

        public int LevelUpCount => _levelUpCount;

        public LevelUpEvent(int levelUpCount)
        {
            _levelUpCount = levelUpCount;
        }
    }
}