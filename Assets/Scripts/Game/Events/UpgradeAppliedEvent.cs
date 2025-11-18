namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 업그레이드가 실제로 적용된 이후 발행되는 이벤트입니다.
    /// </summary>
    public class UpgradeAppliedEvent
    {
        private readonly string _upgradeId;
        private readonly int _newLevel;

        /// <summary>
        /// 적용된 업그레이드의 ID.
        /// </summary>
        public string UpgradeId => _upgradeId;

        /// <summary>
        /// 업그레이드 적용 후의 레벨.
        /// </summary>
        public int NewLevel => _newLevel;

        public UpgradeAppliedEvent(string upgradeId, int newLevel)
        {
            _upgradeId = upgradeId;
            _newLevel = newLevel;
        }
    }
}