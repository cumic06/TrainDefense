using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TrainDefense.Localize
{
    public class LocalizeFontSwitcher : MonoBehaviour
    {
        #region Fields
        #endregion

        #region Variables
        private static LocalizeFontSwitcher _instance;
        private readonly List<(TextMeshProUGUI tmp, TMP_FontAsset originalFont, Material originalMaterial, FontStyles originalFontStyle)> _registered = new();
        #endregion

        #region LifeCycle
        private void OnEnable()
        {
            _SubscribeEvents();
        }

        private void OnDisable()
        {
            _UnsubscribeEvents();
        }

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);

                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region Sub/UnSub
        private void _SubscribeEvents()
        {
            Localization.OnLanguageChanged += _ApplyAll;
            Localization.OnInitialized += _ApplyAll;
            if (Localization.IsInitialized) _ApplyAll();
        }

        private void _UnsubscribeEvents()
        {
            Localization.OnLanguageChanged -= _ApplyAll;
            Localization.OnInitialized -= _ApplyAll;
        }
        #endregion

        public static void Register(TextMeshProUGUI tmp)
        {
            if (_instance == null || tmp == null) return;

            _instance._registered.Add((tmp, tmp.font, tmp.fontSharedMaterial, tmp.fontStyle));
            _instance._ApplySingle(tmp, tmp.font, tmp.fontSharedMaterial, tmp.fontStyle);
        }

        public static void Unregister(TextMeshProUGUI tmp)
        {
            if (_instance == null) return;

            _instance._registered.RemoveAll(x => x.tmp == tmp);
        }

        private void _ApplyAll()
        {
            for (int i = _registered.Count - 1; i >= 0; i--)
            {
                var (tmp, originalFont, originalMaterial, originalFontStyle) = _registered[i];

                if (tmp == null)
                {
                    _registered.RemoveAt(i);

                    continue;
                }

                _ApplySingle(tmp, originalFont, originalMaterial, originalFontStyle);
            }
        }

        private void _ApplySingle(TextMeshProUGUI tmp, TMP_FontAsset originalFont, Material originalMaterial, FontStyles originalFontStyle)
        {
            var setting = Localization.Setting;
            if (setting == null) return;

            // 언어코드별 폰트 테이블 조회. 지정 폰트가 없으면 원래 폰트 유지.
            string code = Localization.CurrentLanguageCode;
            var font = setting.GetFont(code);
            bool useCustom = font != null;

            tmp.font = useCustom ? font : originalFont;
            tmp.fontSharedMaterial = useCustom ? font.material : originalMaterial;
            // 일본어는 기존처럼 Bold 가미(가독성). 그 외 언어는 원래 스타일 유지.
            bool isJapanese = code == "Japanese";
            tmp.fontStyle = isJapanese ? (originalFontStyle | FontStyles.Bold) : originalFontStyle;
        }
    }
}
