using System.Collections.Generic;
using TMPro;
using Cumic.Sequence;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Intro;
using TrainDefense.Game.Manager;
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
      private Toggle cameraShakeToggle;
      [SerializeField]
      private Button colorblindButton;
      [SerializeField]
      private TMP_Text colorblindLabel;
      [SerializeField]
      private TMP_Text accessibilityTabText;
      [SerializeField]
      private TMP_Text cameraShakeLabel;
      [SerializeField]
      private Button deletePlayerPrefsButton;
      [SerializeField]
      private Button tutorialReplayButton;
      [SerializeField]
      private TMP_Text tutorialReplayText;

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
      [SerializeField]
      private TMP_Dropdown languageDropdown;
      // 드롭다운 인덱스 → 언어코드 매핑(런타임에 채움)
      private readonly List<string> _languageCodes = new();

      [Header("탭")]
      [SerializeField]
      private Button soundTabButton;
      [SerializeField]
      private TMP_Text soundTabText;
      [SerializeField]
      private Button languageTabButton;
      [SerializeField]
      private TMP_Text languageTabText;
      [SerializeField]
      private GameObject soundPanel;
      [SerializeField]
      private GameObject languagePanel;
      [SerializeField]
      private Button accessibilityTabButton;
      [SerializeField]
      private GameObject accessibilityPanel;
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
         _ShowTab(OptionTab.Sound);

         if (SoundManager.Instance == null) return;

         _SetBGMSliderValue(SoundManager.Instance.BGMVolume);
         _SetSFXSliderValue(SoundManager.Instance.SFXVolume);
         _SetBGMMuteSprite();
         _SetSFXMuteSprite();
         _RefreshHapticToggle();
         _RefreshCameraShakeToggle();
         _RefreshColorblindLabel();
         _RefreshLanguageButtons();
         _RefreshLanguageDropdown();
         _RefreshAccessibilityTexts();
         _RefreshTabTexts();
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

         if (cameraShakeToggle != null)
            cameraShakeToggle.onValueChanged.AddListener(_OnCameraShakeToggleChanged);

         if (colorblindButton != null)
            colorblindButton.onClick.AddListener(_OnColorblindButtonClick);

         if (deletePlayerPrefsButton != null)
            deletePlayerPrefsButton.onClick.AddListener(OnDeletePlayerPrefsClicked);

         if (tutorialReplayButton != null)
            tutorialReplayButton.onClick.AddListener(_OnTutorialReplayClick);

         if (koreanButton != null)
            koreanButton.onClick.AddListener(() => _OnLanguageButtonClick(SystemLanguage.Korean));

         if (englishButton != null)
            englishButton.onClick.AddListener(() => _OnLanguageButtonClick(SystemLanguage.English));

         if (japaneseButton != null)
            japaneseButton.onClick.AddListener(() => _OnLanguageButtonClick(SystemLanguage.Japanese));

         if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(_OnLanguageDropdownChanged);

         if (soundTabButton != null)
            soundTabButton.onClick.AddListener(() => _ShowTab(OptionTab.Sound));

         if (accessibilityTabButton != null)
            accessibilityTabButton.onClick.AddListener(() => _ShowTab(OptionTab.Accessibility));

         if (languageTabButton != null)
            languageTabButton.onClick.AddListener(() => _ShowTab(OptionTab.Language));

         Localization.OnLanguageChanged += _RefreshLanguageButtons;
         Localization.OnLanguageChanged += _RefreshLanguageDropdown;
         Localization.OnLanguageChanged += _RefreshColorblindLabel;
         Localization.OnLanguageChanged += _RefreshAccessibilityTexts;
         Localization.OnLanguageChanged += _RefreshTabTexts;

         // 로컬라이즈 초기화가 OnEnable보다 늦으면 OnLanguageChanged만으로는 최초 갱신이 누락되므로
         // OnInitialized에도 동일 갱신을 구독한다.
         Localization.OnInitialized += _RefreshLanguageButtons;
         Localization.OnInitialized += _RefreshLanguageDropdown;
         Localization.OnInitialized += _RefreshColorblindLabel;
         Localization.OnInitialized += _RefreshAccessibilityTexts;
         Localization.OnInitialized += _RefreshTabTexts;
      }

      private void _UnSubscribeListeners()
      {
         bgmMuteButton.onClick.RemoveAllListeners();
         sfxMuteButton.onClick.RemoveAllListeners();
         bgmSlider.onValueChanged.RemoveAllListeners();
         sfxSlider.onValueChanged.RemoveAllListeners();

         if (hapticToggle != null)
            hapticToggle.onValueChanged.RemoveListener(_OnHapticToggleChanged);

         if (cameraShakeToggle != null)
            cameraShakeToggle.onValueChanged.RemoveListener(_OnCameraShakeToggleChanged);

         if (colorblindButton != null)
            colorblindButton.onClick.RemoveListener(_OnColorblindButtonClick);

         if (deletePlayerPrefsButton != null)
            deletePlayerPrefsButton.onClick.RemoveAllListeners();

         if (tutorialReplayButton != null)
            tutorialReplayButton.onClick.RemoveListener(_OnTutorialReplayClick);

         if (koreanButton != null)
            koreanButton.onClick.RemoveAllListeners();

         if (englishButton != null)
            englishButton.onClick.RemoveAllListeners();

         if (japaneseButton != null)
            japaneseButton.onClick.RemoveAllListeners();

         if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(_OnLanguageDropdownChanged);

         if (soundTabButton != null)
            soundTabButton.onClick.RemoveAllListeners();

         if (accessibilityTabButton != null)
            accessibilityTabButton.onClick.RemoveAllListeners();

         if (languageTabButton != null)
            languageTabButton.onClick.RemoveAllListeners();

         Localization.OnLanguageChanged -= _RefreshLanguageButtons;
         Localization.OnLanguageChanged -= _RefreshLanguageDropdown;
         Localization.OnLanguageChanged -= _RefreshColorblindLabel;
         Localization.OnLanguageChanged -= _RefreshAccessibilityTexts;
         Localization.OnLanguageChanged -= _RefreshTabTexts;

         Localization.OnInitialized -= _RefreshLanguageButtons;
         Localization.OnInitialized -= _RefreshLanguageDropdown;
         Localization.OnInitialized -= _RefreshColorblindLabel;
         Localization.OnInitialized -= _RefreshAccessibilityTexts;
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

      private void _OnCameraShakeToggleChanged(bool isEnabled)
      {
         if (UserDataManager.Instance != null)
         {
            UserDataManager.Instance.SetCameraShakeEnabled(isEnabled);
         }
      }

      // 색약 유형을 없음→적색맹→녹색맹→청색맹 순으로 순환시키고 즉시 적용한다.
      private void _OnColorblindButtonClick()
      {
         if (UserDataManager.Instance == null)
            return;

         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);

         int next = (UserDataManager.Instance.ColorblindType + 1) % 4;
         UserDataManager.Instance.SetColorblindType(next);

         if (ColorblindController.Instance != null)
            ColorblindController.Instance.Apply(next);

         _RefreshColorblindLabel();
      }

      private enum OptionTab { Sound, Accessibility, Language }

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

      // 드롭다운에서 언어 선택 시 해당 언어코드로 전환한다.
      private void _OnLanguageDropdownChanged(int index)
      {
         if (index < 0 || index >= _languageCodes.Count) return;

         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);
         Localization.SetLanguage(_languageCodes[index]);
      }

      // 데이터에 존재하는 언어코드(헤더 자동수집)로 드롭다운 옵션을 채우고 현재 언어를 선택한다.
      private void _RefreshLanguageDropdown()
      {
         if (languageDropdown == null) return;

         _languageCodes.Clear();
         var options = new List<TMP_Dropdown.OptionData>();
         var setting = Localization.Setting;

         foreach (var code in Localization.AvailableLanguageCodes)
         {
            _languageCodes.Add(code);
            string display = setting != null ? setting.GetDisplayName(code) : code;
            options.Add(new TMP_Dropdown.OptionData(display));
         }

         languageDropdown.options = options;

         int current = _languageCodes.IndexOf(Localization.CurrentLanguageCode);
         languageDropdown.SetValueWithoutNotify(current >= 0 ? current : 0);
         languageDropdown.RefreshShownValue();
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

      private void _RefreshCameraShakeToggle()
      {
         if (cameraShakeToggle == null)
         {
            return;
         }

         bool isEnabled = UserDataManager.Instance == null || UserDataManager.Instance.IsCameraShakeEnabled;
         cameraShakeToggle.SetIsOnWithoutNotify(isEnabled);
      }

      private void _RefreshColorblindLabel()
      {
         if (colorblindLabel == null)
         {
            return;
         }

         int type = UserDataManager.Instance != null ? UserDataManager.Instance.ColorblindType : 0;
         (string key, string fallback) = type switch
         {
            1 => ("UI_Colorblind_Protanopia", "색약 보정: 적색맹"),
            2 => ("UI_Colorblind_Deuteranopia", "색약 보정: 녹색맹"),
            3 => ("UI_Colorblind_Tritanopia", "색약 보정: 청색맹"),
            _ => ("UI_Colorblind_None", "색약 보정: 없음"),
         };
         colorblindLabel.text = LocalizeHelper.GetByKey(key, fallback);
         colorblindLabel.color = Color.black;
         // 긴 언어(영어 색맹 명칭 등)에서 버튼 밖으로 텍스트가 넘치지 않도록 자동 축소.
         colorblindLabel.enableAutoSizing = true;
         colorblindLabel.fontSizeMin = 16;
         colorblindLabel.fontSizeMax = 32;
      }

      // 접근성 탭 텍스트·카메라 흔들림 라벨을 현재 언어로 갱신한다.
      private void _RefreshAccessibilityTexts()
      {
         _ApplyTabText(accessibilityTabText, "UI_Option_Tab_Accessibility", "접근성");

         if (cameraShakeLabel != null)
         {
            cameraShakeLabel.text = LocalizeHelper.GetByKey("UI_Option_CameraShake", "카메라 흔들림");
            cameraShakeLabel.color = Color.black;
         }
      }

      // Sound·Language 탭 라벨과 BGM/SFX 그룹 라벨을 현재 언어로 갱신한다.
      private void _RefreshTabTexts()
      {
         _ApplyTabText(soundTabText, "UI_Option_Tab_Sound", "사운드");
         _ApplyTabText(languageTabText, "UI_Option_Tab_Language", "언어");
         _RefreshSoundLabels();

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

      // Sound 패널의 BGM/SFX 그룹 라벨을 현재 언어로 갱신한다. (그룹이 nested 프리팹이라 자식 TMP로 접근)
      private void _RefreshSoundLabels()
      {
         if (soundPanel == null) return;

         Transform panel = soundPanel.transform;
         if (panel.childCount > 0)
            _ApplySoundGroupLabel(panel.GetChild(0), "UI_Option_BGM", "배경음");

         if (panel.childCount > 1)
            _ApplySoundGroupLabel(panel.GetChild(1), "UI_Option_SFX", "효과음");
      }

      private void _ApplySoundGroupLabel(Transform group, string key, string fallback)
      {
         if (group == null) return;

         TMP_Text label = group.GetComponentInChildren<TMP_Text>(true);
         if (label != null)
            label.text = LocalizeHelper.GetByKey(key, fallback);
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
   }
}
