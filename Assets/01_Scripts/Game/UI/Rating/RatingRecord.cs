using UnityEngine;

namespace TrainDefense.Game.UI
{
    // 평점 팝업 관련 영구 상태(PlayerPrefs) 접근자.
    public static class RatingRecord
    {
        private const string RatedKey = "Rating_IsRated";
        private const string DefeatCountKey = "Rating_DefeatCount";
        private const string LastShownDefeatCountKey = "Rating_LastShownDefeatCount";

        public static bool IsRated
        {
            get => PlayerPrefs.GetInt(RatedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(RatedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static int DefeatCount => PlayerPrefs.GetInt(DefeatCountKey, 0);

        public static int LastShownDefeatCount => PlayerPrefs.GetInt(LastShownDefeatCountKey, 0);

        public static void AddDefeat()
        {
            PlayerPrefs.SetInt(DefeatCountKey, DefeatCount + 1);
            PlayerPrefs.Save();
        }

        public static void MarkShown()
        {
            PlayerPrefs.SetInt(LastShownDefeatCountKey, DefeatCount);
            PlayerPrefs.Save();
        }
    }
}
