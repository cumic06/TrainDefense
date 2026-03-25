using TrainDefense.Game;
using UnityEngine;
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
      private Sprite muteSprite;
      [SerializeField]
      private Sprite unMuteSprite;
      #endregion

      private void Awake()
      {
         _SubscribeListeners();
      }

      private void OnEnable()
      {
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
      }

      public void ShowOptionUI()
      {
         gameObject.SetActive(true);
         if (TimeManager.Instance != null)
            TimeManager.Instance.Pause();
      }

      public void HideOptionUI()
      {
         gameObject.SetActive(false);
         if (TimeManager.Instance != null)
            TimeManager.Instance.Resume();
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
         bgmSlider.value = value;
      }

      private void _SetSFXSliderValue(float value)
      {
         sfxSlider.value = value;
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
