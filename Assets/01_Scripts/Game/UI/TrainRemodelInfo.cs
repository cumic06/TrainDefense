using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 포탑 칸 배지(HUD 포탑 칸·상점 기차 칸 공용)의 개조 진행 표시 규칙.
    /// 배지 숫자 = 그 포탑의 강화 카드 등급 합, 개조 조건(Train.ELITE_PROMOTION_GRADE_SUM)에 닿으면 금색 리본, 개조된 포탑은 "E".
    /// </summary>
    public static class TrainRemodelInfo
    {
        public const string PromotedBadgeText = "E";

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

        // 배지 문구·리본을 포탑 상태에 맞춘다. 개조 가능이면 금색(노란) 리본으로 바꾸고, 아니면 평소 리본으로 되돌린다.
        // (파란 리본에 색을 곱하면 금색이 안 나와서 색이 아니라 스프라이트를 교체한다)
        public static void ApplyBadge(Image ribbonImage, TextMeshProUGUI badgeText, Train train, Sprite normalRibbonSprite, Sprite eligibleRibbonSprite)
        {
            if (train == null)
                return;

            bool isPromoted = IsPromoted(train);

            if (badgeText != null)
                badgeText.text = isPromoted ? PromotedBadgeText : train.UpgradeGradeSum.ToString();

            if (ribbonImage != null)
            {
                bool showEligibleRibbon = !isPromoted && train.IsEliteEligible && eligibleRibbonSprite != null;
                ribbonImage.sprite = showEligibleRibbon ? eligibleRibbonSprite : normalRibbonSprite;
            }
        }
    }
}
