namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 몬스터가 스폰될 때 발행되는 이벤트
    /// </summary>
    public class MonsterSpawnedEvent
    {
        private string _monsterId;
        private bool _isBoss;

        public string MonsterId => _monsterId;
        public bool IsBoss => _isBoss;

        public MonsterSpawnedEvent(string monsterId, bool isBoss)
        {
            _monsterId = monsterId;
            _isBoss = isBoss;
        }
    }
}
