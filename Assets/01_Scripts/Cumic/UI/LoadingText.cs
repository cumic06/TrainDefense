using TMPro;
using UnityEngine;

namespace Cumic.UI
{
    /// <summary>
    /// 로딩 문구 끝의 점(...)을 하나씩 늘렸다 줄이며 진행 중임을 보여준다.
    /// 문구 자체는 같은 오브젝트의 LocalizeText가 채우고, 여기서는 보이는 글자 수만 조절한다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LoadingText : MonoBehaviour
    {
        #region Variables
        private const string DOTS = "...";
        private const int ALL_VISIBLE = 99999;

        private TMP_Text _text;
        private float _elapsedTime;
        private int _dotStep;
        #endregion

        #region Fields
        [SerializeField] private float _stepDuration = 0.4f;
        #endregion

        #region LifeCycle
        private void OnEnable()
        {
            _elapsedTime = 0f;
            _dotStep = DOTS.Length;
            _ApplyVisibleCount();
        }

        private void OnDisable()
        {
            _text.maxVisibleCharacters = ALL_VISIBLE;
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            _elapsedTime += Time.unscaledDeltaTime;

            if (_elapsedTime < _stepDuration) return;

            _elapsedTime -= _stepDuration;
            _dotStep = (_dotStep + 1) % (DOTS.Length + 1);
            _ApplyVisibleCount();
        }
        #endregion

        #region Sub/UnSub
        #endregion

        private void _ApplyVisibleCount()
        {
            string current = _text.text;

            // RTL 언어는 전처리로 글자 순서가 뒤집혀 끝의 점만 가릴 수 없으므로 전부 보인다.
            if (_text.isRightToLeftText || string.IsNullOrEmpty(current) || !current.EndsWith(DOTS))
            {
                _text.maxVisibleCharacters = ALL_VISIBLE;

                return;
            }

            _text.maxVisibleCharacters = current.Length - DOTS.Length + _dotStep;
        }
    }
}
