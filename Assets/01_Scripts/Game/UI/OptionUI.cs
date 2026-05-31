using Cumic.Sequence;
using TrainDefense.Game;
using TrainDefense.Game.UI;
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
      private ConfirmPopup resetConfirmPopup;

      [SerializeField]
      private Sprite muteSprite;
      [SerializeField]
      private Sprite unMuteSprite;

      [Header("인게임 전용 로비 버튼 (로비 씬에서는 자동 숨김)")]
      [SerializeField]
      private Transform lobbyButton;
      #endregion

      // 로비 씬의 빌드 인덱스 (ChangeSceneButton.LobbySceneIndex와 동일)
      private const int LobbySceneBuildIndex = 1;

      private void Awake()
      {
         _SubscribeListeners();
      }

      private void OnEnable()
      {
         if (SoundManager.Instance == null) return;

         _SetBGMSliderValue(SoundManager.Instance.BGMVolume);
         _SetSFXSliderValue(SoundManager.Instance.SFXVolume);
         _SetBGMMuteSprite();
         _SetSFXMuteSprite();
         _RefreshHapticToggle();
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
         {
            hapticToggle.onValueChanged.AddListener(_OnHapticToggleChanged);
         }

         if (deletePlayerPrefsButton != null)
         {
            deletePlayerPrefsButton.onClick.AddListener(OnDeletePlayerPrefsClicked);
         }

         if (resetConfirmPopup != null)
         {
            resetConfirmPopup.OnConfirmed += _DeleteAllPlayerPrefs;
         }
      }

      private void _UnSubscribeListeners()
      {
         bgmMuteButton.onClick.RemoveAllListeners();
         sfxMuteButton.onClick.RemoveAllListeners();
         bgmSlider.onValueChanged.RemoveAllListeners();
         sfxSlider.onValueChanged.RemoveAllListeners();

         if (hapticToggle != null)
         {
            hapticToggle.onValueChanged.RemoveListener(_OnHapticToggleChanged);
         }

         if (deletePlayerPrefsButton != null)
         {
            deletePlayerPrefsButton.onClick.RemoveAllListeners();
         }

         if (resetConfirmPopup != null)
         {
            resetConfirmPopup.OnConfirmed -= _DeleteAllPlayerPrefs;
         }
      }

      public void OnDeletePlayerPrefsClicked()
      {
         if (resetConfirmPopup != null)
         {
            resetConfirmPopup.ShowPopup();
            resetConfirmPopup.OnConfirmed += _DeleteAllPlayerPrefs;
            return;
         }
      }

      public void ShowOptionUI()
      {
         gameObject.SetActive(true);
         _RefreshLobbyButton();

         if (InGameSequence.Instance != null)
            InGameSequence.Instance.PushOverlay(OverlayPhase.Option);
         else if (TimeManager.Instance != null)
            TimeManager.Instance.Pause();
      }

      public void HideOptionUI()
      {
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

      private void _DeleteAllPlayerPrefs()
      {
         PlayerPrefs.DeleteAll();
         PlayerPrefs.Save();

         UserDataManager.Instance.TutorialSaveData.ResetAll();

         Debug.Log("[OptionUI] PlayerPrefs 전체 삭제 완료");
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
