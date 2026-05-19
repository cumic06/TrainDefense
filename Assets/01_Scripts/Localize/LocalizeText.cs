using TMPro;
using UnityEngine;

namespace TrainDefense.Localize
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        [SerializeField] private LocalizeKey _key;

        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

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

        public void Refresh()
        {
            if (_text == null || !Localization.IsInitialized) return;
            string localized = Localization.Get(_key);
            if (!string.IsNullOrEmpty(localized))
                _text.text = localized;
        }
    }
}
