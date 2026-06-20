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

            // 리터럴 "\n"을 실제 줄바꿈으로 먼저 변환(ProtectWordBreak가 \ n 사이를 묶어 이스케이프를
            // 깨뜨리지 않도록)한 뒤, 한글 단어 중간 줄바꿈을 방지(주황→주/황, 줍니다.→줍니/다.). 일/중은 원문 유지.
            string localized = LocalizeHelper.ProtectWordBreak(Localization.Get(_key)?.Replace("\\n", "\n"));
            if (!string.IsNullOrEmpty(localized))
                _text.text = localized;
        }

        private void _ApplyFont()
        {
            var setting = Localization.Setting;
            if (setting == null) return;

            // 언어코드별 폰트 테이블 조회. 지정 폰트가 없으면 TMP 기본 폰트를 그대로 사용한다.
            var font = setting.GetFont(Localization.CurrentLanguageCode);
            if (font != null)
            {
                _text.font = font;
                _text.fontSharedMaterial = font.material;
            }
            else
            {
                _text.font = _defaultFont;
                _text.fontSharedMaterial = _defaultMaterial;
            }
        }
    }
}
