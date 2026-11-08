using System;
using TMPro;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 종착역에 도착하면 무한모드로 계속할지 묻는 팝업. "계속하기"/"그만두기" 중 하나를 골라야 닫힌다.
    /// 고른 뒤 무엇을 할지는 StageManager가 콜백으로 정한다 — 이 팝업은 묻고 답을 돌려주는 일만 한다.
    /// 버튼 이름에 close/cancel이 없어 뒤로가기(BackButtonHandler)로 실수로 판이 끝나지 않는다.
    /// 자체 Screen Space Overlay 캔버스를 가진 Resources 프리팹이라 부모 없이 띄운다.
    /// </summary>
    public class InfiniteModePopupUI : MonoBehaviour
    {
        #region Variables

        private const string POPUP_RESOURCE_NAME = "Popup_InfiniteMode";
        private const string TITLE_KEY = "InfiniteMode_Title";
        private const string TITLE_FALLBACK = "종착역에 도착했습니다";
        private const string DESCRIPTION_KEY = "InfiniteMode_Desc";
        private const string DESCRIPTION_FALLBACK = "무한모드로 계속할까요? 적이 점점 더 강해집니다.";
        private const string CONTINUE_KEY = "InfiniteMode_Continue";
        private const string CONTINUE_FALLBACK = "계속하기";
        private const string FINISH_KEY = "InfiniteMode_Finish";
        private const string FINISH_FALLBACK = "그만두기";

        #endregion

        #region Fields

        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button finishButton;
        [SerializeField] private TextMeshProUGUI continueText;
        [SerializeField] private TextMeshProUGUI finishText;

        private Action _onContinue;
        private Action _onFinish;
        private bool _isSelected;

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
            LocalizeFontSwitcher.Register(descriptionText);
            LocalizeFontSwitcher.Register(continueText);
            LocalizeFontSwitcher.Register(finishText);
        }

        private void OnDestroy()
        {
            LocalizeFontSwitcher.Unregister(titleText);
            LocalizeFontSwitcher.Unregister(descriptionText);
            LocalizeFontSwitcher.Unregister(continueText);
            LocalizeFontSwitcher.Unregister(finishText);
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            continueButton.onClick.AddListener(_OnClickContinue);
            finishButton.onClick.AddListener(_OnClickFinish);
            Localization.OnLanguageChanged += _ApplyTexts;
        }

        private void _UnsubscribeEvents()
        {
            continueButton.onClick.RemoveListener(_OnClickContinue);
            finishButton.onClick.RemoveListener(_OnClickFinish);
            Localization.OnLanguageChanged -= _ApplyTexts;
        }

        #endregion

        /// <summary>Resources의 프리팹을 띄운다. 프리팹이 없으면 null — 호출자는 바로 결과창으로 넘어가면 된다.</summary>
        public static InfiniteModePopupUI Open(Action onContinue, Action onFinish)
        {
            GameObject popupPrefab = Resources.Load<GameObject>(POPUP_RESOURCE_NAME);

            if (popupPrefab == null)
            {
                Debug.LogError($"[InfiniteMode] Resources에서 {POPUP_RESOURCE_NAME} 프리팹을 찾을 수 없습니다.");

                return null;
            }

            GameObject popupObject = Instantiate(popupPrefab);

            if (!popupObject.TryGetComponent(out InfiniteModePopupUI popup))
            {
                Debug.LogError($"[InfiniteMode] {POPUP_RESOURCE_NAME}에 InfiniteModePopupUI가 없습니다.");
                Destroy(popupObject);

                return null;
            }

            popup._onContinue = onContinue;
            popup._onFinish = onFinish;
            popup._isSelected = false;

            return popup;
        }

        /// <summary>고르지 않고 즉시 닫는다(씬 이탈 등). 콜백은 부르지 않는다.</summary>
        public void Close()
        {
            _isSelected = true;
            _onContinue = null;
            _onFinish = null;
            Destroy(gameObject);
        }

        private void _ApplyTexts()
        {
            titleText.text = LocalizeHelper.GetByKey(TITLE_KEY, TITLE_FALLBACK);
            descriptionText.text = LocalizeHelper.GetByKey(DESCRIPTION_KEY, DESCRIPTION_FALLBACK);
            continueText.text = LocalizeHelper.GetByKey(CONTINUE_KEY, CONTINUE_FALLBACK);
            finishText.text = LocalizeHelper.GetByKey(FINISH_KEY, FINISH_FALLBACK);
        }

        private void _OnClickContinue() => _Select(_onContinue);

        private void _OnClickFinish() => _Select(_onFinish);

        // 두 번 눌러도 한 번만 처리한다. 콜백을 먼저 비워 두고 부른다(콜백 안에서 Close가 불려도 안전하게).
        private void _Select(Action callback)
        {
            if (_isSelected)
                return;

            _isSelected = true;
            _onContinue = null;
            _onFinish = null;

            PopupTween.PlayHide(gameObject, () => Destroy(gameObject));
            callback?.Invoke();
        }
    }
}
