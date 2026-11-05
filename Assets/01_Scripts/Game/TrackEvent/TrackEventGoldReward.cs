using TrainDefense.Game.Manager;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 선로 이벤트 골드 보상 공식. 상점 가격이 후반에 크게 오르므로 지난 역 수에 비례해 올린다.
    /// 값은 시작값이다: 18역에서 첫 역의 약 12.5배로 상점 구매가 상승 폭과 비슷하게 맞췄다(봇 판 기록 165~209 → 1,755~3,276).
    /// </summary>
    public static class TrackEventGoldReward
    {
        private const float GROWTH_PER_STATION = 0.64f;

        public static int Get(float baseGold)
        {
            int stationCount = StageManager.Instance != null ? StageManager.Instance.TotalInspectionPassedCount : 0;

            return Mathf.Max(1, Mathf.RoundToInt(baseGold * (1f + GROWTH_PER_STATION * stationCount)));
        }
    }
}
