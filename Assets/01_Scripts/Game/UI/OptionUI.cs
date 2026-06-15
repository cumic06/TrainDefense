using Cumic.Sequence;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using TrainDefense.Game.UI;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrainDefense
{
   public class OptionUI : MonoBehaviour
   {
      #region Fields
      [SerializeField]
      private Slider bgmSlider;
      [SerializeField]
      private Button bgmMuteButton;
      [SerializeField]
      private Slider sfxSlider;
      [SerializeField]
      private Button sfxMuteButton;
      [SerializeField]
      private Toggle hapticToggle;
      [SerializeField]
      private Button deletePlayerPrefsButton;

      [SerializeField]
      private Sprite muteSprite;
      [SerializeField]
      private Sprite unMuteSprite;

      [Header("인게임 전용 로비 버튼 (로비 씬에서는 자동 숨김)")]
      [SerializeField]
      private Transform lobbyButton;

      [Header("언어 선택 버튼")]
      [SerializeField]
      private Button koreanButton;
      [SerializeField]
      private Button englishButton;
      [SerializeField]
      private Button japaneseButton;

      [Header("탭")]
      [SerializeField]
      private Button soundTabButton;
      [SerializeField]
      private Button languageTabButton;
      [SerializeField]
      private GameObject soundPanel;
      [SerializeField]
      private GameObject languagePanel;
      #endregion

      private static readonly Color _langSelectedColor = new Color(1f, 0.85f, 0.3f);
      private static readonly Color _langNormalColor = Color.white;
      private static readonly Color _tabSelectedColor = Color.white;
      private static readonly Color _tabNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);

      // 로비 씬의 빌드 인덱스 (ChangeSceneButton.LobbySceneIndex와 동일)
      private const int LobbySceneBuildIndex = 1;

      private void Awake()
      {
         _SubscribeListeners();
      }

      private void OnEnable()
      {
         _ShowTab(isSound: true);

         if (SoundManager.Instance == null) return;

         _SetBGMSliderValue(SoundManager.Instance.BGMVolume);
         _SetSFXSliderValue(SoundManager.Instance.SFXVolume);
         _SetBGMMuteSprite();
         _SetSFXMuteSprite();
         _RefreshHapticToggle();
         _RefreshLanguageButtons();
      }

      private void OnDestroy()
      {
         _UnSubscribeListeners();
      }

      private void _SubscribeListeners()
      {
         bgmMuteButton.onClick.AddListener(_MuteBGM);
         sfxMuteButton.onClick.AddListener(_MuteSFX);
         bgmSlider.onValueChanged.AddListener(_ChangeBGMVolume);
         sfxSlider.onValueChanged.AddListener(_ChangeSFXVolume);

         if (hapticToggle != null)
            hapticToggle.onValueChanged.AddListener(_OnHapticToggleChanged);

         if (deletePlayerPrefsButton != null)
            deletePlayerPrefsButton.onClick.AddListener(OnDeletePlayerPrefsClicked);

         if (koreanButton != null)
            koreanButton.onClick.AddListener(() => _OnLanguageButtonClick(SystemLanguage.Korean));

         if (englishButton != null)
            englishButton.onClick.AddListener(() => _OnLanguageButtonClick(SystemLanguage.English));

         if (japaneseButton != null)
            japaneseButton.onClick.AddListener(() => _OnLanguageButtonClick(SystemLanguage.Japanese));

         if (soundTabButton != null)
            soundTabButton.onClick.AddListener(() => _ShowTab(isSound: true));

         if (languageTabButton != null)
            languageTabButton.onClick.AddListener(() => _ShowTab(isSound: false));

         Localization.OnLanguageChanged += _RefreshLanguageButtons;
      }

      private void _UnSubscribeListeners()
      {
         bgmMuteButton.onClick.RemoveAllListeners();
         sfxMuteButton.onClick.RemoveAllListeners();
         bgmSlider.onValueChanged.RemoveAllListeners();
         sfxSlider.onValueChanged.RemoveAllListeners();

         if (hapticToggle != null)
            hapticToggle.onValueChanged.RemoveListener(_OnHapticToggleChanged);

         if (deletePlayerPrefsButton != null)
            deletePlayerPrefsButton.onClick.RemoveAllListeners();

         if (koreanButton != null)
            koreanButton.onClick.RemoveAllListeners();

         if (englishButton != null)
            englishButton.onClick.RemoveAllListeners();

         if (japaneseButton != null)
            japaneseButton.onClick.RemoveAllListeners();

         if (soundTabButton != null)
            soundTabButton.onClick.RemoveAllListeners();

         if (languageTabButton != null)
            languageTabButton.onClick.RemoveAllListeners();

         Localization.OnLanguageChanged -= _RefreshLanguageButtons;
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

      private void _ChangeBGMVolume(float value)
      {
         SoundManager.Instance.SetBGMVolume(value);
      }

      private void _ChangeSFXVolume(float value)
      {
         SoundManager.Instance.SetSFXVolume(value);
      }

      private void _MuteBGM()
      {
         SoundManager.Instance.MuteBGM();
         _SetBGMMuteSprite();
      }

      private void _MuteSFX()
      {
         SoundManager.Instance.MuteSFX();
         _SetSFXMuteSprite();
      }

      private void _SetBGMSliderValue(float value)
      {
         bgmSlider.SetValueWithoutNotify(value);
      }

      private void _SetSFXSliderValue(float value)
      {
         sfxSlider.SetValueWithoutNotify(value);
      }

      private void _SetBGMMuteSprite()
      {
         Sprite sprite = SoundManager.Instance.IsBgmMuted ? muteSprite : unMuteSprite;
         bgmMuteButton.image.sprite = sprite;
      }

      private void _SetSFXMuteSprite()
      {
         Sprite sprite = SoundManager.Instance.IsSfxMuted ? muteSprite : unMuteSprite;
         sfxMuteButton.image.sprite = sprite;
      }

      private void _OnHapticToggleChanged(bool isEnabled)
      {
         if (HapticManager.Instance != null)
         {
            HapticManager.Instance.SetEnabled(isEnabled);
         }
         else if (UserDataManager.Instance != null)
         {
            UserDataManager.Instance.SetHapticEnabled(isEnabled);
         }
      }

      private void _ShowTab(bool isSound)
      {
         if (soundPanel != null)
            soundPanel.SetActive(isSound);

         if (languagePanel != null)
            languagePanel.SetActive(!isSound);

         if (soundTabButton != null)
            soundTabButton.image.color = isSound ? _tabSelectedColor : _tabNormalColor;

         if (languageTabButton != null)
            languageTabButton.image.color = isSound ? _tabNormalColor : _tabSelectedColor;
      }

      private void _OnLanguageButtonClick(SystemLanguage language)
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);
         Localization.SetLanguage(language);
      }

      private void _RefreshLanguageButtons()
      {
         SystemLanguage current = Localization.CurrentLanguage;

         if (koreanButton != null)
            koreanButton.image.color = current == SystemLanguage.Korean ? _langSelectedColor : _langNormalColor;

         if (englishButton != null)
            englishButton.image.color = current == SystemLanguage.English ? _langSelectedColor : _langNormalColor;

         if (japaneseButton != null)
            japaneseButton.image.color = current == SystemLanguage.Japanese ? _langSelectedColor : _langNormalColor;
      }

      private void _RefreshHapticToggle()
      {
         if (hapticToggle == null)
         {
            return;
         }

         bool isEnabled = true;
         if (HapticManager.Instance != null)
         {
            isEnabled = HapticManager.Instance.IsEnabled;
         }
         else if (UserDataManager.Instance != null)
         {
            isEnabled = UserDataManager.Instance.IsHapticEnabled;
         }

         hapticToggle.SetIsOnWithoutNotify(isEnabled);
      }
   }
}
