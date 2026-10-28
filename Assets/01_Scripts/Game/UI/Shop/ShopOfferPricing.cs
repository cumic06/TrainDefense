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
        private int turretPurchaseCost = 160;

        [Tooltip("1등급 스탯 강화 비용. 상위 등급은 등급표의 가격 배수(효과 배수보다 완만 = 고등급 단가 할인)와 스탯 프리미엄이 곱해진다")]
        [SerializeField]
        private int statUpgradeBaseCost = 50;

        [Tooltip("엘리트 승격(개조) 기본 비용. 역 인상이 곱해진 값이 처음 등장하는 역부터 그 역 수입 한 번으로 살 수 있어야 한다(모으지 않고)")]
        [SerializeField]
        private int elitePromotionCost = 3000;

        [Header("Reroll")]
        [Tooltip("첫 상점의 첫 리롤 비용 기준값. 실제 첫 비용 = 이 값 + 방문 수 × rerollCostPerShopVisit")]
        [SerializeField]
        private int rerollBaseCost = 50;

        [Tooltip("상점 방문 1회마다 첫 리롤 비용에 더해지는 값. 수입이 커지는 후반에도 리롤이 공짜처럼 되지 않게 한다")]
        [SerializeField]
        private int rerollCostPerShopVisit = 20;

        [Tooltip("리롤할 때마다 더해지는 증가분 = 이번 상점 첫 비용 × 이 배율. 상점을 새로 열면 첫 비용으로 초기화")]
        [SerializeField]
        private float rerollCostIncreaseRatio = 0.5f;

        // 복합 카드 가격 배율. 1이면 한 등급 아래 단일 카드 두 장 값 그대로다(2등급 복합 = 1등급 두 장).
        // 할인하면 원당 가격이 게임에서 가장 싼 카드가 된다. 한 칸에 효과 두 개가 담기는 것만으로 이득이 남는다.
        private const float COMBO_COST_RATIO = 1f;

        [Tooltip("상점이 한 번 열릴 때마다 전 상품 가격이 이 퍼센트씩 오른다 (선형). 모든 상품이 같은 배율을 받으므로 등급 간 가격 비율은 유지된다. 기본 비용은 '방문 0 기준' 값이라 늦게 열리는 상품은 처음부터 오른 값으로 보인다")]
        [SerializeField]
        private float stationPriceIncreasePercent;

        [Header("Repair")]
        [Tooltip("잃은 체력 1당 수리 코인(방문 0 기준). 다른 상품과 같은 역 인상이 곱해진다")]
        [SerializeField]
        private float repairCostPerHp = 1f;

        public int GetPrice(IChoiceOption option)
        {
            return Mathf.RoundToInt(_GetBasePrice(option) * _GetStationPriceMultiplier());
        }

        // 체력 계산의 소수점 오차로 꽉 찬 칸에 1코인 수리비가 붙지 않게 이 아래는 고칠 게 없는 것으로 본다.
        private const float REPAIR_MISSING_HP_EPSILON = 0.01f;

        // 회복 없는 난이도의 상점 수리 비용. 부서진 칸의 잃은 체력은 최대 체력 전부다.
        public int GetRepairPrice(float missingHp)
        {
            return missingHp > REPAIR_MISSING_HP_EPSILON ? Mathf.CeilToInt(missingHp * repairCostPerHp * _GetStationPriceMultiplier()) : 0;
        }

        // 이번 상점의 첫 리롤 비용. 방문할수록 올라 후반에도 리롤이 공짜처럼 되지 않는다.
        public int GetFirstRerollCost() => rerollBaseCost + rerollCostPerShopVisit * _GetShopVisitCount();

        // 리롤할 때마다 더해지는 증가분(이번 상점 첫 비용 × 배율).
        public int GetRerollCostIncrease(int firstRerollCost) => Mathf.RoundToInt(firstRerollCost * rerollCostIncreaseRatio);

        // 스킬 트리 노드와 영구 강화의 무료 새로고침 횟수 합. 둘 다 "레벨당 +1회"라 정수로 쓴다.
        public int GetFreeRerollCount()
        {
            int count = 0;

            if (SkillTreeManager.Instance != null)
                count += Mathf.RoundToInt(SkillTreeManager.Instance.GetValue(SkillTreePassiveType.FreeReroll));

            if (PermanentUpgradeManager.Instance != null)
                count += Mathf.RoundToInt(PermanentUpgradeManager.Instance.GetValue(PermanentUpgradeType.FreeReroll));

            return count;
        }

        // 가격은 상품 등급만으로 정해진다 — 대상 포탑의 레벨과 무관해야 같은 포탑의 다른 스탯 카드를
        // 하나 샀다고 나머지 카드 가격이 따라 오르지 않는다(슬롯 간 간섭 제거).
        // 가격 배수 = 등급 배수(고등급일수록 단가 할인 — 희귀 보상) × 스탯 프리미엄(+1 가치가 큰 정수 스탯).
        // 복합은 "카드 두 장 값"이라 두 스탯의 프리미엄을 더한 뒤 COMBO_COST_RATIO를 곱한다.
        private float _GetStatUpgradePrice(TrainStatUpgradeChoice statUpgradeChoice)
        {
            var databaseManager = DatabaseManager.Instance;
            float statPremium = databaseManager != null ? databaseManager.GetStatCostMultiplier(statUpgradeChoice.StatType) : 1f;
            if (statUpgradeChoice.SecondStatType.HasValue)
                statPremium += databaseManager != null ? databaseManager.GetStatCostMultiplier(statUpgradeChoice.SecondStatType.Value) : 0f;

            float comboRatio = statUpgradeChoice.IsCombo ? COMBO_COST_RATIO : 1f;
            return statUpgradeBaseCost * statUpgradeChoice.EffectTier.CostMultiplier * statPremium * comboRatio;
        }

        // 누적 상점 방문 수 비례 선형 인상. 스테이지가 바뀌어도 리셋되지 않는다.
        // 적 HP·공격력·골드 스케일과 같은 축을 쓴다 — 상점이 열릴 때마다 적도 세지고 가격도 오른다.
        private float _GetStationPriceMultiplier()
            => 1f + stationPriceIncreasePercent / 100f * _GetShopVisitCount();

        // 가격·리롤 비용이 함께 쓰는 축. 역 도착 상점과 맵 이동 상점을 모두 센다.
        private static int _GetShopVisitCount()
        {
            var stageManager = StageManager.Instance;
            return stageManager != null ? stageManager.TotalInspectionPassedCount : 0;
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

                // 전체 포탑 아이템: 강화 카드 등급표의 가격 배수(1등급 카드 값 × 등급 배수) × 성장률^보유 수 (같은 아이템을 쌓을수록 비싸진다).
                // 등급이 없는 데이터는 데이터 기본가로.
                case StatUpgradeChoice itemChoice when itemChoice.UpgradeData != null:
                {
                    int ownedCount = UserDataManager.Instance != null ? UserDataManager.Instance.GetUpgradeLevel(itemChoice.Id) : 0;
                    var tier = DatabaseManager.Instance != null ? DatabaseManager.Instance.GetStatUpgradeTier(itemChoice.UpgradeData.Grade) : null;

                    if (tier == null)
                        return itemChoice.UpgradeData.GetCostAtLevel(ownedCount);

                    return statUpgradeBaseCost * tier.CostMultiplier * Mathf.Pow(itemChoice.UpgradeData.GrowthRate, ownedCount);
                }

                default:
                    // 새 상품 타입이 가격 분기 없이 상점에 들어오면 조용히 오가격되는 걸 막기 위해 경고.
                    Debug.LogWarning($"ShopOfferPricing: 가격 분기가 없는 상품 타입 {option?.GetType().Name} — 포탑 구매가로 폴백");
                    return turretPurchaseCost;
            }
        }
    }
}
