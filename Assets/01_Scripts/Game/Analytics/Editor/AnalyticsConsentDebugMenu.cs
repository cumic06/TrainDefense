using UnityEditor;
using UnityEngine;

namespace TrainDefense.Game.Analytics.Editor
{
    /// <summary>
    /// Analytics 동의 상태 디버그 메뉴. 동의 팝업을 다시 띄워 테스트할 때 사용한다.
    /// 저장된 동의값(PlayerPrefs "AnalyticsConsent")만 삭제하므로 코인·진행도·업적·옵션 등 다른 저장 데이터는 보존된다.
    /// </summary>
    public static class AnalyticsConsentDebugMenu
    {
        [MenuItem("TrainDefense/Analytics/Reset Consent (show popup again)")]
        private static void ResetConsent()
        {
            AnalyticsConsent.Clear();
            Debug.Log("[Analytics] 동의 상태를 리셋했습니다(PlayerPrefs 'AnalyticsConsent' 삭제). 다음 게임 실행 시 동의 팝업이 다시 표시됩니다.");
        }
    }
}
