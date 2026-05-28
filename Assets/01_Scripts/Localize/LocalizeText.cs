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

            string localized = Localization.Get(_key);
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
