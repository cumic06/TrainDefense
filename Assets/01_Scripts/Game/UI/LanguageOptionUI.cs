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
   // 옵션 창 언어 탭(Panel_Language)의 언어 선택 버튼·드롭다운을 담당한다.
   // Panel_Language 오브젝트에 부착하고, 언어 관련 SerializeField만 이 컴포넌트에 연결한다.
   public class LanguageOptionUI : MonoBehaviour
   {
      #region Fields
      [SerializeField]
      private Button koreanButton;
      [SerializeField]
      private Button englishButton;
      [SerializeField]
      private Button japaneseButton;
      [SerializeField]
      private TMP_Dropdown languageDropdown;
      #endregion

      private static readonly Color _langSelectedColor = new Color(1f, 0.85f, 0.3f);
      private static readonly Color _langNormalColor = Color.white;

      // 드롭다운 인덱스 → 언어코드 매핑(런타임에 채움)
      private readonly List<string> _languageCodes = new();

      private void OnEnable()
      {
         if (koreanButton != null)
            koreanButton.onClick.AddListener(_OnKoreanClick);

         if (englishButton != null)
            englishButton.onClick.AddListener(_OnEnglishClick);

         if (japaneseButton != null)
            japaneseButton.onClick.AddListener(_OnJapaneseClick);

         if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(_OnLanguageDropdownChanged);

         Localization.OnLanguageChanged += _Refresh;
         Localization.OnInitialized += _Refresh;

         _Refresh();
      }

      private void OnDisable()
      {
         if (koreanButton != null)
            koreanButton.onClick.RemoveListener(_OnKoreanClick);

         if (englishButton != null)
            englishButton.onClick.RemoveListener(_OnEnglishClick);

         if (japaneseButton != null)
            japaneseButton.onClick.RemoveListener(_OnJapaneseClick);

         if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(_OnLanguageDropdownChanged);

         Localization.OnLanguageChanged -= _Refresh;
         Localization.OnInitialized -= _Refresh;
      }

      private void _Refresh()
      {
         _RefreshLanguageButtons();
         _RefreshLanguageDropdown();
      }

      private void _OnKoreanClick() => _OnLanguageButtonClick(SystemLanguage.Korean);
      private void _OnEnglishClick() => _OnLanguageButtonClick(SystemLanguage.English);
      private void _OnJapaneseClick() => _OnLanguageButtonClick(SystemLanguage.Japanese);

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
   }
}
