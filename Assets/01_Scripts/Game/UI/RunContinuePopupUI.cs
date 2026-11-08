using System;
using TMPro;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 판 도중 앱을 껐다가 다시 켜서 로비에 들어오면 "이어서 할지 / 처음부터 할지"를 묻는 팝업.
    /// 둘 중 하나를 골라야 닫힌다 — 고르기 전에 로비(영구 강화 등)를 만질 수 없게 화면 전체를 딤으로 막는다.
    /// 고른 뒤 무엇을 할지는 RunContinueAutoEntry가 콜백으로 정한다 — 이 팝업은 묻고 답을 돌려주는 일만 한다.
    /// 버튼 이름에 close/cancel이 없어 뒤로가기(BackButtonHandler)로 닫히지 않는다.
    /// 자체 Screen Space Overlay 캔버스를 가진 Resources 프리팹이라 부모 없이 띄운다.
    /// </summary>
    public class RunContinuePopupUI : MonoBehaviour
    {
        #region Variables

        private const string POPUP_RESOURCE_NAME = "Popup_RunContinue";
        private const string TITLE_KEY = "RunContinue_Title";
        private const string TITLE_FALLBACK = "이어서 하시겠습니까?";
        private const string DESCRIPTION_KEY = "RunContinue_Desc";
        private const string DESCRIPTION_FALLBACK = "진행 중이던 게임이 있습니다.";
        private const string PROGRESS_KEY = "RunContinue_Progress";
        private const string PROGRESS_FALLBACK = "{0}번째 역 · Lv.{1}";
        private const string CONTINUE_KEY = "RunContinue_Continue";
        private const string CONTINUE_FALLBACK = "이어하기";
        private const string RESTART_KEY = "RunContinue_Restart";
        private const string RESTART_FALLBACK = "처음부터 하기";

        #endregion

        #region Fields

        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private TextMeshProUGUI continueText;
        [SerializeField] private TextMeshProUGUI restartText;

        private Action _onContinue;
        private Action _onRestart;
        private bool _isSelected;
        private int _stationPassedCount;
        private int _playerLevel;

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
            LocalizeFontSwitcher.Register(restartText);
        }

        private void OnDestroy()
        {
            LocalizeFontSwitcher.Unregister(titleText);
            LocalizeFontSwitcher.Unregister(descriptionText);
            LocalizeFontSwitcher.Unregister(continueText);
            LocalizeFontSwitcher.Unregister(restartText);
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            continueButton.onClick.AddListener(_OnClickContinue);
            restartButton.onClick.AddListener(_OnClickRestart);
            Localization.OnLanguageChanged += _ApplyTexts;
        }

        private void _UnsubscribeEvents()
        {
            continueButton.onClick.RemoveListener(_OnClickContinue);
            restartButton.onClick.RemoveListener(_OnClickRestart);
            Localization.OnLanguageChanged -= _ApplyTexts;
        }

        #endregion

        /// <summary>
        /// Resources의 프리팹을 띄운다. 프리팹이 없으면 null — 호출자는 대체 동작(바로 이어하기)을 정하면 된다.
        /// </summary>
        /// <param name="stationPassedCount">저장된 판에서 지나온 역 수. 진행 정보 표기용.</param>
        /// <param name="playerLevel">저장된 판의 레벨. 진행 정보 표기용.</param>
        public static RunContinuePopupUI Open(int stationPassedCount, int playerLevel, Action onContinue, Action onRestart)
        {
            GameObject popupPrefab = Resources.Load<GameObject>(POPUP_RESOURCE_NAME);

            if (popupPrefab == null)
            {
                Debug.LogError($"[RunSave] Resources에서 {POPUP_RESOURCE_NAME} 프리팹을 찾을 수 없습니다.");

                return null;
            }

            GameObject popupObject = Instantiate(popupPrefab);

            if (!popupObject.TryGetComponent(out RunContinuePopupUI popup))
            {
                Debug.LogError($"[RunSave] {POPUP_RESOURCE_NAME}에 RunContinuePopupUI가 없습니다.");
                Destroy(popupObject);

                return null;
            }

            popup._onContinue = onContinue;
            popup._onRestart = onRestart;
            popup._isSelected = false;
            popup._stationPassedCount = stationPassedCount;
            popup._playerLevel = playerLevel;
            // 인스턴스화 시점의 OnEnable은 진행 정보가 들어오기 전이라, 값을 넣은 뒤 텍스트를 다시 채운다.
            popup._ApplyTexts();

            return popup;
        }

        private void _ApplyTexts()
        {
            titleText.text = LocalizeHelper.GetByKey(TITLE_KEY, TITLE_FALLBACK);
            continueText.text = LocalizeHelper.GetByKey(CONTINUE_KEY, CONTINUE_FALLBACK);
            restartText.text = LocalizeHelper.GetByKey(RESTART_KEY, RESTART_FALLBACK);

            string description = LocalizeHelper.GetByKey(DESCRIPTION_KEY, DESCRIPTION_FALLBACK);
            string progressFormat = LocalizeHelper.GetByKey(PROGRESS_KEY, PROGRESS_FALLBACK);
            descriptionText.text = $"{description}\n{_FormatProgress(progressFormat)}";
        }

        // 번역 문자열의 자리표시자가 깨져 있어도 팝업 자체는 떠야 하므로 폴백 형식으로 한 번 더 시도한다.
        private string _FormatProgress(string format)
        {
            try
            {
                return string.Format(format, _stationPassedCount, _playerLevel);
            }
            catch (FormatException)
            {
                return string.Format(PROGRESS_FALLBACK, _stationPassedCount, _playerLevel);
            }
        }

        private void _OnClickContinue() => _Select(_onContinue);

        private void _OnClickRestart() => _Select(_onRestart);

        // 두 번 눌러도 한 번만 처리한다. 콜백을 먼저 비워 두고 부른다(콜백 안에서 씬이 바뀌어도 안전하게).
        private void _Select(Action callback)
        {
            if (_isSelected)
                return;

            _isSelected = true;
            _onContinue = null;
            _onRestart = null;

            PopupTween.PlayHide(gameObject, () => Destroy(gameObject));
            callback?.Invoke();
        }
    }
}
