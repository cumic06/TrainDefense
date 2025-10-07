namespace TrainDefense.Game.Events
{
    public class AddExpEvent
    {
        private int _exp;

        public int Exp => _exp;

        public AddExpEvent(int exp)
        {
            _exp = exp;
        }
    }
}