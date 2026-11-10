namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 새로운 몬스터를 처음 발견했을 때 UI에 알리기 위한 이벤트
    /// </summary>
    public class NewMonsterDiscoveredEvent
    {
        private string _monsterId;
        private bool _isElite;

        public string MonsterId => _monsterId;

        /// <summary>엘리트로 처음 발견됐는지. 엘리트는 전용 튜토리얼이 설명하므로 새 몬스터 알림 UI는 띄우지 않는다(도감 등록·업적은 그대로).</summary>
        public bool IsElite => _isElite;

        public NewMonsterDiscoveredEvent(string monsterId, bool isElite = false)
        {
            _monsterId = monsterId;
            _isElite = isElite;
        }
    }
}
