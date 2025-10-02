namespace Cumic.Events
{
    public class GameEndEvent
    {
        private bool _isClear;
        public bool IsClear => _isClear;

        public GameEndEvent(bool isClear)
        {
            _isClear = isClear;
        }
    }
}