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
   // 옵션 창 사운드 탭(Panel_Sound)의 BGM/SFX 컨트롤을 담당한다.
   // Panel_Sound 오브젝트에 부착하고, 사운드 관련 SerializeField만 이 컴포넌트에 연결한다.
   // (BGM/SFX 그룹 라벨은 Panel_Sound의 첫·두 번째 자식 그룹에서 자동 탐색하므로 배치 순서 유지 필요)
   public class SoundOptionUI : MonoBehaviour
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
      private Sprite muteSprite;
      [SerializeField]
      private Sprite unMuteSprite;
      #endregion

      private void OnEnable()
      {
         bgmMuteButton.onClick.AddListener(_MuteBGM);
         sfxMuteButton.onClick.AddListener(_MuteSFX);
         bgmSlider.onValueChanged.AddListener(_ChangeBGMVolume);
         sfxSlider.onValueChanged.AddListener(_ChangeSFXVolume);

         Localization.OnLanguageChanged += _RefreshSoundLabels;
         Localization.OnInitialized += _RefreshSoundLabels;

         _RefreshValues();
         _RefreshSoundLabels();
      }

      private void OnDisable()
      {
         bgmMuteButton.onClick.RemoveListener(_MuteBGM);
         sfxMuteButton.onClick.RemoveListener(_MuteSFX);
         bgmSlider.onValueChanged.RemoveListener(_ChangeBGMVolume);
         sfxSlider.onValueChanged.RemoveListener(_ChangeSFXVolume);

         Localization.OnLanguageChanged -= _RefreshSoundLabels;
         Localization.OnInitialized -= _RefreshSoundLabels;
      }

      private void _RefreshValues()
      {
         if (SoundManager.Instance == null) return;

         bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BGMVolume);
         sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SFXVolume);
         _SetBGMMuteSprite();
         _SetSFXMuteSprite();
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

      // Panel_Sound의 BGM/SFX 그룹 라벨을 현재 언어로 갱신한다. (그룹이 nested 프리팹이라 자식 TMP로 접근)
      private void _RefreshSoundLabels()
      {
         Transform panel = transform;
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
   }
}
