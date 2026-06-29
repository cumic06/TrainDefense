using System;
using TMPro;
using TrainDefense.Game.Analytics;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.Consent
{
    /// <summary>
    /// 첫 실행 시 1회 표시하는 개인정보(Analytics) 수집 옵트인 동의 팝업.
    /// [동의]/[거부] 결과를 콜백으로 전달하고, 개인정보처리방침 링크를 외부 브라우저로 연다.
    /// 텍스트는 LocalizeHelper.GetByKey 로 다국어 적용하며 언어 변경에 실시간 반응한다.
    /// </summary>
    public class ConsentPopupUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text agreeText;
        [SerializeField] private TMP_Text declineText;
        [SerializeField] private TMP_Text policyText;
        [SerializeField] private Button agreeButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Button policyButton;
        #endregion

        #region Variables
        private Action<bool> _onResult;
        #endregion

        #region LifeCycle
        private void OnEnable()
        {
            Localization.OnLanguageChanged += _RefreshTexts;

            PopupTween.PlayShow(gameObject);
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= _RefreshTexts;
        }

        private void Awake()
        {
            if (agreeButton != null)
                agreeButton.onClick.AddListener(_OnAgree);

            if (declineButton != null)
                declineButton.onClick.AddListener(_OnDecline);

            if (policyButton != null)
                policyButton.onClick.AddListener(_OnPolicyLink);
        }

        private void OnDestroy()
        {
            if (agreeButton != null)
                agreeButton.onClick.RemoveListener(_OnAgree);

            if (declineButton != null)
                declineButton.onClick.RemoveListener(_OnDecline);

            if (policyButton != null)
                policyButton.onClick.RemoveListener(_OnPolicyLink);
        }
        #endregion

        #region Public API
        /// <summary>팝업을 표시하고 동의(true)/거부(false) 결과를 콜백으로 전달한다.</summary>
        public void Show(Action<bool> onResult)
        {
            _onResult = onResult;
            _RefreshTexts();
            gameObject.SetActive(true);
        }
        #endregion

        #region Internal
        private void _RefreshTexts()
        {
            _SetText(titleText, "UI_Consent_Title", "개인정보 수집 동의");
            _SetText(bodyText, "UI_Consent_Body", "더 나은 게임 경험을 위해 익명 사용 통계를 수집합니다. 동의하지 않아도 게임 이용에는 제한이 없으며, 설정에서 언제든 변경할 수 있습니다.");
            _SetText(agreeText, "UI_Consent_Agree", "동의");
            _SetText(declineText, "UI_Consent_Decline", "동의 안 함");
            _SetText(policyText, "UI_Consent_Policy", "개인정보처리방침");
        }

        private void _SetText(TMP_Text target, string key, string fallback)
        {
            if (target != null)
                target.text = LocalizeHelper.GetByKey(key, fallback);
        }

        private void _OnAgree()
        {
            _Finish(true);
        }

        private void _OnDecline()
        {
            _Finish(false);
        }

        private void _OnPolicyLink()
        {
            Application.OpenURL(AnalyticsConsent.PrivacyPolicyUrl);
        }

        private void _Finish(bool granted)
        {
            var callback = _onResult;
            _onResult = null;
            callback?.Invoke(granted);

            // 미리 배치된 팝업이라 파괴하지 않고 비활성으로 닫는다(재표시 가능).
            PopupTween.PlayHide(gameObject, () => gameObject.SetActive(false));
        }
        #endregion
    }
}
