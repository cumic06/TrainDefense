using Cumic;
using Cumic.Events;
using Cumic.Sequence;
using TMPro;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Manager;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    // 인게임 우상단 일시정지 버튼으로 여닫는 일시정지 메뉴(딤 + 옵션/도감/포기 버튼).
    // 도감 버튼 클릭은 같은 GO의 CollectionButton이 처리하고, 여기서는 라벨만 로컬라이즈한다.
    // 열려 있는 동안 OverlayPhase.MenuPause로 게임을 멈추고, 옵션 창은 이 위에 겹쳐 뜬다.
    public class PauseUI : MonoBehaviour
    {
        #region Fields
        [Header("메뉴 버튼")]
        [SerializeField]
        private Button optionButton;
        [SerializeField]
        private TMP_Text optionText;
        [SerializeField]
        private TMP_Text collectionText;
        // 포기 버튼. 필드 이름은 로비로 나가던 시절 그대로다 — 바꾸면 인스펙터 배선이 끊긴다.
        [SerializeField]
        private Button lobbyButton;
        [SerializeField]
        private TMP_Text lobbyText;
        [SerializeField]
        private Button closeButton;

        [Header("옵션 창 (씬의 Group_Option)")]
        [SerializeField]
        private OptionUI optionUI;
        #endregion

        private void Awake()
        {
            if (optionButton != null)
                optionButton.onClick.AddListener(_OnClickOption);

            if (lobbyButton != null)
                lobbyButton.onClick.AddListener(_OnClickGiveUp);

            if (closeButton != null)
                closeButton.onClick.AddListener(HidePauseUI);

            Localization.OnLanguageChanged += _RefreshTexts;
            Localization.OnInitialized += _RefreshTexts;
        }

        private void OnEnable()
        {
            _RefreshTexts();
            PopupTween.PlayShow(gameObject);
        }

        private void OnDestroy()
        {
            Localization.OnLanguageChanged -= _RefreshTexts;
            Localization.OnInitialized -= _RefreshTexts;
        }

        // 우상단 일시정지 버튼: 열려 있으면 닫고, 닫혀 있으면 연다.
        public void OnClickPauseButton()
        {
            if (gameObject.activeSelf)
            {
                HidePauseUI();
                return;
            }

            // 게임오버 슬로모션·결과 화면 중에는 일시정지를 열지 않는다. (슬로모션 중 로비로 나가면 DDOL TimeManager 코루틴이 GameEndEvent를 다음 씬에 쏘는 것도 함께 차단)
            if (TimeManager.Instance != null && TimeManager.Instance.IsGameOverSlowing)
                return;

            if (InGameSequence.Instance != null && InGameSequence.Instance.CurrentBase == BasePhase.GameOver)
                return;

            ShowPauseUI();
        }

        public void ShowPauseUI()
        {
            SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_WindowOpen, ignoreSuppress: true);

            gameObject.SetActive(true);

            if (InGameSequence.Instance != null)
                InGameSequence.Instance.PushOverlay(OverlayPhase.MenuPause);
            else if (TimeManager.Instance != null)
                TimeManager.Instance.Pause();
        }

        public void HidePauseUI()
        {
            SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_WindowClose, ignoreSuppress: true);

            PopupTween.PlayHide(gameObject, () => gameObject.SetActive(false));

            if (InGameSequence.Instance != null)
                InGameSequence.Instance.PopOverlay(OverlayPhase.MenuPause);
            else if (TimeManager.Instance != null)
                TimeManager.Instance.Resume();
        }

        // 옵션 창을 일시정지 메뉴 위에 연다. MenuPause 오버레이는 유지되므로 옵션을 닫아도 계속 멈춰 있다.
        private void _OnClickOption()
        {
            if (optionUI != null)
                optionUI.ShowOptionUI();
        }

        // 포기 — 씬을 떠나지 않고 그 자리에서 판을 끝낸다. 패배와 같은 결과창이 뜬다.
        // 세이브 삭제는 GameEndEvent를 받은 RunSaveManager가 한다(판이 끝났으니 이어할 대상이 없다).
        private void _OnClickGiveUp()
        {
            // 결과 화면(BasePhase.GameOver)으로 먼저 넘긴다 — 메뉴 오버레이를 먼저 걷으면 그 사이 게임이 한 번 재개된다.
            GameEventSystem.Publish(new GameEndEvent(false));

            HidePauseUI();
        }

        private void _RefreshTexts()
        {
            if (optionText != null)
                optionText.text = LocalizeHelper.GetByKey("UI_Reset_Option", "옵션");

            if (collectionText != null)
                collectionText.text = LocalizeHelper.GetByKey("Collection_Title", "도감");

            if (lobbyText != null)
                lobbyText.text = LocalizeHelper.GetByKey("UI_GiveUp", "포기");
        }
    }
}
