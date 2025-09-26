namespace Cumic.Events
{
    public class StageEndEvent
    {
        private bool _isClear;
        public bool IsClear => _isClear;
        
        public StageEndEvent(bool isClear)
        {
            _isClear = isClear;
        }
    }
}