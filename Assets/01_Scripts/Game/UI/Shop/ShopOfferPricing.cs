using System;
using UnityEngine;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 역 상점 판매 가격 정책. 가격은 상점의 관심사이므로 선택지 데이터가 아니라 여기서 책정한다.
    /// (상품 타입 분기의 단일 지점 — ShopUI의 [SerializeField]로 노출되어 인스펙터에서 조정)
    /// </summary>
    [Serializable]
    public class ShopOfferPricing
    {
        [Tooltip("★ 플레이스홀더 가격 — 미보유 포탑 구매 비용. 테스트 후 조정")]
        [SerializeField]
        private int turretPurchaseCost = 150;

        [Tooltip("★ 플레이스홀더 가격 — 포탑 스탯 강화 레벨당 비용. 실제 가격 = 이 값 × (포탑 레벨 + 1)")]
        [SerializeField]
        private int statUpgradeCostPerLevel = 40;

        [Tooltip("★ 플레이스홀더 가격 — 엘리트 승격 비용. 테스트 후 조정")]
        [SerializeField]
        private int elitePromotionCost = 400;

        public int GetPrice(IChoiceOption option)
        {
            switch (option)
            {
                case EliteTrainChoice:
                    return elitePromotionCost;

                // 레벨 = 받은 업그레이드 횟수(획득 0)라 첫 강화가 1배, 이후 레벨 비례 상승.
                case TrainStatUpgradeChoice statUpgradeChoice:
                    return statUpgradeCostPerLevel
                        * ((statUpgradeChoice.TargetTrain != null ? statUpgradeChoice.TargetTrain.CurrentLevel : 0) + 1);

                case AddTrainChoice:
                    return turretPurchaseCost;

                default:
                    // 새 상품 타입이 가격 분기 없이 상점에 들어오면 조용히 오가격되는 걸 막기 위해 경고.
                    Debug.LogWarning($"ShopOfferPricing: 가격 분기가 없는 상품 타입 {option?.GetType().Name} — 포탑 구매가로 폴백");
                    return turretPurchaseCost;
            }
        }
    }
}
