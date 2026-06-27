using System.Collections.Generic;
using TMPro;
using Cumic.Sequence;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Analytics;
using TrainDefense.Game.Intro;
using TrainDefense.Game.Manager;
using TrainDefense.Game.UI;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrainDefense
{
   // 옵션 창의 개인정보(Analytics 동의·개인정보처리방침) 컨트롤을 담당한다.
   // 동의 토글/정책 버튼이 들어 있는 오브젝트(Panel_Sound 등)에 부착하고, 관련 SerializeField만 연결한다.
   public class PrivacyOptionUI : MonoBehaviour
   {
      #region Fields
      [SerializeField]
      private Toggle analyticsConsentToggle;
      [SerializeField]
      private TMP_Text analyticsConsentLabel;
      [SerializeField]
      private Button privacyPolicyButton;
      [SerializeField]
      private TMP_Text privacyPolicyText;
      #endregion

      private void OnEnable()
      {
         if (analyticsConsentToggle != null)
            analyticsConsentToggle.onValueChanged.AddListener(_OnAnalyticsConsentChanged);

         if (privacyPolicyButton != null)
            privacyPolicyButton.onClick.AddListener(_OnPrivacyPolicyClick);

         Localization.OnLanguageChanged += _RefreshAnalyticsConsent;
         Localization.OnInitialized += _RefreshAnalyticsConsent;

         _RefreshAnalyticsConsent();
      }

      private void OnDisable()
      {
         if (analyticsConsentToggle != null)
            analyticsConsentToggle.onValueChanged.RemoveListener(_OnAnalyticsConsentChanged);

         if (privacyPolicyButton != null)
            privacyPolicyButton.onClick.RemoveListener(_OnPrivacyPolicyClick);

         Localization.OnLanguageChanged -= _RefreshAnalyticsConsent;
         Localization.OnInitialized -= _RefreshAnalyticsConsent;
      }

      // Analytics 데이터 수집 동의 토글/정책 링크를 현재 상태·언어로 갱신한다.
      private void _RefreshAnalyticsConsent()
      {
         if (analyticsConsentToggle != null)
            analyticsConsentToggle.SetIsOnWithoutNotify(AnalyticsConsent.IsGranted);

         if (analyticsConsentLabel != null)
            analyticsConsentLabel.text = LocalizeHelper.GetByKey("UI_Option_Analytics", "데이터 수집 동의");

         if (privacyPolicyText != null)
            privacyPolicyText.text = LocalizeHelper.GetByKey("UI_Consent_Policy", "개인정보처리방침");
      }

      // 토글 변경 시 동의 상태를 저장하고 즉시 Firebase 수집 on/off 에 반영한다. (GDPR 철회 수단)
      private void _OnAnalyticsConsentChanged(bool isGranted)
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);

         if (AnalyticsManager.Instance != null)
            AnalyticsManager.Instance.SetConsent(isGranted);
         else
            AnalyticsConsent.Set(isGranted ? AnalyticsConsentState.Granted : AnalyticsConsentState.Denied);
      }

      private void _OnPrivacyPolicyClick()
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);
         Application.OpenURL(AnalyticsConsent.PrivacyPolicyUrl);
      }
   }
}
