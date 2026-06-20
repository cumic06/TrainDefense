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
    }
}
