namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 몬스터가 스폰될 때 발행되는 이벤트
    /// </summary>
    public class MonsterSpawnedEvent
    {
        private string _monsterId;

        public string MonsterId => _monsterId;

        public MonsterSpawnedEvent(string monsterId)
        {
            _monsterId = monsterId;
        }
    }
}
