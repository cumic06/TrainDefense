using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game
{
    public class UpgradeManager : Singleton<UpgradeManager>
    {
        private void Start()
        {
            GameEventSystem.Subscribe<BuyShopItemEvent>(OnBuyShopItem);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<BuyShopItemEvent>(OnBuyShopItem);
        }

        private void OnBuyShopItem(BuyShopItemEvent buyShopItemEvent)
        {
            if (buyShopItemEvent == null || string.IsNullOrEmpty(buyShopItemEvent.UpgradeId))
            {
                Debug.LogWarning("UpgradeManager: BuyShopItemEvent is null or UpgradeId is empty");
                return;
            }

            string upgradeId = buyShopItemEvent.UpgradeId;
            UpgradeData upgradeData = DatabaseManager.Instance.GetUpgradeData(upgradeId);

            if (upgradeData == null)
            {
                Debug.LogWarning($"UpgradeManager: UpgradeData not found for ID '{upgradeId}'");
                return;
            }

            int currentLevel = UserDataManager.Instance.GetUpgradeLevel(upgradeId);
            if (upgradeData.MaxUpgradeCount > 0 && currentLevel >= upgradeData.MaxUpgradeCount)
            {
                Debug.LogWarning($"UpgradeManager: Max upgrade count reached for '{upgradeId}'");
                return;
            }

            UserDataManager.Instance.UpgradeLevel(upgradeId);

            ApplyUpgrade(upgradeData);

            // 업그레이드 적용 이후 UI 및 기타 시스템에 알려주기 위한 이벤트 발행
            int newLevel = UserDataManager.Instance.GetUpgradeLevel(upgradeId);
            GameEventSystem.Publish(new UpgradeAppliedEvent(upgradeId, newLevel));
        }

        private void ApplyUpgrade(UpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            switch (upgradeData.UpgradeDataType)
            {
                case UpgradeDataType.TrainUpgrade:
                    ApplyTrainUpgrade(upgradeData);
                    break;
                case UpgradeDataType.NonTrainUpgrade:
                    ApplyNonTrainUpgrade(upgradeData);
                    break;
                default:
                    Debug.LogWarning($"UpgradeManager: Unknown upgrade type '{upgradeData.UpgradeDataType}' for '{upgradeData.Id}'");
                    break;
            }
        }

        /// <summary>
        /// Train 관련 업그레이드 적용
        /// </summary>
        private void ApplyTrainUpgrade(UpgradeData upgradeData)
        {
            if (TrainManager.Instance == null || TrainManager.Instance.MainTrain == null)
            {
                Debug.LogWarning("UpgradeManager: TrainManager or MainTrain is null");
                return;
            }
            
            foreach (var stat in upgradeData.Stats)
            {
            }
            TrainManager.Instance.ApplyUpgrade(upgradeData);
        }

        /// <summary>
        /// Train 외의 업그레이드 적용
        /// </summary>
        private void ApplyNonTrainUpgrade(UpgradeData upgradeData)
        {
            // NonTrainUpgrade 타입의 업그레이드 처리 로직 추가 가능
            Debug.Log($"UpgradeManager: NonTrainUpgrade applied for '{upgradeData.Id}'");
        }
    }
}