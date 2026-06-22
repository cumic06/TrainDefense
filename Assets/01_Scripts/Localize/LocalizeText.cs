using TMPro;
using UnityEngine;

namespace TrainDefense.Localize
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        #region Fields
        [SerializeField] private LocalizeKey _key;
        [SerializeField] private bool _autoSizeForLongText = true;
        #endregion

        #region Variables
        private TextMeshProUGUI _text;
        private TMP_FontAsset _defaultFont;
        private Material _defaultMaterial;
        private float _baseFontSize;
        private bool _hadAutoSizingByDesign;
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

            // 자동 축소 기준 폰트 크기 캡처. 이미 인스펙터에서 Auto Size가 켜진 텍스트는
            // 그 설정을 존중하고, 꺼진 텍스트만 런타임에 켜서 긴 언어의 오버플로를 막는다.
            _hadAutoSizingByDesign = _text.enableAutoSizing;
            _baseFontSize = _text.enableAutoSizing ? _text.fontSizeMax : _text.fontSize;
        }
        #endregion

        #region Sub/UnSub
        #endregion

        public void Refresh()
        {
            if (_text == null || !Localization.IsInitialized) return;

            _ApplyFont();
            _ApplyAutoSize();

            // RTL(아랍어/히브리어 등) 언어에서는 텍스트 방향을 오른쪽→왼쪽으로 맞춘다.
            // 비-RTL 언어로 되돌아오면 false로 리셋된다.
            _text.isRightToLeftText = Localization.IsCurrentRightToLeft;

            // 리터럴 "\n"을 실제 줄바꿈으로 변환(TSV 셀에 직접 개행을 못 넣어 \n 문자열로 저장됨).
            // 한글 단어 중간 줄바꿈은 TMP_Settings의 Modern Hangul Line Breaking Rules로 처리한다.
            string localized = Localization.Get(_key)?.Replace("\\n", "\n");
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

        // 언어마다 텍스트 길이가 달라(독일어/핀란드어 등은 길다) 고정 박스를 넘치므로,
        // TMP Auto Size로 넘칠 때만 폰트를 자동 축소해 박스 안에 맞춘다.
        private void _ApplyAutoSize()
        {
            if (!_autoSizeForLongText || _text == null) return;
            if (_hadAutoSizingByDesign) return; // 디자인된 Auto Size 설정은 건드리지 않음

            _text.enableAutoSizing = true;
            if (_baseFontSize > 0f)
            {
                _text.fontSizeMax = _baseFontSize;                       // 원래 크기보다 커지지 않음
                _text.fontSizeMin = Mathf.Max(8f, _baseFontSize * 0.5f); // 최대 절반까지 축소 허용
            }
        }
    }
}
