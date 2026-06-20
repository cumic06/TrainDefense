using TMPro;
using UnityEngine;

namespace TrainDefense.Localize
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        #region Fields
        [SerializeField] private LocalizeKey _key;
        #endregion

        #region Variables
        private TextMeshProUGUI _text;
        private TMP_FontAsset _defaultFont;
        private Material _defaultMaterial;
        #endregion

        #region LifeCycle
        private void OnEnable()
        {
            Localization.OnLanguageChanged += Refresh;
            Localization.OnInitialized += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= Refresh;
            Localization.OnInitialized -= Refresh;
        }

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _defaultFont = _text.font;
            _defaultMaterial = _text.fontSharedMaterial;
        }
        #endregion

        #region Sub/UnSub
        #endregion

        public void Refresh()
        {
            if (_text == null || !Localization.IsInitialized) return;

            _ApplyFont();

            // 리터럴 "\n"을 실제 줄바꿈으로 변환(TSV 셀에 직접 개행을 못 넣어 \n 문자열로 저장됨).
            // 한글 단어 중간 줄바꿈은 TMP_Settings의 Modern Hangul Line Breaking Rules로 처리한다.
            string localized = Localization.Get(_key)?.Replace("\\n", "\n");
            if (!string.IsNullOrEmpty(localized))
                _text.text = localized;
        }

        private void _ApplyFont()
        {
            var setting = Localization.Setting;
            if (setting?.japaneseFontAsset == null) return;

            bool isJapanese = Localization.CurrentLanguage == SystemLanguage.Japanese;
            _text.font = isJapanese ? setting.japaneseFontAsset : _defaultFont;
            _text.fontSharedMaterial = isJapanese ? setting.japaneseFontAsset.material : _defaultMaterial;
        }
    }
}
