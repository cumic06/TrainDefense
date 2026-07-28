using System;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Manager;

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
        private int turretPurchaseCost = 190;

        [Tooltip("1등급 스탯 강화 비용. 상위 등급은 등급표의 가격 배수(효과 배수보다 완만 = 고등급 단가 할인)와 스탯 프리미엄이 곱해진다")]
        [SerializeField]
        private int statUpgradeBaseCost = 50;

        [Tooltip("엘리트 승격 비용 — 고레벨 강화 여러 번 값에 해당하는 최상위 목표템. 실지불은 상점 방문당 인상 포함 2~3구간치 수입")]
        [SerializeField]
        private int elitePromotionCost = 5000;

        [Tooltip("상점이 한 번 열릴 때마다 전 상품 가격이 이 퍼센트씩 오른다 (선형, 역 도착 상점 + 맵 선택 상점 모두 포함). 안 사고 모으면 자동으로 손해가 되는 시간 축 인상 — 상점 내 남발 억제는 슬롯 소진+리롤 비용이 담당")]
        [SerializeField]
        private float stationPriceIncreasePercent = 20f;

        public int GetPrice(IChoiceOption option)
        {
            return Mathf.RoundToInt(_GetBasePrice(option) * _GetStationPriceMultiplier());
        }

        // 가격은 상품 등급만으로 정해진다 — 대상 포탑의 레벨과 무관해야 같은 포탑의 다른 스탯 카드를
        // 하나 샀다고 나머지 카드 가격이 따라 오르지 않는다(슬롯 간 간섭 제거).
        private float _GetStatUpgradePrice(TrainStatUpgradeChoice statUpgradeChoice)
        {
            return statUpgradeBaseCost * statUpgradeChoice.CostMultiplier;
        }

        // 누적 상점 방문 수 비례 선형 인상. 스테이지가 바뀌어도 리셋되지 않는다.
        // 적 HP·공격력·골드 스케일과 같은 축을 쓴다 — 상점이 열릴 때마다 적도 세지고 가격도 오른다.
        private float _GetStationPriceMultiplier()
        {
            var stageManager = StageManager.Instance;
            int totalInspectionPassedCount = stageManager != null ? stageManager.TotalInspectionPassedCount : 0;

            return 1f + stationPriceIncreasePercent / 100f * totalInspectionPassedCount;
        }

        private float _GetBasePrice(IChoiceOption option)
        {
            switch (option)
            {
                case EliteTrainChoice:
                    return elitePromotionCost;

                case TrainStatUpgradeChoice statUpgradeChoice:
                    return _GetStatUpgradePrice(statUpgradeChoice);

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
