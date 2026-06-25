namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 영구 레벨업(메타 업그레이드) 구매가 성공했을 때 발행. 구매 행동 분석에 사용한다.
    /// </summary>
    public class PermanentUpgradePurchasedEvent
    {
        public string UpgradeId { get; }
        public int Cost { get; }
        public int NewLevel { get; }

        public PermanentUpgradePurchasedEvent(string upgradeId, int cost, int newLevel)
        {
            UpgradeId = upgradeId;
            Cost = cost;
            NewLevel = newLevel;
        }
    }
}
