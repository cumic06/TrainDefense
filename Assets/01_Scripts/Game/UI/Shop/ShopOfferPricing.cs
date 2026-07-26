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

        [Tooltip("포탑 스탯 강화 기본 비용. 실제 가격 = 이 값 × 성장률^포탑레벨 — 저레벨은 부담 없이 여러 개, 고레벨은 저축해야 사는 급경사 곡선")]
        [SerializeField]
        private int statUpgradeBaseCost = 50;

        [Tooltip("스탯 강화 가격의 레벨당 성장률 (1.8 = 만렙행 강화 3061, 역당 인상 별도)")]
        [SerializeField]
        private float statUpgradeGrowthPerLevel = 1.8f;

        [Tooltip("엘리트 승격 비용 — 만렙행 강화(약 3천)의 두 배쯤인 최상위 목표템. 실지불은 역당 인상 포함 2~3역치 수입")]
        [SerializeField]
        private int elitePromotionCost = 5000;

        [Tooltip("상점이 한 번 열릴 때마다 전 상품 가격이 이 퍼센트씩 오른다 (선형, 역 도착 상점 + 맵 선택 상점 모두 포함). 안 사고 모으면 자동으로 손해가 되는 시간 축 인상 — 상점 내 남발 억제는 슬롯 소진+리롤 비용이 담당")]
        [SerializeField]
        private float stationPriceIncreasePercent = 10f;

        public int GetPrice(IChoiceOption option)
        {
            return Mathf.RoundToInt(_GetBasePrice(option) * _GetStationPriceMultiplier());
        }

        private float _GetStatUpgradePrice(TrainStatUpgradeChoice statUpgradeChoice)
        {
            int trainLevel = statUpgradeChoice.TargetTrain != null ? statUpgradeChoice.TargetTrain.CurrentLevel : 0;

            return statUpgradeBaseCost * Mathf.Pow(statUpgradeGrowthPerLevel, trainLevel);
        }

        // 누적 상점 방문 수 비례 선형 인상. 스테이지가 바뀌어도 리셋되지 않는다.
        // ★ 역 통과 수(TotalStationPassedCount)가 아니라 상점 방문 수를 쓴다 — 맵 선택 상점은 역 카운터를
        //   올리지 않아서, 역 기준으로 재면 스테이지마다 한 번은 같은 가격으로 상점을 두 번 보게 된다.
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

                // 레벨 = 이 포탑이 상점에서 받은 강화 횟수(획득 시 0). 레벨업 카드(110xxx)는 전역 스탯
                // 업그레이드라 포탑 레벨을 올리지 않으므로 이 가격에 영향을 주지 않는다.
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
