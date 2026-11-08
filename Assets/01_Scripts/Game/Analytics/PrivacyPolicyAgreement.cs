using UnityEngine;

namespace TrainDefense.Game.Analytics
{
    /// <summary>
    /// 개인정보처리방침 필수 확인("읽고 이해했습니다") 여부를 PlayerPrefs에 영속화한다.
    /// OAuthScene은 이 값이 참이 될 때까지 다음 씬으로 넘어가지 않는다.
    /// 확인한 방침의 시행일(버전)을 함께 저장하므로, 방침이 바뀌면 POLICY_VERSION만 올리면 모두에게 다시 묻는다.
    /// 선택 항목인 분석 수집 동의는 AnalyticsConsent가 따로 관리한다.
    /// </summary>
    public static class PrivacyPolicyAgreement
    {
        #region Variables

        private const string AGREEMENT_KEY = "PrivacyPolicyAgreedVersion";

        // 현재 앱에 들어 있는 개인정보처리방침의 시행일. privacy.html / Resources/PrivacyPolicy/*.txt 와 맞춘다.
        private const string POLICY_VERSION = "2026-06-24";

        #endregion

        public static bool IsAccepted => PlayerPrefs.GetString(AGREEMENT_KEY, string.Empty) == POLICY_VERSION;

        public static void Accept()
        {
            PlayerPrefs.SetString(AGREEMENT_KEY, POLICY_VERSION);
            PlayerPrefs.Save();
        }

        /// <summary>확인 기록을 지운다. 다음 실행 때 OAuthScene에서 동의 팝업이 다시 뜬다. (디버그/테스트용)</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(AGREEMENT_KEY);
            PlayerPrefs.Save();
        }
    }
}
