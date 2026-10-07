using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 포탑 칸 배지(HUD 포탑 칸·상점 기차 칸 공용)의 개조 진행 표시 규칙.
    /// 배지 숫자 = 그 포탑의 강화 카드 등급 합. 리본은 흰 리본 하나에 상태 색을 곱한다 — 평소 파랑, 개조 조건(Train.ELITE_PROMOTION_GRADE_SUM)에 닿으면 노랑, 개조된 포탑은 주황.
    /// </summary>
    public static class TrainRemodelInfo
    {
        // 팩 원본 리본(회청·금색)은 탁해서 밝게 다시 그린 흰 리본에 색을 곱한다. 노랑과 주황은 서로 헷갈리지 않게 거리를 둔다.
        private static readonly Color NormalRibbonColor = new Color(0.40f, 0.62f, 1f);
        private static readonly Color EligibleRibbonColor = new Color(1f, 0.86f, 0.25f);
        private static readonly Color PromotedRibbonColor = new Color(1f, 0.62f, 0.15f);

        // 이미 개조(엘리트 승격)된 포탑인가 — 개조 후 포탑의 TrainData가 어떤 개조 카드의 결과 ID와 같은지로 판정한다.
        public static bool IsPromoted(Train train)
        {
            var databaseManager = DatabaseManager.Instance;

            if (train == null || train.TrainData == null || databaseManager == null)
                return false;

            string trainId = train.TrainData.Id;

            return databaseManager.GetEliteTrainChoices()
                .OfType<EliteTrainChoice>()
                .Any(eliteChoice => eliteChoice.EliteTrainDataId == trainId);
        }

        // 배지 숫자와 리본 색을 포탑 상태에 맞춘다. 리본 이미지는 흰 리본(BigRibbons White)이어야 색이 제대로 나온다.
        public static void ApplyBadge(Image ribbonImage, TextMeshProUGUI badgeText, Train train)
        {
            if (train == null)
                return;

            if (badgeText != null)
                badgeText.text = train.UpgradeGradeSum.ToString();

            if (ribbonImage == null)
                return;

            if (IsPromoted(train))
                ribbonImage.color = PromotedRibbonColor;
            else
                ribbonImage.color = train.IsEliteEligible ? EligibleRibbonColor : NormalRibbonColor;
        }
    }
}
