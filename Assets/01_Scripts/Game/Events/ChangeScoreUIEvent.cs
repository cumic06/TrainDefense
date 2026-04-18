namespace TrainDefense.Game.Events
{
    public class ChangeScoreUIEvent
    {
        private int _beforeScore;
        private int _afterScore;
        public int BeforeScore => _beforeScore;
        public int AfterScore => _afterScore;

        public ChangeScoreUIEvent(int beforeScore, int afterScore)
        {
            _beforeScore = beforeScore;
            _afterScore = afterScore;
        }
    }
}
