namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 새로운 몬스터를 처음 발견했을 때 UI에 알리기 위한 이벤트
    /// </summary>
    public class NewMonsterDiscoveredEvent
    {
        private string _monsterId;
        private bool _isBoss;

        public string MonsterId => _monsterId;
        public bool IsBoss => _isBoss;

        public NewMonsterDiscoveredEvent(string monsterId, bool isBoss)
        {
            _monsterId = monsterId;
            _isBoss = isBoss;
        }
    }
}
