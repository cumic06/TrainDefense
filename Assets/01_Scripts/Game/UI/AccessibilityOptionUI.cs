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
   // 옵션 창 접근성 탭(Panel_Accessibility)의 햅틱·카메라 흔들림·색약 보정 컨트롤을 담당한다.
   // Panel_Accessibility 오브젝트에 부착하고, 접근성 관련 SerializeField만 이 컴포넌트에 연결한다.
   public class AccessibilityOptionUI : MonoBehaviour
   {
      #region Fields
      [SerializeField]
      private Toggle hapticToggle;
      [SerializeField]
      private Toggle cameraShakeToggle;
      [SerializeField]
      private TMP_Text cameraShakeLabel;
      [SerializeField]
      private Button colorblindButton;
      [SerializeField]
      private TMP_Text colorblindLabel;
      #endregion

      private void OnEnable()
      {
         if (hapticToggle != null)
            hapticToggle.onValueChanged.AddListener(_OnHapticToggleChanged);

         if (cameraShakeToggle != null)
            cameraShakeToggle.onValueChanged.AddListener(_OnCameraShakeToggleChanged);

         if (colorblindButton != null)
            colorblindButton.onClick.AddListener(_OnColorblindButtonClick);

         Localization.OnLanguageChanged += _RefreshTexts;
         Localization.OnInitialized += _RefreshTexts;

         _RefreshHapticToggle();
         _RefreshCameraShakeToggle();
         _RefreshTexts();
      }

      private void OnDisable()
      {
         if (hapticToggle != null)
            hapticToggle.onValueChanged.RemoveListener(_OnHapticToggleChanged);

         if (cameraShakeToggle != null)
            cameraShakeToggle.onValueChanged.RemoveListener(_OnCameraShakeToggleChanged);

         if (colorblindButton != null)
            colorblindButton.onClick.RemoveListener(_OnColorblindButtonClick);

         Localization.OnLanguageChanged -= _RefreshTexts;
         Localization.OnInitialized -= _RefreshTexts;
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

      // 카메라 흔들림 라벨·색약 보정 라벨을 현재 언어로 갱신한다.
      private void _RefreshTexts()
      {
         if (cameraShakeLabel != null)
         {
            cameraShakeLabel.text = LocalizeHelper.GetByKey("UI_Option_CameraShake", "카메라 흔들림");
         }

         _RefreshColorblindLabel();
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
         // 긴 언어(영어 색맹 명칭 등)에서 버튼 밖으로 텍스트가 넘치지 않도록 자동 축소.
         colorblindLabel.enableAutoSizing = true;
         colorblindLabel.fontSizeMin = 16;
         colorblindLabel.fontSizeMax = 32;
      }
   }
}
