using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 레벨업 삼중택일에 스탯 업그레이드(110xxx)를 카드로 띄우기 위한 런타임 래퍼.
    /// DB에 직렬화되지 않으며, 적용은 StatUpgradeSelectEvent 발행으로 기존 적용 파이프라인
    /// (TrainUpgradeManager: 레벨 증가 → 스탯 반영 → UpgradeAppliedEvent)을 재사용한다.
    /// </summary>
    public class StatUpgradeChoice : IChoiceOption
    {
        private readonly UpgradeData _upgradeData;

        public StatUpgradeChoice(UpgradeData upgradeData)
        {
            _upgradeData = upgradeData;
        }

        public UpgradeData UpgradeData => _upgradeData;

        public string Id => _upgradeData?.Id;

        // 만렙 전까지 매 레벨업마다 반복 선택 가능.
        public bool IsRepeatable => true;

        public bool IsValid()
        {
            if (_upgradeData == null)
                return false;

            var userDataManager = UserDataManager.Instance;

            return userDataManager == null || !userDataManager.IsUpgradeMaxLevel(_upgradeData.Id);
        }

        public void Execute()
        {
            GameEventSystem.Publish(new StatUpgradeSelectEvent(_upgradeData.Id));
        }
    }
}
