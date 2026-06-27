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
   // 옵션 창의 프레임(탭 전환·열기/닫기·데이터 초기화·로비 이동)을 담당하는 메인 컨트롤러.
   // 각 탭 내부 컨트롤은 SoundOptionUI / AccessibilityOptionUI / LanguageOptionUI / PrivacyOptionUI 가 분담한다.
   public class OptionUI : MonoBehaviour
   {
      #region Fields
      [Header("탭")]
      [SerializeField]
      private Button soundTabButton;
      [SerializeField]
      private TMP_Text soundTabText;
      [SerializeField]
      private Button accessibilityTabButton;
      [SerializeField]
      private TMP_Text accessibilityTabText;
      [SerializeField]
      private Button languageTabButton;
      [SerializeField]
      private TMP_Text languageTabText;
      [SerializeField]
      private GameObject soundPanel;
      [SerializeField]
      private GameObject accessibilityPanel;
      [SerializeField]
      private GameObject languagePanel;

      [Header("데이터 초기화 / 튜토리얼")]
      [SerializeField]
      private Button deletePlayerPrefsButton;
      [SerializeField]
      private Button tutorialReplayButton;
      [SerializeField]
      private TMP_Text tutorialReplayText;

      [Header("인게임 전용 로비 버튼 (로비 씬에서는 자동 숨김)")]
      [SerializeField]
      private Transform lobbyButton;
      #endregion

      private static readonly Color _tabSelectedColor = Color.white;
      private static readonly Color _tabNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);

      // 로비 씬의 빌드 인덱스 (ChangeSceneButton.LobbySceneIndex와 동일)
      private const int LobbySceneBuildIndex = 1;

      private enum OptionTab { Sound, Accessibility, Language }

      private void Awake()
      {
         _SubscribeListeners();
      }

      private void OnEnable()
      {
         _ShowTab(OptionTab.Sound);
         _RefreshTabTexts();
      }

      private void OnDestroy()
      {
         _UnSubscribeListeners();
      }

      private void _SubscribeListeners()
      {
         if (soundTabButton != null)
            soundTabButton.onClick.AddListener(() => _ShowTab(OptionTab.Sound));

         if (accessibilityTabButton != null)
            accessibilityTabButton.onClick.AddListener(() => _ShowTab(OptionTab.Accessibility));

         if (languageTabButton != null)
            languageTabButton.onClick.AddListener(() => _ShowTab(OptionTab.Language));

         if (deletePlayerPrefsButton != null)
            deletePlayerPrefsButton.onClick.AddListener(OnDeletePlayerPrefsClicked);

         if (tutorialReplayButton != null)
            tutorialReplayButton.onClick.AddListener(_OnTutorialReplayClick);

         Localization.OnLanguageChanged += _RefreshTabTexts;
         Localization.OnInitialized += _RefreshTabTexts;
      }

      private void _UnSubscribeListeners()
      {
         if (soundTabButton != null)
            soundTabButton.onClick.RemoveAllListeners();

         if (accessibilityTabButton != null)
            accessibilityTabButton.onClick.RemoveAllListeners();

         if (languageTabButton != null)
            languageTabButton.onClick.RemoveAllListeners();

         if (deletePlayerPrefsButton != null)
            deletePlayerPrefsButton.onClick.RemoveAllListeners();

         if (tutorialReplayButton != null)
            tutorialReplayButton.onClick.RemoveListener(_OnTutorialReplayClick);

         Localization.OnLanguageChanged -= _RefreshTabTexts;
         Localization.OnInitialized -= _RefreshTabTexts;
      }

      public void OnDeletePlayerPrefsClicked()
      {
         // 무엇을 초기화할지 고르는 선택형 팝업을 띄운다. (실제 초기화는 팝업 내부에서 UserDataManager를 통해 수행)
         ResetSelectPopupUI.Show();
      }

      public void ShowOptionUI()
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_WindowOpen, ignoreSuppress: true);

         gameObject.SetActive(true);
         _RefreshLobbyButton();

         if (InGameSequence.Instance != null)
            InGameSequence.Instance.PushOverlay(OverlayPhase.Option);
         else if (TimeManager.Instance != null)
            TimeManager.Instance.Pause();
      }

      public void HideOptionUI()
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_WindowClose, ignoreSuppress: true);

         gameObject.SetActive(false);

         if (InGameSequence.Instance != null)
            InGameSequence.Instance.PopOverlay(OverlayPhase.Option);
         else if (TimeManager.Instance != null)
            TimeManager.Instance.Resume();
      }

      // 로비 씬에서는 이미 로비이므로 인게임 전용 로비 버튼을 숨긴다.
      private void _RefreshLobbyButton()
      {
         if (lobbyButton == null) return;

         bool isLobbyScene = SceneManager.GetActiveScene().buildIndex == LobbySceneBuildIndex;
         lobbyButton.gameObject.SetActive(!isLobbyScene);
      }

      private void _ShowTab(OptionTab tab)
      {
         if (soundPanel != null)
            soundPanel.SetActive(tab == OptionTab.Sound);

         if (accessibilityPanel != null)
            accessibilityPanel.SetActive(tab == OptionTab.Accessibility);

         if (languagePanel != null)
            languagePanel.SetActive(tab == OptionTab.Language);

         if (soundTabButton != null)
            soundTabButton.image.color = tab == OptionTab.Sound ? _tabSelectedColor : _tabNormalColor;

         if (accessibilityTabButton != null)
            accessibilityTabButton.image.color = tab == OptionTab.Accessibility ? _tabSelectedColor : _tabNormalColor;

         if (languageTabButton != null)
            languageTabButton.image.color = tab == OptionTab.Language ? _tabSelectedColor : _tabNormalColor;
      }

      // 튜토리얼·인트로 진행 기록을 초기화한다. 인트로가 현재 씬(로비 등)에 있으면 즉시 다시 재생하고,
      // 없으면 다음 게임 진입 시 튜토리얼·인트로가 처음부터 다시 표시된다.
      private void _OnTutorialReplayClick()
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);

         if (UserDataManager.Instance != null)
            UserDataManager.Instance.ResetTutorialData();

         if (IntroManager.Instance != null)
            IntroManager.Instance.Replay();
      }

      // Sound·Accessibility·Language 탭 라벨과 튜토리얼 다시보기 라벨을 현재 언어로 갱신한다.
      private void _RefreshTabTexts()
      {
         _ApplyTabText(soundTabText, "UI_Option_Tab_Sound", "사운드");
         _ApplyTabText(accessibilityTabText, "UI_Option_Tab_Accessibility", "접근성");
         _ApplyTabText(languageTabText, "UI_Option_Tab_Language", "언어");

         if (tutorialReplayText != null)
            tutorialReplayText.text = LocalizeHelper.GetByKey("UI_Option_Tutorial_Replay", "튜토리얼 다시 보기");
      }

      // 탭 라벨을 현재 언어로 설정하고, 긴 언어에서 탭 폭을 넘지 않도록 자동 축소한다.
      private void _ApplyTabText(TMP_Text text, string key, string fallback)
      {
         if (text == null) return;

         text.text = LocalizeHelper.GetByKey(key, fallback);
         text.enableAutoSizing = true;
         text.fontSizeMin = 18;
         text.fontSizeMax = 40;
      }
   }
}
