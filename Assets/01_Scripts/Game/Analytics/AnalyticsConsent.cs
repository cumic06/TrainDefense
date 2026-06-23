using UnityEngine;

namespace TrainDefense.Game.Analytics
{
    public enum AnalyticsConsentState
    {
        Unset = 0,
        Denied = 1,
        Granted = 2,
    }

    /// <summary>
    /// Analytics 데이터 수집 동의 상태를 PlayerPrefs에 영속화한다.
    /// 첫 실행 시 Unset → 옵트인 동의 팝업에서 Granted/Denied 로 확정한다.
    /// (GDPR: 동의 전에는 수집을 정지하기 위해 Unset/Denied 를 구분한다.)
    /// </summary>
    public static class AnalyticsConsent
    {
        private const string CONSENT_KEY = "AnalyticsConsent";

        // TODO: 실제 개인정보처리방침 호스팅 URL 로 교체 (동의 팝업·옵션 화면이 공유)
        public const string PrivacyPolicyUrl = "https://example.com/traindefense/privacy";

        public static AnalyticsConsentState State
            => (AnalyticsConsentState)PlayerPrefs.GetInt(CONSENT_KEY, (int)AnalyticsConsentState.Unset);

        public static bool IsDecided => State != AnalyticsConsentState.Unset;

        public static bool IsGranted => State == AnalyticsConsentState.Granted;

        public static void Set(AnalyticsConsentState state)
        {
            PlayerPrefs.SetInt(CONSENT_KEY, (int)state);
            PlayerPrefs.Save();
        }
    }
}
