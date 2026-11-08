using TMPro;
using TrainDefense.Game.Analytics;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.Consent
{
    /// <summary>
    /// 개인정보처리방침을 외부 브라우저 없이 앱 안에서 보여주는 스크롤 팝업.
    /// 원문은 Resources/PrivacyPolicy/PrivacyPolicy_{Korean|English}.txt (웹 privacy.html 과 같은 내용) 이다.
    /// 정책 원문이 한국어·영어 두 벌뿐이라 한국어 설정이면 한국어, 그 외 언어는 영어로 보여준다.
    /// 자체 Screen Space Overlay 캔버스(정렬 600)를 가진 Resources 프리팹이라 옵션 창·동의 팝업 위에 부모 없이 뜬다.
    /// 닫기 버튼 이름이 Btn_Close 라 뒤로가기(BackButtonHandler)로도 닫힌다.
    /// </summary>
    public class PrivacyPolicyPopupUI : MonoBehaviour, IPointerClickHandler
    {
        #region Variables

        private const string POPUP_RESOURCE_NAME = "Popup_PrivacyPolicy";
        private const string KOREAN_POLICY_PATH = "PrivacyPolicy/PrivacyPolicy_Korean";
        private const string ENGLISH_POLICY_PATH = "PrivacyPolicy/PrivacyPolicy_English";
        private const string TITLE_KEY = "UI_Consent_Policy";
        private const string TITLE_FALLBACK = "개인정보처리방침";
        private const string CLOSE_KEY = "UI_PrivacyPolicy_Close";
        private const string CLOSE_FALLBACK = "닫기";
        private const string OPEN_WEB_KEY = "UI_PrivacyPolicy_OpenWeb";
        private const string OPEN_WEB_FALLBACK = "웹에서 보기";

        // 같은 팝업이 두 번 겹쳐 뜨지 않도록 열린 인스턴스를 하나만 기억한다.
        private static PrivacyPolicyPopupUI _openInstance;

        #endregion

        #region Fields

        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openWebButton;
        [SerializeField] private TextMeshProUGUI closeText;
        [SerializeField] private TextMeshProUGUI openWebText;

        private bool _isClosing;

        #endregion

        #region LifeCycle

        private void OnEnable()
        {
            _SubscribeEvents();
            _ApplyTexts();
            PopupTween.PlayShow(gameObject);
        }

        private void OnDisable()
        {
            _UnsubscribeEvents();
        }

        private void Awake()
        {
            LocalizeFontSwitcher.Register(titleText);
            LocalizeFontSwitcher.Register(bodyText);
            LocalizeFontSwitcher.Register(closeText);
            LocalizeFontSwitcher.Register(openWebText);
        }

        private void OnDestroy()
        {
            LocalizeFontSwitcher.Unregister(titleText);
            LocalizeFontSwitcher.Unregister(bodyText);
            LocalizeFontSwitcher.Unregister(closeText);
            LocalizeFontSwitcher.Unregister(openWebText);

            if (_openInstance == this)
                _openInstance = null;
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            closeButton.onClick.AddListener(_OnClickClose);
            openWebButton.onClick.AddListener(_OnClickOpenWeb);
            Localization.OnLanguageChanged += _ApplyTexts;
        }

        private void _UnsubscribeEvents()
        {
            closeButton.onClick.RemoveListener(_OnClickClose);
            openWebButton.onClick.RemoveListener(_OnClickOpenWeb);
            Localization.OnLanguageChanged -= _ApplyTexts;
        }

        #endregion

        /// <summary>
        /// 앱 내 개인정보처리방침 팝업을 띄운다. 이미 열려 있으면 그것을 돌려준다.
        /// 프리팹이 없으면 웹 페이지를 대신 열고 null 을 돌려준다(정책 열람 수단은 항상 보장).
        /// </summary>
        public static PrivacyPolicyPopupUI Open()
        {
            if (_openInstance != null)
                return _openInstance;

            GameObject popupPrefab = Resources.Load<GameObject>(POPUP_RESOURCE_NAME);

            if (popupPrefab == null)
            {
                Debug.LogError($"[PrivacyPolicy] Resources에서 {POPUP_RESOURCE_NAME} 프리팹을 찾을 수 없어 웹 페이지로 엽니다.");
                Application.OpenURL(AnalyticsConsent.PrivacyPolicyUrl);

                return null;
            }

            GameObject popupObject = Instantiate(popupPrefab);

            if (!popupObject.TryGetComponent(out PrivacyPolicyPopupUI popup))
            {
                Debug.LogError($"[PrivacyPolicy] {POPUP_RESOURCE_NAME}에 PrivacyPolicyPopupUI가 없습니다.");
                Destroy(popupObject);
                Application.OpenURL(AnalyticsConsent.PrivacyPolicyUrl);

                return null;
            }

            _openInstance = popup;

            return popup;
        }

        // 본문의 <link="url"> (제3자 정책 링크)를 누르면 외부 브라우저로 연다.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (bodyText == null)
                return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(bodyText, eventData.position, eventData.pressEventCamera);

            if (linkIndex < 0)
                return;

            string url = bodyText.textInfo.linkInfo[linkIndex].GetLinkID();

            if (string.IsNullOrEmpty(url))
                return;

            Application.OpenURL(url);
        }

        private void _ApplyTexts()
        {
            titleText.text = LocalizeHelper.GetByKey(TITLE_KEY, TITLE_FALLBACK);
            closeText.text = LocalizeHelper.GetByKey(CLOSE_KEY, CLOSE_FALLBACK);
            openWebText.text = LocalizeHelper.GetByKey(OPEN_WEB_KEY, OPEN_WEB_FALLBACK);
            bodyText.text = _LoadPolicyText();

            // 언어가 바뀌면 다른 원문이 들어오므로 맨 위부터 다시 읽게 한다.
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        private string _LoadPolicyText()
        {
            bool isKorean = Localization.CurrentLanguage == SystemLanguage.Korean;
            string path = isKorean ? KOREAN_POLICY_PATH : ENGLISH_POLICY_PATH;
            TextAsset policyAsset = Resources.Load<TextAsset>(path);

            if (policyAsset == null)
            {
                Debug.LogError($"[PrivacyPolicy] Resources/{path}.txt 를 찾을 수 없습니다.");

                return AnalyticsConsent.PrivacyPolicyUrl;
            }

            return policyAsset.text;
        }

        private void _OnClickClose()
        {
            if (_isClosing)
                return;

            _isClosing = true;
            _PlayClickSound();

            PopupTween.PlayHide(gameObject, () => Destroy(gameObject));
        }

        private void _OnClickOpenWeb()
        {
            _PlayClickSound();
            Application.OpenURL(AnalyticsConsent.PrivacyPolicyUrl);
        }

        private void _PlayClickSound()
        {
            if (SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);
        }
    }
}
