using System;
using TMPro;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.Consent
{
    /// <summary>
    /// OAuthScene에서 게임을 시작하기 전에 받는 이용 동의 팝업.
    /// [필수] 개인정보처리방침 확인 체크를 해야 [확인] 버튼이 눌리고, [선택] 분석 데이터 수집은 체크하지 않아도 넘어간다.
    /// [모두 동의]는 두 항목을 한 번에 켜는 편의 체크이며, 선택 항목은 기본값이 꺼져 있다(옵트인 — 미리 체크 금지).
    /// 확인을 누르면 선택 항목 체크 여부(분석 수집 동의)를 콜백으로 넘긴다. 저장·진행 결정은 호출자(AnalyticsManager)가 한다.
    /// 방침 [보기]는 앱 내 팝업(PrivacyPolicyPopupUI)으로 연다.
    /// 버튼 이름에 close/cancel이 없어 뒤로가기(BackButtonHandler)로 건너뛸 수 없다.
    /// </summary>
    public class ConsentPopupUI : MonoBehaviour
    {
        #region Variables

        private const string TITLE_KEY = "UI_Consent_Title";
        private const string TITLE_FALLBACK = "개인정보 수집 동의";
        private const string BODY_KEY = "UI_Consent_AgreementBody";
        private const string BODY_FALLBACK = "게임을 시작하려면 개인정보처리방침을 확인해 주세요.\n분석 데이터 수집은 선택이며, 설정에서 언제든 변경할 수 있습니다.";
        private const string REQUIRED_KEY = "UI_Consent_Required";
        private const string REQUIRED_FALLBACK = "[필수] 개인정보처리방침을 읽고 이해했습니다";
        private const string OPTIONAL_KEY = "UI_Consent_Optional";
        private const string OPTIONAL_FALLBACK = "[선택] 분석 데이터 수집에 동의합니다";
        private const string ALL_KEY = "UI_Consent_All";
        private const string ALL_FALLBACK = "모두 동의 (선택 항목 포함)";
        private const string OPTIONAL_REASON_KEY = "UI_Consent_OptionalReason";
        private const string OPTIONAL_REASON_FALLBACK = "수집된 데이터는 밸런스 조정과 버그 수정에만 쓰여요.";
        private const string VIEW_KEY = "UI_Consent_View";
        private const string VIEW_FALLBACK = "보기";
        private const string CONFIRM_KEY = "UI_Consent_Confirm";
        private const string CONFIRM_FALLBACK = "확인";

        // [확인] 버튼이 잠겨 있을 때 흐리게 보이는 정도.
        private const float DISABLED_ALPHA = 0.45f;

        #endregion

        #region Fields

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Toggle allToggle;
        [SerializeField] private TMP_Text allText;
        [SerializeField] private Toggle requiredToggle;
        [SerializeField] private TMP_Text requiredText;
        [SerializeField] private Button policyButton;
        [SerializeField] private TMP_Text policyText;
        [SerializeField] private Toggle optionalToggle;
        [SerializeField] private TMP_Text optionalText;
        [SerializeField] private TMP_Text optionalReasonText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private CanvasGroup confirmCanvasGroup;

        private Action<bool> _onConfirm;

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
            _SubscribeEvents();
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            allToggle.onValueChanged.AddListener(_OnAllChanged);
            requiredToggle.onValueChanged.AddListener(_OnItemChanged);
            optionalToggle.onValueChanged.AddListener(_OnItemChanged);
            policyButton.onClick.AddListener(_OnPolicyLink);
            confirmButton.onClick.AddListener(_OnConfirm);
        }

        private void _UnsubscribeEvents()
        {
            allToggle.onValueChanged.RemoveListener(_OnAllChanged);
            requiredToggle.onValueChanged.RemoveListener(_OnItemChanged);
            optionalToggle.onValueChanged.RemoveListener(_OnItemChanged);
            policyButton.onClick.RemoveListener(_OnPolicyLink);
            confirmButton.onClick.RemoveListener(_OnConfirm);
        }

        #endregion

        /// <summary>
        /// 팝업을 띄운다. 필수 체크는 항상 꺼진 채로 시작하고, 선택 체크는 isOptionalChecked(이전 동의값)로 시작한다.
        /// [확인]을 누르면 선택 항목 체크 여부를 onConfirm으로 넘긴다.
        /// </summary>
        public void Show(bool isOptionalChecked, Action<bool> onConfirm)
        {
            _onConfirm = onConfirm;
            requiredToggle.SetIsOnWithoutNotify(false);
            optionalToggle.SetIsOnWithoutNotify(isOptionalChecked);
            _SyncAllToggle();
            _RefreshConfirmState();
            _RefreshTexts();
            gameObject.SetActive(true);
        }

        private void _RefreshTexts()
        {
            _SetText(titleText, TITLE_KEY, TITLE_FALLBACK);
            _SetText(bodyText, BODY_KEY, BODY_FALLBACK);
            _SetText(requiredText, REQUIRED_KEY, REQUIRED_FALLBACK);
            _SetText(optionalText, OPTIONAL_KEY, OPTIONAL_FALLBACK);
            _SetText(allText, ALL_KEY, ALL_FALLBACK);
            _SetText(optionalReasonText, OPTIONAL_REASON_KEY, OPTIONAL_REASON_FALLBACK);
            _SetText(policyText, VIEW_KEY, VIEW_FALLBACK);
            _SetText(confirmText, CONFIRM_KEY, CONFIRM_FALLBACK);
        }

        private void _SetText(TMP_Text target, string key, string fallback)
        {
            if (target == null)
                return;

            target.text = LocalizeHelper.GetByKey(key, fallback);
        }

        // [모두 동의]는 필수·선택을 한 번에 켜고 끈다. 각 항목은 그 뒤에도 따로 풀 수 있다.
        private void _OnAllChanged(bool isOn)
        {
            requiredToggle.SetIsOnWithoutNotify(isOn);
            optionalToggle.SetIsOnWithoutNotify(isOn);
            _RefreshConfirmState();
        }

        private void _OnItemChanged(bool isOn)
        {
            _SyncAllToggle();
            _RefreshConfirmState();
        }

        // 항목이 전부 켜져 있을 때만 [모두 동의]도 켜진 것으로 보인다.
        private void _SyncAllToggle()
        {
            allToggle.SetIsOnWithoutNotify(requiredToggle.isOn && optionalToggle.isOn);
        }

        // 필수 항목을 체크해야만 [확인]이 눌린다.
        private void _RefreshConfirmState()
        {
            bool canConfirm = requiredToggle.isOn;
            confirmButton.interactable = canConfirm;

            if (confirmCanvasGroup != null)
                confirmCanvasGroup.alpha = canConfirm ? 1f : DISABLED_ALPHA;
        }

        private void _OnPolicyLink()
        {
            PrivacyPolicyPopupUI.Open();
        }

        private void _OnConfirm()
        {
            if (!requiredToggle.isOn)
                return;

            var callback = _onConfirm;
            _onConfirm = null;
            bool isOptionalGranted = optionalToggle.isOn;

            // 미리 배치된 팝업이라 파괴하지 않고 비활성으로 닫는다(재표시 가능).
            PopupTween.PlayHide(gameObject, () => gameObject.SetActive(false));
            callback?.Invoke(isOptionalGranted);
        }
    }
}
