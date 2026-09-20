namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 몬스터가 스폰될 때 발행되는 이벤트
    /// </summary>
    public class MonsterSpawnedEvent
    {
        private string _monsterId;
        private bool _isElite;

        public string MonsterId => _monsterId;

        /// <summary>엘리트로 승격되어 스폰됐는지. 처치 쪽(<see cref="MonsterDeadEvent"/>)과 짝을 이뤄 등장 대비 처치율을 잴 수 있다.</summary>
        public bool IsElite => _isElite;

        public MonsterSpawnedEvent(string monsterId, bool isElite = false)
        {
            _monsterId = monsterId;
            _isElite = isElite;
        }
    }
}
